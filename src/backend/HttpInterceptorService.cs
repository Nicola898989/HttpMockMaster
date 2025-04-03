using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class HttpInterceptorService : BackgroundService
{
    private readonly HttpListener _listener;
    private readonly ILogger<HttpInterceptorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ProxyService _proxyService;

    public HttpInterceptorService(ILogger<HttpInterceptorService> logger, ProxyService proxyService, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _proxyService = proxyService;
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://+:8888/");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _listener.Start();
            _logger.LogInformation("HTTP interceptor listening on port 8888");

            while (!stoppingToken.IsCancellationRequested)
            {
                var listenerContext = await _listener.GetContextAsync();
                ProcessRequestAsync(listenerContext, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in HTTP interceptor service");
        }
        finally
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }

    private async void ProcessRequestAsync(HttpListenerContext context, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Record the incoming request
            var request = await RecordHttpRequestAsync(context.Request, dbContext);

            // Check if there's a matching rule
            var rule = await dbContext.Rules
                .Where(r => context.Request.Url.PathAndQuery.Contains(r.PathPattern))
                .FirstOrDefaultAsync(stoppingToken);

            if (rule != null)
            {
                // Apply the rule and send a custom response
                await SendRuleResponseAsync(context.Response, rule, request, dbContext);
            }
            else
            {
                // Forward the request to the target server
                await _proxyService.ProxyRequestAsync(context, request, dbContext);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing HTTP request");
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch
            {
                // Ignore errors when trying to close an already closed response
            }
        }
    }

    private async Task<HttpRequest> RecordHttpRequestAsync(HttpListenerRequest request, AppDbContext dbContext)
    {
        var requestModel = new HttpRequest
        {
            Method = request.HttpMethod,
            Url = request.Url.ToString(),
            Path = request.Url.PathAndQuery,
            Timestamp = DateTime.UtcNow,
            Headers = SerializeHeaders(request.Headers),
            Body = await ReadRequestBodyAsync(request)
        };

        dbContext.Requests.Add(requestModel);
        await dbContext.SaveChangesAsync();
        
        return requestModel;
    }

    private async Task SendRuleResponseAsync(HttpListenerResponse response, Rule rule, HttpRequest requestModel, AppDbContext dbContext)
    {
        try
        {
            // Set status code
            response.StatusCode = rule.StatusCode;

            // Set headers from rule
            if (!string.IsNullOrEmpty(rule.Headers))
            {
                var headers = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(rule.Headers);
                foreach (var header in headers)
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }

            // Set content type
            response.ContentType = rule.ContentType ?? "application/json";

            // Write response body
            var buffer = Encoding.UTF8.GetBytes(rule.ResponseBody ?? "");
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

            // Log the response
            var responseModel = new HttpResponse
            {
                StatusCode = rule.StatusCode,
                Headers = rule.Headers,
                Body = rule.ResponseBody,
                Timestamp = DateTime.UtcNow,
                RequestId = requestModel.Id,
                IsFromRule = true,
                RuleId = rule.Id
            };

            dbContext.Responses.Add(responseModel);
            await dbContext.SaveChangesAsync();
        }
        finally
        {
            response.Close();
        }
    }

    private string SerializeHeaders(WebHeaderCollection headers)
    {
        var dict = new Dictionary<string, string>();
        foreach (string key in headers.Keys)
        {
            dict[key] = headers[key];
        }
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }

    private async Task<string> ReadRequestBodyAsync(HttpListenerRequest request)
    {
        if (!request.HasEntityBody)
        {
            return string.Empty;
        }

        using var bodyStream = request.InputStream;
        using var reader = new StreamReader(bodyStream, request.ContentEncoding);
        return await reader.ReadToEndAsync();
    }

    public override void Dispose()
    {
        if (_listener != null)
        {
            if (_listener.IsListening)
            {
                _listener.Stop();
            }
            _listener.Close();
        }
        
        base.Dispose();
    }
}
