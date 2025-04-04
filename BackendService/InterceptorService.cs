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
            // Setup listener
            string prefix = $"http://+:{_port}/";
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            _logger.LogInformation($"Interceptor started listening on {prefix}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    // Process the request asynchronously
                    _ = Task.Run(() => ProcessRequestAsync(context), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while handling HTTP request");
                }
            }

            _listener.Stop();
            _listener.Close();
            _logger.LogInformation("Interceptor stopped");
            
            // Return to complete the task
            return;
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
                    Timestamp = DateTime.UtcNow,
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
                        Timestamp = DateTime.UtcNow
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
