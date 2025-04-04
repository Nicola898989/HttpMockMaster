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
                    r.Host,
                    r.Path,
                    r.QueryString,
                    r.Headers,
                    r.Body,
                    r.ContentType,
                    r.Date,
                    r.IsProxied,
                    ResponseStatusCode = r.Response != null ? r.Response.StatusCode : null,
                    ResponseHeaders = r.Response != null ? r.Response.Headers : null,
                    ResponseBody = r.Response != null ? r.Response.Body : null,
                    ResponseContentType = r.Response != null ? r.Response.ContentType : null
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
                    var date = request.Date.ToString("yyyy-MM-dd HH:mm:ss");
                    var path = EscapeCsvField(request.Path ?? "");
                    
                    writer.WriteLine($"{request.Id},{date},{request.Method},{request.Host},{path},{request.IsProxied},{statusCode}");
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
                        request.Host,
                        request.Path,
                        request.QueryString,
                        request.Headers,
                        request.Body,
                        request.ContentType,
                        request.Date,
                        request.IsProxied
                    },
                    response = request.Response != null ? new
                    {
                        request.Response.Id,
                        request.Response.StatusCode,
                        request.Response.Headers,
                        request.Response.Body,
                        request.Response.ContentType,
                        request.Response.Date
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
        private async Task<List<HttpRequest>> GetFilteredRequestsAsync(
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
                query = query.Where(r => r.Date >= fromDate.Value);
            }
            
            if (toDate.HasValue)
            {
                query = query.Where(r => r.Date <= toDate.Value);
            }
            
            if (!string.IsNullOrEmpty(method))
            {
                query = query.Where(r => r.Method.ToUpper() == method.ToUpper());
            }
            
            if (!string.IsNullOrEmpty(host))
            {
                query = query.Where(r => r.Host.Contains(host));
            }
            
            // Ordina per data (più recenti prima)
            query = query.OrderByDescending(r => r.Date);
            
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
    }
}