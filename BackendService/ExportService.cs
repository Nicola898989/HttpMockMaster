using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackendService
{
    public class ExportService
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<ExportService> _logger;
        
        public ExportService(DatabaseContext context, ILogger<ExportService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        /// <summary>
        /// Esporta le richieste in formato JSON con filtri opzionali
        /// </summary>
        public async Task<byte[]> ExportRequestsAsJsonAsync(
            int? page = null, 
            int? pageSize = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            string method = null, 
            string host = null)
        {
            try
            {
                _logger.LogInformation("Esportazione richieste in formato JSON");
                
                var requests = await GetFilteredRequests(fromDate, toDate, method, host)
                    .OrderByDescending(r => r.Timestamp)
                    .Select(r => new 
                    {
                        r.Id,
                        r.Method,
                        r.Url,
                        r.Host,
                        r.Path,
                        r.QueryString,
                        r.Headers,
                        r.Timestamp,
                        r.IsProxied,
                        ContentSize = r.Content?.Length ?? 0,
                        r.ContentType,
                        HasResponse = r.Response != null,
                        ResponseStatus = r.Response != null ? r.Response.StatusCode : null,
                        ResponseTime = r.Response != null ? r.Response.ResponseTime : null
                    })
                    .ToListAsync();
                
                _logger.LogInformation($"Esportate {requests.Count} richieste in formato JSON");
                
                // Paginazione se richiesta
                if (page.HasValue && pageSize.HasValue)
                {
                    int skip = (page.Value - 1) * pageSize.Value;
                    requests = requests.Skip(skip).Take(pageSize.Value).ToList();
                    _logger.LogInformation($"Applicata paginazione (page: {page}, pageSize: {pageSize}, records: {requests.Count})");
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                return JsonSerializer.SerializeToUtf8Bytes(requests, options);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante l'esportazione JSON: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Esporta le richieste in formato CSV con filtri opzionali
        /// </summary>
        public async Task<byte[]> ExportRequestsAsCsvAsync(
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            string method = null, 
            string host = null)
        {
            try
            {
                _logger.LogInformation("Esportazione richieste in formato CSV");
                
                var requests = await GetFilteredRequests(fromDate, toDate, method, host)
                    .OrderByDescending(r => r.Timestamp)
                    .Select(r => new 
                    {
                        r.Id,
                        r.Method,
                        r.Url,
                        r.Host,
                        r.Path,
                        r.Timestamp,
                        r.IsProxied,
                        ContentSize = r.Content?.Length ?? 0,
                        r.ContentType,
                        ResponseStatus = r.Response != null ? r.Response.StatusCode : null,
                        ResponseTime = r.Response != null ? r.Response.ResponseTime : null
                    })
                    .ToListAsync();
                
                _logger.LogInformation($"Esportate {requests.Count} richieste in formato CSV");
                
                // Creazione dell'header CSV
                var csv = new StringBuilder();
                csv.AppendLine("ID,Method,URL,Host,Path,Timestamp,IsProxied,ContentSize,ContentType,ResponseStatus,ResponseTime");
                
                // Aggiunta delle righe di dati
                foreach (var req in requests)
                {
                    csv.AppendLine(string.Join(",", 
                        EscapeCsvField(req.Id.ToString()),
                        EscapeCsvField(req.Method),
                        EscapeCsvField(req.Url),
                        EscapeCsvField(req.Host),
                        EscapeCsvField(req.Path),
                        EscapeCsvField(req.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                        EscapeCsvField(req.IsProxied.ToString()),
                        EscapeCsvField(req.ContentSize.ToString()),
                        EscapeCsvField(req.ContentType),
                        EscapeCsvField(req.ResponseStatus?.ToString() ?? ""),
                        EscapeCsvField(req.ResponseTime?.ToString() ?? "")
                    ));
                }
                
                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante l'esportazione CSV: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Esporta i dettagli di una specifica richiesta in formato JSON
        /// </summary>
        public async Task<byte[]> ExportRequestDetailsAsJsonAsync(int requestId)
        {
            try
            {
                _logger.LogInformation($"Esportazione dettagli richiesta ID: {requestId}");
                
                var request = await _context.Requests
                    .Include(r => r.Response)
                    .FirstOrDefaultAsync(r => r.Id == requestId);
                
                if (request == null)
                {
                    _logger.LogWarning($"Richiesta con ID {requestId} non trovata");
                    throw new KeyNotFoundException($"Request with ID {requestId} not found");
                }
                
                var requestDetails = new
                {
                    Request = new
                    {
                        request.Id,
                        request.Method,
                        request.Url,
                        request.Host,
                        request.Path,
                        request.QueryString,
                        Headers = ParseHeaders(request.Headers),
                        request.Timestamp,
                        request.IsProxied,
                        Content = request.Content != null ? System.Text.Encoding.UTF8.GetString(request.Content) : null,
                        request.ContentType
                    },
                    Response = request.Response != null ? new
                    {
                        request.Response.Id,
                        request.Response.StatusCode,
                        Headers = ParseHeaders(request.Response.Headers),
                        Content = request.Response.Content != null ? System.Text.Encoding.UTF8.GetString(request.Response.Content) : null,
                        request.Response.ContentType,
                        request.Response.ResponseTime
                    } : null
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                return JsonSerializer.SerializeToUtf8Bytes(requestDetails, options);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante l'esportazione dei dettagli della richiesta {requestId}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Ottiene le richieste filtrate in base ai parametri specificati
        /// </summary>
        private IQueryable<Models.HttpRequest> GetFilteredRequests(
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            string method = null, 
            string host = null)
        {
            var query = _context.Requests
                .Include(r => r.Response)
                .AsQueryable();
            
            if (fromDate.HasValue)
            {
                query = query.Where(r => r.Timestamp >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                // Impostiamo l'ora a fine giornata
                var endOfDay = toDate.Value.Date.AddDays(1).AddMilliseconds(-1);
                query = query.Where(r => r.Timestamp <= endOfDay);
            }
            
            if (!string.IsNullOrEmpty(method))
            {
                query = query.Where(r => r.Method.ToUpper() == method.ToUpper());
            }
            
            if (!string.IsNullOrEmpty(host))
            {
                query = query.Where(r => r.Host.Contains(host));
            }
            
            return query;
        }
        
        /// <summary>
        /// Converte l'header da stringa in un dizionario di chiave-valore
        /// </summary>
        private Dictionary<string, string> ParseHeaders(string headers)
        {
            if (string.IsNullOrEmpty(headers))
            {
                return new Dictionary<string, string>();
            }
            
            try
            {
                var headerDict = new Dictionary<string, string>();
                var headerLines = headers.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in headerLines)
                {
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        string key = line.Substring(0, colonIndex).Trim();
                        string value = colonIndex < line.Length - 1 
                            ? line.Substring(colonIndex + 1).Trim() 
                            : string.Empty;
                        
                        if (!headerDict.ContainsKey(key))
                        {
                            headerDict.Add(key, value);
                        }
                    }
                }
                
                return headerDict;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore durante il parsing degli headers: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }
        
        /// <summary>
        /// Funzione per l'escape dei campi CSV
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return "";
            }
            
            bool containsSpecialChars = field.Contains(',') || field.Contains('"') || field.Contains('\n');
            if (containsSpecialChars)
            {
                // Sostituzione dei doppi apici con doppi apici doppi (escape CSV standard)
                field = field.Replace("\"", "\"\"");
                // Racchiusione in doppi apici
                return $"\"{field}\"";
            }
            
            return field;
        }
    }
}