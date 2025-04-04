using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BackendService.Models;
using HttpRequestModel = BackendService.Models.HttpRequest;

namespace BackendService
{
    public class ExportService
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<ExportService> _logger;
        
        public ExportService(DatabaseContext dbContext, ILogger<ExportService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        
        /// <summary>
        /// Esporta le richieste HTTP come JSON in base ai filtri specificati
        /// </summary>
        public async Task<byte[]> ExportRequestsAsJsonAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            string? method = null, 
            string? host = null)
        {
            _logger.LogInformation("Esportazione richieste come JSON");
            
            try
            {
                var requests = await GetFilteredRequestsAsync(fromDate, toDate, method, host);
                
                // Semplifica i dati per l'esportazione, escludendo le proprietà cicliche
                var exportData = requests.Select(r => new
                {
                    r.Id,
                    r.Url,
                    r.Method,
                    Host = ExtractHost(r.Url),
                    Path = ExtractPath(r.Url),
                    QueryString = ExtractQueryString(r.Url),
                    r.Headers,
                    r.Body,
                    ContentType = ExtractContentType(r.Headers),
                    Date = DateTime.TryParse(r.Timestamp, out var timestamp) ? timestamp : DateTime.UtcNow,
                    r.IsProxied,
                    ResponseStatusCode = r.Response?.StatusCode,
                    ResponseHeaders = r.Response?.Headers,
                    ResponseBody = r.Response?.Body,
                    ResponseContentType = ExtractContentType(r.Response?.Headers)
                });
                
                var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione JSON");
                throw;
            }
        }
        
        /// <summary>
        /// Esporta le richieste HTTP come CSV in base ai filtri specificati
        /// </summary>
        public async Task<byte[]> ExportRequestsAsCsvAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            string? method = null, 
            string? host = null)
        {
            _logger.LogInformation("Esportazione richieste come CSV");
            
            try
            {
                var requests = await GetFilteredRequestsAsync(fromDate, toDate, method, host);
                
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
                
                // Intestazioni CSV
                writer.WriteLine("ID,Data,Metodo,Host,Percorso,IsProxied,Stato Risposta");
                
                // Dati
                foreach (var request in requests)
                {
                    var statusCode = request.Response != null ? request.Response.StatusCode.ToString() : "N/A";
                    var timestamp = DateTime.TryParse(request.Timestamp, out var parsedDate) ? parsedDate : DateTime.UtcNow;
                    var date = timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    var urlHost = ExtractHost(request.Url);
                    var path = EscapeCsvField(ExtractPath(request.Url) ?? "");
                    
                    writer.WriteLine($"{request.Id},{date},{request.Method},{urlHost},{path},{request.IsProxied},{statusCode}");
                }
                
                writer.Flush();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione CSV");
                throw;
            }
        }
        
        /// <summary>
        /// Esporta i dettagli di una specifica richiesta HTTP come JSON
        /// </summary>
        public async Task<byte[]> ExportRequestDetailsAsJsonAsync(int requestId)
        {
            _logger.LogInformation($"Esportazione dettagli della richiesta {requestId}");
            
            try
            {
                var request = await _dbContext.Requests
                    .Include(r => r.Response)
                    .FirstOrDefaultAsync(r => r.Id == requestId);
                    
                if (request == null)
                {
                    throw new KeyNotFoundException($"Richiesta con ID {requestId} non trovata");
                }
                
                // Crea un oggetto semplificato con i dati di richiesta e risposta
                var exportData = new
                {
                    request = new
                    {
                        request.Id,
                        request.Url,
                        request.Method,
                        Host = ExtractHost(request.Url),
                        Path = ExtractPath(request.Url),
                        QueryString = ExtractQueryString(request.Url),
                        request.Headers,
                        request.Body,
                        ContentType = ExtractContentType(request.Headers),
                        Date = DateTime.TryParse(request.Timestamp, out var parsedDate) ? parsedDate : DateTime.UtcNow,
                        request.IsProxied
                    },
                    response = request.Response != null ? new
                    {
                        request.Response.Id,
                        request.Response.StatusCode,
                        request.Response.Headers,
                        request.Response.Body,
                        ContentType = ExtractContentType(request.Response.Headers),
                        Date = DateTime.TryParse(request.Response.Timestamp, out var respDate) ? respDate : DateTime.UtcNow
                    } : null
                };
                
                var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                return Encoding.UTF8.GetBytes(json);
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException))
            {
                _logger.LogError(ex, $"Errore durante l'esportazione dei dettagli della richiesta {requestId}");
                throw;
            }
        }
        
        /// <summary>
        /// Ottiene le richieste filtrate dal database
        /// </summary>
        private async Task<List<HttpRequestModel>> GetFilteredRequestsAsync(
            DateTime? fromDate, 
            DateTime? toDate, 
            string? method, 
            string? host)
        {
            var query = _dbContext.Requests
                .Include(r => r.Response)
                .AsQueryable();
                
            // Applica i filtri
            if (fromDate.HasValue)
            {
                var fromDateStr = fromDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                query = query.Where(r => r.Timestamp.CompareTo(fromDateStr) >= 0);
            }
            
            if (toDate.HasValue)
            {
                var toDateStr = toDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                query = query.Where(r => r.Timestamp.CompareTo(toDateStr) <= 0);
            }
            
            if (!string.IsNullOrEmpty(method))
            {
                query = query.Where(r => r.Method.ToUpper() == method.ToUpper());
            }
            
            if (!string.IsNullOrEmpty(host))
            {
                query = query.Where(r => r.Url.Contains(host));
            }
            
            // Ordina per data (più recenti prima)
            query = query.OrderByDescending(r => r.Timestamp);
            
            return await query.ToListAsync();
        }
        
        /// <summary>
        /// Prepara un campo per l'esportazione CSV
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return "";
            }
            
            // Se il campo contiene virgole, virgolette o ritorni a capo, racchiudilo tra virgolette
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                // Sostituisci le virgolette con due virgolette (standard CSV)
                field = field.Replace("\"", "\"\"");
                // Racchiudi tra virgolette
                return $"\"{field}\"";
            }
            
            return field;
        }
        
        /// <summary>
        /// Estrae l'host dall'URL
        /// </summary>
        private string ExtractHost(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }
            
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return uri.Host;
                }
                
                // Fallback per URL relativi o invalidi
                var parts = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length > 0 ? parts[0] : string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Errore durante l'estrazione dell'host dall'URL: {url}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Estrae il percorso dall'URL
        /// </summary>
        private string ExtractPath(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }
            
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return uri.AbsolutePath;
                }
                
                // Fallback per URL relativi o invalidi
                var questionMarkIndex = url.IndexOf('?');
                var pathWithoutQuery = questionMarkIndex >= 0 ? url.Substring(0, questionMarkIndex) : url;
                
                var parts = pathWithoutQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= 1)
                {
                    return "/";
                }
                
                return "/" + string.Join("/", parts.Skip(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Errore durante l'estrazione del percorso dall'URL: {url}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Estrae la query string dall'URL
        /// </summary>
        private string ExtractQueryString(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }
            
            try
            {
                var questionMarkIndex = url.IndexOf('?');
                if (questionMarkIndex >= 0 && questionMarkIndex < url.Length - 1)
                {
                    return url.Substring(questionMarkIndex);
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Errore durante l'estrazione della query string dall'URL: {url}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Estrae il content type dagli headers
        /// </summary>
        private string ExtractContentType(string headers)
        {
            if (string.IsNullOrEmpty(headers))
            {
                return string.Empty;
            }
            
            try
            {
                const string contentTypeHeader = "Content-Type:";
                var lines = headers.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith(contentTypeHeader, StringComparison.OrdinalIgnoreCase))
                    {
                        return trimmedLine.Substring(contentTypeHeader.Length).Trim();
                    }
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Errore durante l'estrazione del Content-Type dagli headers");
                return string.Empty;
            }
        }
    }
}