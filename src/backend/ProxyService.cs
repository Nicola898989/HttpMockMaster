using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

public class ProxyService
{
    private readonly ILogger<ProxyService> _logger;
    private readonly HttpClient _httpClient;
    private string _targetDomain = string.Empty;
    private bool _mockMode = false;

    public ProxyService(ILogger<ProxyService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        });
    }

    public void SetTargetDomain(string domain)
    {
        _targetDomain = domain.TrimEnd('/');
    }

    public void SetMockMode(bool enabled)
    {
        _mockMode = enabled;
    }

    public async Task ProxyRequestAsync(HttpListenerContext context, HttpRequest requestRecord, AppDbContext dbContext)
    {
        if (string.IsNullOrEmpty(_targetDomain) && !_mockMode)
        {
            await SendErrorResponseAsync(context.Response, "Proxy target domain not configured", HttpStatusCode.BadGateway);
            return;
        }

        if (_mockMode)
        {
            // In mock mode, try to find a matching rule
            var rule = await dbContext.Rules
                .Where(r => context.Request.Url.PathAndQuery.Contains(r.PathPattern))
                .FirstOrDefaultAsync();

            if (rule != null)
            {
                await SendMockResponseAsync(context.Response, rule, requestRecord, dbContext);
                return;
            }
            else
            {
                await SendErrorResponseAsync(context.Response, "No mock rule found for this request", HttpStatusCode.NotFound);
                return;
            }
        }

        try
        {
            // Create the proxy request
            var targetUrl = BuildTargetUrl(context.Request);
            var proxyRequest = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.HttpMethod),
                RequestUri = new Uri(targetUrl)
            };

            // Copy headers from original request to proxy request
            foreach (string headerName in context.Request.Headers.AllKeys)
            {
                if (headerName.ToLower() != "host" && headerName.ToLower() != "content-length")
                {
                    proxyRequest.Headers.TryAddWithoutValidation(headerName, context.Request.Headers[headerName]);
                }
            }

            // Copy the body from the original request
            if (context.Request.HasEntityBody)
            {
                using var requestStream = new MemoryStream();
                await context.Request.InputStream.CopyToAsync(requestStream);
                requestStream.Position = 0;
                proxyRequest.Content = new StreamContent(requestStream);
                
                // Set content type if it exists
                if (context.Request.ContentType != null)
                {
                    proxyRequest.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
                }
            }

            // Send the proxy request
            var proxyResponse = await _httpClient.SendAsync(proxyRequest);

            // Create response record
            var responseRecord = new HttpResponse
            {
                StatusCode = (int)proxyResponse.StatusCode,
                Headers = SerializeHeaders(proxyResponse.Headers),
                Timestamp = DateTime.UtcNow,
                RequestId = requestRecord.Id,
                IsFromRule = false
            };

            // Copy the response body
            var responseBody = await proxyResponse.Content.ReadAsByteArrayAsync();
            responseRecord.Body = Encoding.UTF8.GetString(responseBody);

            // Store response in database
            dbContext.Responses.Add(responseRecord);
            await dbContext.SaveChangesAsync();

            // Forward the response back to the client
            context.Response.StatusCode = (int)proxyResponse.StatusCode;
            
            // Copy headers from proxy response to original response
            foreach (var header in proxyResponse.Headers)
            {
                if (header.Key.ToLower() != "transfer-encoding")
                {
                    context.Response.Headers.Add(header.Key, string.Join(", ", header.Value));
                }
            }

            foreach (var header in proxyResponse.Content.Headers)
            {
                context.Response.Headers.Add(header.Key, string.Join(", ", header.Value));
            }

            // Write the response body
            context.Response.ContentLength64 = responseBody.Length;
            await context.Response.OutputStream.WriteAsync(responseBody, 0, responseBody.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in proxy request");
            await SendErrorResponseAsync(context.Response, $"Proxy error: {ex.Message}", HttpStatusCode.BadGateway);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private string BuildTargetUrl(HttpListenerRequest request)
    {
        var originUrl = request.Url;
        var targetUrl = $"{_targetDomain}{originUrl.PathAndQuery}";
        return targetUrl;
    }

    private async Task SendMockResponseAsync(HttpListenerResponse response, Rule rule, HttpRequest requestRecord, AppDbContext dbContext)
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
                RequestId = requestRecord.Id,
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

    private async Task SendErrorResponseAsync(HttpListenerResponse response, string message, HttpStatusCode statusCode)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json";
        
        var errorJson = $"{{\"error\": \"{message}\"}}";
        var buffer = Encoding.UTF8.GetBytes(errorJson);
        
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.Close();
    }

    private string SerializeHeaders(System.Net.Http.Headers.HttpHeaders headers)
    {
        var dict = headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }
}
