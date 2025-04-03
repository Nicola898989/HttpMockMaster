using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using BackendService.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace BackendService
{
    public class ProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<ProxyService> _logger;
        private readonly RuleService _ruleService;

        public ProxyService(HttpClient httpClient, DatabaseContext dbContext, ILogger<ProxyService> logger, RuleService ruleService)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
            _ruleService = ruleService;
        }

        public async Task<Models.HttpResponse> ForwardRequestAsync(HttpListenerRequest request, string targetDomain)
        {
            try
            {
                // Check if there's a rule matching this request
                var rule = await _ruleService.FindMatchingRuleAsync(request);
                if (rule != null)
                {
                    _logger.LogInformation($"Rule match found for {request.Url.PathAndQuery}. Using mocked response.");
                    return rule.Response;
                }

                // Build the target URL
                var targetUrl = new UriBuilder
                {
                    Scheme = targetDomain.StartsWith("https") ? "https" : "http",
                    Host = targetDomain.Replace("https://", "").Replace("http://", ""),
                    Path = request.Url.AbsolutePath,
                    Query = request.Url.Query.TrimStart('?')
                }.Uri;

                // Create the HTTP request
                var requestMessage = new HttpRequestMessage
                {
                    Method = new HttpMethod(request.HttpMethod),
                    RequestUri = targetUrl
                };

                // Copy the headers (excluding some that cause issues with proxying)
                foreach (string headerName in request.Headers.AllKeys)
                {
                    if (!headerName.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                        !headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        requestMessage.Headers.TryAddWithoutValidation(headerName, request.Headers[headerName]);
                    }
                }

                // Copy the body content if there is any
                if (request.HasEntityBody)
                {
                    using (var bodyStream = request.InputStream)
                    {
                        var bodyBytes = new byte[request.ContentLength64];
                        await bodyStream.ReadAsync(bodyBytes, 0, bodyBytes.Length);
                        requestMessage.Content = new ByteArrayContent(bodyBytes);

                        // Set content type
                        if (!string.IsNullOrEmpty(request.ContentType))
                        {
                            requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
                        }
                    }
                }

                // Record the request
                var requestModel = new Models.HttpRequest
                {
                    Url = request.Url.ToString(),
                    Method = request.HttpMethod,
                    Headers = SerializeHeaders(request.Headers),
                    Body = await GetRequestBodyAsync(request),
                    Timestamp = DateTime.UtcNow,
                    IsProxied = true,
                    TargetDomain = targetDomain
                };
                _dbContext.Requests.Add(requestModel);
                await _dbContext.SaveChangesAsync();

                // Send the request
                var response = await _httpClient.SendAsync(requestMessage);

                // Record the response
                var responseModel = new Models.HttpResponse
                {
                    RequestId = requestModel.Id,
                    StatusCode = (int)response.StatusCode,
                    Headers = SerializeHeaders(response.Headers, response.Content.Headers),
                    Body = await response.Content.ReadAsStringAsync(),
                    Timestamp = DateTime.UtcNow
                };
                _dbContext.Responses.Add(responseModel);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Proxied request to {targetUrl}. Status code: {response.StatusCode}");
                return responseModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error proxying request to {targetDomain}");
                throw;
            }
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

        private string SerializeHeaders(System.Net.Http.Headers.HttpResponseHeaders headers, 
                                     System.Net.Http.Headers.HttpContentHeaders contentHeaders)
        {
            var builder = new StringBuilder();
            foreach (var header in headers)
            {
                builder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            foreach (var header in contentHeaders)
            {
                builder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            return builder.ToString();
        }
    }
}
