using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/export")]
    public class ExportController : ControllerBase
    {
        private readonly ExportService _exportService;
        private readonly ILogger<ExportController> _logger;
        
        public ExportController(ExportService exportService, ILogger<ExportController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }
        
        /// <summary>
        /// Esporta le richieste in formato JSON
        /// </summary>
        [HttpGet("json")]
        public async Task<FileResult> ExportAsJson(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string method = null,
            [FromQuery] string host = null)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta di esportazione JSON");
                
                var data = await _exportService.ExportRequestsAsJsonAsync(
                    page, pageSize, fromDate, toDate, method, host);
                
                return File(data, "application/json", "requests_export.json");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante l'esportazione JSON: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Esporta le richieste in formato CSV
        /// </summary>
        [HttpGet("csv")]
        public async Task<FileResult> ExportAsCsv(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string method = null,
            [FromQuery] string host = null)
        {
            try
            {
                _logger.LogInformation("Ricevuta richiesta di esportazione CSV");
                
                var data = await _exportService.ExportRequestsAsCsvAsync(
                    fromDate, toDate, method, host);
                
                return File(data, "text/csv", "requests_export.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante l'esportazione CSV: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Esporta i dettagli di una singola richiesta
        /// </summary>
        [HttpGet("request/{id}")]
        public async Task<FileResult> ExportRequestDetails(int id)
        {
            try
            {
                _logger.LogInformation($"Ricevuta richiesta di esportazione dettagli per richiesta {id}");
                
                var data = await _exportService.ExportRequestDetailsAsJsonAsync(id);
                
                return File(data, "application/json", $"request_{id}_details.json");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"Richiesta non trovata con ID {id}: {ex.Message}");
                throw new Exception($"Richiesta con ID {id} non trovata", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore durante l'esportazione dei dettagli della richiesta {id}: {ex.Message}");
                throw;
            }
        }
    }
}