using System;
using System.Collections.Generic;
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
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<ExportService> _logger;

        public ExportService(
            DatabaseContext dbContext,
            ILogger<ExportService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Verifica se esiste una richiesta con l'ID specificato
        /// </summary>
        public async Task<bool> RequestExistsAsync(int requestId)
        {
            return await _dbContext.HttpRequests.AnyAsync(r => r.Id == requestId);
        }

        /// <summary>
        /// Esporta le richieste in formato JSON con filtri opzionali
        /// </summary>
        public async Task<byte[]> ExportAsJsonAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? method = null,
            string? host = null)
        {
            try
            {
                _logger.LogInformation("Generazione export JSON iniziata");
                
                var query = BuildFilteredQuery(startDate, endDate, method, host);
                
                // Include dati correlati
                var requests = await query
                    .Include(r => r.Response)
                    .Select(r => new {
                        r.Id,
                        r.Method,
                        r.Url,
                        r.Host,
                        r.Path,
                        r.QueryString,
                        r.ContentType,
                        r.IsHttps,
                        r.Timestamp,
                        Headers = r.Headers,
                        Body = r.Body,
                        Response = r.Response != null ? new {
                            r.Response.Id,
                            r.Response.StatusCode,
                            r.Response.ContentType,
                            Headers = r.Response.Headers,
                            Body = r.Response.Body,
                            r.Response.Timestamp
                        } : null
                    })
                    .ToListAsync();
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(new {
                    TotalCount = requests.Count,
                    ExportTimestamp = DateTime.UtcNow,
                    Requests = requests
                }, options);
                
                _logger.LogInformation($"Export JSON completato: {requests.Count} richieste esportate");
                
                return jsonBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione JSON");
                throw;
            }
        }

        /// <summary>
        /// Esporta le richieste in formato CSV con filtri opzionali
        /// </summary>
        public async Task<byte[]> ExportAsCsvAsync(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? method = null,
            string? host = null)
        {
            try
            {
                _logger.LogInformation("Generazione export CSV iniziata");
                
                var query = BuildFilteredQuery(startDate, endDate, method, host);
                
                var requests = await query
                    .Include(r => r.Response)
                    .ToListAsync();
                
                var csv = new StringBuilder();
                
                // Intestazioni CSV
                csv.AppendLine("Id,Method,Url,Host,Path,IsHttps,ContentType,StatusCode,ResponseContentType,Timestamp");
                
                foreach (var request in requests)
                {
                    csv.AppendLine(string.Join(",",
                        request.Id,
                        EscapeCsvField(request.Method),
                        EscapeCsvField(request.Url),
                        EscapeCsvField(request.Host),
                        EscapeCsvField(request.Path),
                        request.IsHttps,
                        EscapeCsvField(request.ContentType),
                        request.Response?.StatusCode,
                        EscapeCsvField(request.Response?.ContentType),
                        request.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                    ));
                }
                
                _logger.LogInformation($"Export CSV completato: {requests.Count} richieste esportate");
                
                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione CSV");
                throw;
            }
        }

        /// <summary>
        /// Esporta i dettagli di una singola richiesta in formato JSON
        /// </summary>
        public async Task<byte[]> ExportRequestDetailsAsync(int requestId)
        {
            try
            {
                _logger.LogInformation($"Esportazione dettagli richiesta {requestId} iniziata");
                
                var request = await _dbContext.HttpRequests
                    .Include(r => r.Response)
                    .FirstOrDefaultAsync(r => r.Id == requestId);
                
                if (request == null)
                {
                    throw new KeyNotFoundException($"Richiesta con ID {requestId} non trovata");
                }
                
                var detailsObject = new {
                    Request = new {
                        request.Id,
                        request.Method,
                        request.Url,
                        request.Host,
                        request.Path,
                        request.QueryString,
                        request.ContentType,
                        request.IsHttps,
                        request.Timestamp,
                        Headers = ParseHeaders(request.Headers),
                        Body = request.Body
                    },
                    Response = request.Response != null ? new {
                        request.Response.Id,
                        request.Response.StatusCode,
                        request.Response.ContentType,
                        Headers = ParseHeaders(request.Response.Headers),
                        Body = request.Response.Body,
                        request.Response.Timestamp
                    } : null
                };
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(detailsObject, options);
                
                _logger.LogInformation($"Esportazione dettagli richiesta {requestId} completata");
                
                return jsonBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante l'esportazione dei dettagli della richiesta {requestId}");
                throw;
            }
        }

        /// <summary>
        /// Costruisce una query filtrata per le richieste HTTP
        /// </summary>
        private IQueryable<Models.HttpRequest> BuildFilteredQuery(
            DateTime? startDate,
            DateTime? endDate,
            string? method,
            string? host)
        {
            var query = _dbContext.HttpRequests.AsQueryable();
            
            if (startDate.HasValue)
            {
                query = query.Where(r => r.Timestamp >= startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                // Aggiungi un giorno per includere tutto il giorno finale
                var adjustedEndDate = endDate.Value.AddDays(1).AddSeconds(-1);
                query = query.Where(r => r.Timestamp <= adjustedEndDate);
            }
            
            if (!string.IsNullOrWhiteSpace(method))
            {
                query = query.Where(r => r.Method.ToUpper() == method.ToUpper());
            }
            
            if (!string.IsNullOrWhiteSpace(host))
            {
                query = query.Where(r => r.Host.Contains(host));
            }
            
            // Ordina per timestamp decrescente (più recenti prima)
            return query.OrderByDescending(r => r.Timestamp);
        }

        /// <summary>
        /// Escape dei campi CSV per gestire virgole e apici
        /// </summary>
        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }
            
            // Se contiene virgole, virgolette o nuove righe, racchiudi tra virgolette e raddoppia le virgolette interne
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            
            return field;
        }

        /// <summary>
        /// Analizza le intestazioni HTTP da stringa a dizionario
        /// </summary>
        private Dictionary<string, string> ParseHeaders(string? headersString)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrEmpty(headersString))
            {
                return headers;
            }
            
            try
            {
                var headerLines = headersString.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in headerLines)
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var key = line.Substring(0, colonIndex).Trim();
                        var value = line.Substring(colonIndex + 1).Trim();
                        
                        if (!headers.ContainsKey(key))
                        {
                            headers.Add(key, value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore durante l'analisi delle intestazioni HTTP");
            }
            
            return headers;
        }
    }
}