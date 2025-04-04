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
                // Verifica che la lista di ID sia valida
                if (requestIds == null || requestIds.Count < 2)
                {
                    return BadRequest("Devi fornire almeno due ID di richieste da confrontare");
                }
                
                // Verifica che tutti gli ID siano validi
                if (requestIds.Any(id => id <= 0))
                {
                    return BadRequest("Tutti gli ID delle richieste devono essere numeri positivi");
                }
                
                _logger.LogInformation("Richiesta di confronto per le richieste con ID: {RequestIds}", string.Join(", ", requestIds));
                
                var result = await _comparisonService.CompareRequests(requestIds);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Errore nei parametri di confronto: {ErrorMessage}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante il confronto delle richieste: {ErrorMessage}", ex.Message);
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }

        /// <summary>
        /// Confronta due stringhe JSON
        /// </summary>
        /// <param name="request">Richiesta contenente le due stringhe JSON da confrontare</param>
        /// <returns>Le differenze rilevate tra i due JSON</returns>
        [HttpPost("json")]
        public ActionResult<Dictionary<string, object>> CompareJson([FromBody] JsonDiffRequest request)
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
                _logger.LogError(ex, "Errore durante il confronto JSON: {ErrorMessage}", ex.Message);
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
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
                
                // Verifiche preliminari
                if (responseId1 <= 0 || responseId2 <= 0)
                {
                    return BadRequest("Gli ID delle risposte devono essere numeri positivi");
                }
                
                // Recupera le risposte dal database utilizzando un'unica chiamata al DB per ottimizzazione
                var responses = await Task.WhenAll(
                    _dbContext.Responses.FindAsync(responseId1), 
                    _dbContext.Responses.FindAsync(responseId2)
                );
                
                var response1 = responses[0];
                var response2 = responses[1];

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
                _logger.LogError(ex, "Errore durante il confronto delle risposte: {ErrorMessage}", ex.Message);
                return StatusCode(500, "Si è verificato un errore durante l'elaborazione della richiesta");
            }
        }
    }
}