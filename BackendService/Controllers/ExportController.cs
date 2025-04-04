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
        /// Esporta le richieste HTTP in formato JSON
        /// </summary>
        [HttpGet("json")]
        public async Task<IActionResult> ExportAsJson(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? method = null,
            [FromQuery] string? host = null)
        {
            try
            {
                _logger.LogInformation("Richiesta di esportazione JSON ricevuta");
                
                var fileBytes = await _exportService.ExportRequestsAsJsonAsync(
                    fromDate, toDate, method, host);
                
                var fileName = $"requests_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                
                return File(fileBytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione JSON");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Esporta le richieste HTTP in formato CSV
        /// </summary>
        [HttpGet("csv")]
        public async Task<IActionResult> ExportAsCsv(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? method = null,
            [FromQuery] string? host = null)
        {
            try
            {
                _logger.LogInformation("Richiesta di esportazione CSV ricevuta");
                
                var fileBytes = await _exportService.ExportRequestsAsCsvAsync(
                    fromDate, toDate, method, host);
                
                var fileName = $"requests_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                
                return File(fileBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione CSV");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Esporta i dettagli di una specifica richiesta HTTP
        /// </summary>
        [HttpGet("request/{id}")]
        public async Task<IActionResult> ExportRequestDetails(int id)
        {
            try
            {
                _logger.LogInformation($"Richiesta di esportazione dettagli richiesta ID {id}");
                
                var fileBytes = await _exportService.ExportRequestDetailsAsJsonAsync(id);
                
                var fileName = $"request_details_{id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
                
                return File(fileBytes, "application/json", fileName);
            }
            catch (Exception ex) when (ex is KeyNotFoundException)
            {
                _logger.LogWarning($"Tentativo di esportare dettagli di una richiesta inesistente: {id}");
                return NotFound($"Richiesta con ID {id} non trovata");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante l'esportazione dei dettagli della richiesta {id}");
                return StatusCode(500, $"Errore interno del server: {ex.Message}");
            }
        }
    }
}