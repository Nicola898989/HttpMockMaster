using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackendService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ModelHttpResponse = BackendService.Models.HttpResponse;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComparisonController : ControllerBase
    {
        private readonly IComparisonService _comparisonService;
        private readonly ILogger<ComparisonController> _logger;
        private readonly DatabaseContext _dbContext;

        public ComparisonController(IComparisonService comparisonService, ILogger<ComparisonController> logger, DatabaseContext dbContext)
        {
            _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Confronta due o più richieste HTTP
        /// </summary>
        /// <param name="requestIds">Lista di ID delle richieste da confrontare</param>
        /// <returns>Risultato del confronto</returns>
        [HttpPost("requests")]
        public async Task<ActionResult<ComparisonResult>> Compare([FromBody] List<int> requestIds)
        {
            try
            {
                _logger.LogInformation("Richiesta di confronto per le richieste con ID: {RequestIds}", string.Join(", ", requestIds));
                
                var result = await _comparisonService.CompareRequests(requestIds);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Errore nei parametri di confronto");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il confronto delle richieste");
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }

        /// <summary>
        /// Confronta due stringhe JSON
        /// </summary>
        /// <param name="request">Richiesta contenente le due stringhe JSON da confrontare</param>
        /// <returns>Le differenze rilevate tra i due JSON</returns>
        [HttpPost("json")]
        public async Task<ActionResult<Dictionary<string, object>>> CompareJson([FromBody] JsonDiffRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Richiesta di confronto JSON");
                
                var result = _comparisonService.GetJsonDiff(request.Json1, request.Json2);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il confronto JSON");
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }

            // Task.CompletedTask è usato solo per mantenere il metodo asincrono
            // per consistenza con gli altri metodi del controller.
            // In questo caso, l'operazione è sincrona.
            await Task.CompletedTask;
        }

        /// <summary>
        /// Confronta due risposte HTTP
        /// </summary>
        /// <param name="responseId1">ID della prima risposta</param>
        /// <param name="responseId2">ID della seconda risposta</param>
        /// <returns>Le differenze rilevate tra le due risposte</returns>
        [HttpGet("responses")]
        public async Task<ActionResult<Dictionary<string, object>>> CompareResponses([FromQuery] int responseId1, [FromQuery] int responseId2)
        {
            try
            {
                _logger.LogInformation("Richiesta di confronto risposte con ID: {ResponseId1}, {ResponseId2}", responseId1, responseId2);
                
                // Recupera le risposte dal database
                var response1 = await _dbContext.Responses.FindAsync(responseId1);
                var response2 = await _dbContext.Responses.FindAsync(responseId2);

                if (response1 == null)
                {
                    return NotFound($"Risposta con ID {responseId1} non trovata");
                }

                if (response2 == null)
                {
                    return NotFound($"Risposta con ID {responseId2} non trovata");
                }

                var result = _comparisonService.CompareResponses(response1, response2);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il confronto delle risposte");
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }
    }
}