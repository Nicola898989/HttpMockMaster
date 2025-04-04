using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using BackendService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace BackendService
{
    public class InterceptorService : BackgroundService
    {
        private readonly HttpListener _listener;
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<InterceptorService> _logger;
        private readonly ProxyService _proxyService;
        private readonly RuleService _ruleService;
        private readonly TestScenarioService _testScenarioService;
        private string _proxyDomain;
        private readonly int _port;
        private int? _recordingScenarioId;

        // Proprietà per controllare se stiamo registrando
        public bool IsRecording => _recordingScenarioId.HasValue;

        public InterceptorService(
            DatabaseContext dbContext, 
            ILogger<InterceptorService> logger, 
            ProxyService proxyService, 
            RuleService ruleService,
            TestScenarioService testScenarioService)
        {
            _listener = new HttpListener();
            _dbContext = dbContext;
            _logger = logger;
            _proxyService = proxyService;
            _ruleService = ruleService;
            _testScenarioService = testScenarioService;
            _port = 8888; // Default port for interception
            _proxyDomain = ""; // Will be set dynamically
            _recordingScenarioId = null; // Not recording by default
        }

        public void SetProxyDomain(string domain)
        {
            _proxyDomain = domain;
        }
        
        public string GetProxyDomain()
        {
            return _proxyDomain;
        }
        
        public async Task<bool> StartRecordingAsync(int scenarioId)
        {
            try
            {
                _recordingScenarioId = scenarioId;
                await _testScenarioService.StartRecordingAsync(scenarioId);
                _logger.LogInformation($"Started recording to test scenario with ID: {scenarioId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to start recording to scenario ID: {scenarioId}");
                return false;
            }
        }
        
        public async Task<bool> StopRecordingAsync()
        {
            try 
            {
                _recordingScenarioId = null;
                await _testScenarioService.StopRecordingAsync();
                _logger.LogInformation("Stopped recording to test scenario");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop recording");
                return false;
            }
        }
        
        public Task<bool> IsRecordingAsync()
        {
            // Since this is a simple property check, we can just return a completed task
            return Task.FromResult(_recordingScenarioId.HasValue);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Setup listener with improved error handling e ambiente Replit-safe
            string prefix = $"http://+:{_port}/";
            
            try 
            {
                // Verifichiamo se siamo in ambiente Replit
                bool isReplitEnv = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REPL_ID"));
                
                if (isReplitEnv)
                {
                    _logger.LogWarning("Rilevato ambiente Replit: HttpListener potrebbe non funzionare correttamente.");
                    _logger.LogInformation("Attivazione dell'intercettore in modalità compatibilità Replit...");
                    
                    // In Replit, tentiamo di utilizzare un indirizzo più specifico anziché il wildcard
                    _listener.Prefixes.Clear();
                    _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
                    
                    // Utilizziamo un pattern di retry limitato per non bloccare l'avvio
                    int maxRetries = 3;
                    for (int retry = 0; retry < maxRetries; retry++)
                    {
                        try
                        {
                            _listener.Start();
                            _logger.LogInformation($"Interceptor avviato su http://127.0.0.1:{_port}/ (tentativo {retry + 1})");
                            break;
                        }
                        catch (Exception ex) when (retry < maxRetries - 1)
                        {
                            _logger.LogWarning($"Tentativo {retry + 1} fallito: {ex.Message}, riprovando...");
                            await Task.Delay(500, stoppingToken); // Breve attesa tra tentativi
                        }
                    }
                }
                else
                {
                    // In ambienti non-Replit, comportamento normale
                    _listener.Prefixes.Add(prefix);
                    _listener.Start();
                    _logger.LogInformation($"Interceptor avviato su {prefix}");
                }
                
                // Loop principale di ascolto
                while (!stoppingToken.IsCancellationRequested && _listener.IsListening)
                {
                    try
                    {
                        // Utilizziamo un timeout per evitare blocchi indefiniti
                        var timeoutTask = Task.Delay(5000, stoppingToken); // 5 secondi di timeout
                        var contextTask = _listener.GetContextAsync();
                        
                        var completedTask = await Task.WhenAny(contextTask, timeoutTask);
                        
                        if (completedTask == contextTask)
                        {
                            var context = await contextTask;
                            // Process the request asynchronously with explicit exception handling
                            _ = Task.Run(async () => {
                                try {
                                    await ProcessRequestAsync(context);
                                }
                                catch (Exception ex) {
                                    _logger.LogError(ex, "Errore nel processare la richiesta");
                                }
                            }, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Errore durante la gestione della richiesta HTTP");
                        // Breve pausa per evitare cicli di errore troppo rapidi
                        await Task.Delay(500, stoppingToken);
                    }
                }
            }
            catch (HttpListenerException ex)
            {
                _logger.LogError(ex, "Errore nell'avvio dell'HttpListener. Probabile mancanza di privilegi o porta già in uso.");
                _logger.LogWarning("L'intercettore HTTP non è stato avviato, ma il servizio REST API continuerà a funzionare.");
                
                // Simuliamo un task attivo per mantenere il servizio in esecuzione
                try
                {
                    await Task.Delay(-1, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Normale durante lo shutdown, ignoriamo
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore critico nell'intercettore");
            }
            finally
            {
                // Cleanup
                if (_listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                }
                _logger.LogInformation("Interceptor fermato");
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                // Record the incoming request
                var requestModel = new Models.HttpRequest
                {
                    Url = request.Url.ToString(),
                    Method = request.HttpMethod,
                    Headers = SerializeHeaders(request.Headers),
                    Body = await GetRequestBodyAsync(request),
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    IsProxied = !string.IsNullOrEmpty(_proxyDomain),
                    TargetDomain = _proxyDomain
                };
                _dbContext.Requests.Add(requestModel);
                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation($"Received {request.HttpMethod} request for {request.Url}");

                // Store HTTP response to be recorded
                Models.HttpResponse responseModel = null;
                
                // Check if there's a matching rule
                var rule = await _ruleService.FindMatchingRuleAsync(request);
                if (rule != null)
                {
                    _logger.LogInformation($"Found matching rule for {request.Url}. Returning configured response.");
                    await SendResponseFromRuleAsync(response, rule.Response);
                    responseModel = rule.Response;
                }
                // If we have a proxy domain and no rule matched, forward the request
                else if (!string.IsNullOrEmpty(_proxyDomain))
                {
                    _logger.LogInformation($"Forwarding request to proxy target: {_proxyDomain}");
                    var proxyResponse = await _proxyService.ForwardRequestAsync(request, _proxyDomain);
                    await SendProxyResponseAsync(response, proxyResponse);
                    responseModel = proxyResponse;
                    
                    // Se stiamo registrando, salva la chiamata proxy nello scenario di test
                    if (IsRecording && _recordingScenarioId.HasValue)
                    {
                        try 
                        {
                            // Registra lo scambio di proxy nel test scenario
                            if (requestModel != null && responseModel != null)
                            {
                                // Crea oggetti HttpRequest/HttpResponse per la richiesta al server e la risposta
                                BackendService.Models.HttpRequest? serverRequest = null;
                                BackendService.Models.HttpResponse? serverResponse = null;
                                
                                // Se abbiamo fatto una chiamata al server, costruisci i modelli
                                // Controllo se abbiamo una risposta proxy valida
                                bool hasValidProxyResp = responseModel != null && _proxyDomain != null;
                                
                                if (hasValidProxyResp) 
                                {
                                    // Usa il modello della richiesta client come base per la richiesta server
                                    serverRequest = new BackendService.Models.HttpRequest
                                    {
                                        Method = requestModel.Method,
                                        Url = $"{_proxyDomain}{request.Url.PathAndQuery}",  // Usa l'URL del proxy
                                        Headers = requestModel.Headers, // Utilizza gli stessi headers
                                        Body = requestModel.Body,
                                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                    };
                                    
                                    // Per la risposta, usa direttamente il responseModel già costruito
                                    serverResponse = responseModel;
                                }
                                
                                await _testScenarioService.RecordProxyExchangeAsync(
                                    _recordingScenarioId.Value,
                                    requestModel,
                                    responseModel,
                                    serverRequest,
                                    serverResponse
                                );
                                
                                _logger.LogInformation($"Registrato scambio proxy nello scenario {_recordingScenarioId.Value}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Errore durante la registrazione dello scambio proxy: {ex.Message}");
                        }
                    }
                }
                // No rule or proxy - return 404
                else
                {
                    _logger.LogInformation("No matching rule or proxy configuration. Returning 404.");
                    response.StatusCode = 404;
                    byte[] buffer = Encoding.UTF8.GetBytes("No matching rule found for this request.");
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    
                    // Create a 404 response model
                    responseModel = new Models.HttpResponse
                    {
                        StatusCode = 404,
                        Body = "No matching rule found for this request.",
                        Headers = "Content-Type: text/plain",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    _dbContext.Responses.Add(responseModel);
                    await _dbContext.SaveChangesAsync();
                }
                
                // If recording to a test scenario, store this request/response pair
                if (_recordingScenarioId.HasValue && responseModel != null)
                {
                    try 
                    {
                        _logger.LogInformation($"Recording request/response to test scenario {_recordingScenarioId.Value}");
                        
                        // Use TestScenarioService to record this request/response pair
                        await _testScenarioService.RecordRequestResponseAsync(
                            _recordingScenarioId.Value,
                            requestModel,
                            responseModel
                        );
                        
                        _logger.LogInformation($"Successfully recorded request to scenario {_recordingScenarioId.Value}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error recording to test scenario");
                    }
                }

                response.Close();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                context.Response.StatusCode = 500;
                byte[] buffer = Encoding.UTF8.GetBytes("Internal server error occurred.");
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.Close();
                return;
            }
        }

        private async Task SendResponseFromRuleAsync(HttpListenerResponse response, Models.HttpResponse ruleResponse)
        {
            response.StatusCode = ruleResponse.StatusCode;
            
            // Set headers from the rule
            if (!string.IsNullOrEmpty(ruleResponse.Headers))
            {
                string[] headerLines = ruleResponse.Headers.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var headerLine in headerLines)
                {
                    var parts = headerLine.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var name = parts[0].Trim();
                        var value = parts[1].Trim();
                        try
                        {
                            response.Headers.Add(name, value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to add header {name}: {value}");
                        }
                    }
                }
            }

            // Write the body
            if (!string.IsNullOrEmpty(ruleResponse.Body))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(ruleResponse.Body);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            
            return;
        }

        private async Task SendProxyResponseAsync(HttpListenerResponse response, Models.HttpResponse proxyResponse)
        {
            response.StatusCode = proxyResponse.StatusCode;
            
            // Set headers from the proxied response
            if (!string.IsNullOrEmpty(proxyResponse.Headers))
            {
                string[] headerLines = proxyResponse.Headers.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var headerLine in headerLines)
                {
                    var parts = headerLine.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var name = parts[0].Trim();
                        var value = parts[1].Trim();
                        try
                        {
                            // Skip headers that would cause issues when transferred
                            if (!name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) &&
                                !name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                            {
                                response.Headers.Add(name, value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to add header {name}: {value}");
                        }
                    }
                }
            }

            // Write the body
            if (!string.IsNullOrEmpty(proxyResponse.Body))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(proxyResponse.Body);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            
            return;
        }

        private async Task<string> GetRequestBodyAsync(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return string.Empty;

            using (var bodyStream = request.InputStream)
            {
                using (var reader = new StreamReader(bodyStream, Encoding.UTF8))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private string SerializeHeaders(System.Collections.Specialized.NameValueCollection headers)
        {
            var builder = new StringBuilder();
            foreach (string key in headers.AllKeys)
            {
                builder.AppendLine($"{key}: {headers[key]}");
            }
            return builder.ToString();
        }
    }
}
