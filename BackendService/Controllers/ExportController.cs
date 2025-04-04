using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExportController : ControllerBase
    {
        private readonly ExportService _exportService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(
            ExportService exportService,
            ILogger<ExportController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }

        /// <summary>
        /// Esporta le richieste in formato JSON con filtri opzionali
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
                
                var data = await _exportService.ExportAsJsonAsync(fromDate, toDate, method, host);
                
                return File(data, "application/json", $"http_requests_{DateTime.UtcNow:yyyyMMdd}.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione in JSON");
                return StatusCode(500, $"Errore durante l'esportazione: {ex.Message}");
            }
        }

        /// <summary>
        /// Esporta le richieste in formato CSV con filtri opzionali
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
                
                var data = await _exportService.ExportAsCsvAsync(fromDate, toDate, method, host);
                
                return File(data, "text/csv", $"http_requests_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante l'esportazione in CSV");
                return StatusCode(500, $"Errore durante l'esportazione: {ex.Message}");
            }
        }

        /// <summary>
        /// Esporta i dettagli di una singola richiesta in formato JSON
        /// </summary>
        [HttpGet("request/{id}")]
        public async Task<IActionResult> ExportRequestDetails(int id)
        {
            try
            {
                _logger.LogInformation($"Richiesta di esportazione dettagli per ID {id} ricevuta");
                
                if (!await _exportService.RequestExistsAsync(id))
                {
                    return NotFound($"La richiesta con ID {id} non esiste");
                }
                
                var data = await _exportService.ExportRequestDetailsAsync(id);
                
                return File(data, "application/json", $"request_details_{id}.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante l'esportazione dei dettagli della richiesta {id}");
                return StatusCode(500, $"Errore durante l'esportazione: {ex.Message}");
            }
        }
    }
}