using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MockResponseLibraryController : ControllerBase
    {
        private readonly MockResponseLibrary _mockLibrary;
        private readonly ILogger<MockResponseLibraryController> _logger;

        public MockResponseLibraryController(
            MockResponseLibrary mockLibrary,
            ILogger<MockResponseLibraryController> logger)
        {
            _mockLibrary = mockLibrary;
            _logger = logger;
        }

        /// <summary>
        /// Ottiene tutti i template di risposta disponibili
        /// </summary>
        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplates()
        {
            try
            {
                var templates = await _mockLibrary.GetAvailableTemplatesAsync();
                
                // Raggruppiamo i template per categoria per una migliore organizzazione
                var groupedTemplates = templates
                    .GroupBy(t => t.Category ?? "Uncategorized")
                    .Select(g => new 
                    {
                        category = g.Key,
                        templates = g.OrderBy(t => t.Name).Select(t => new 
                        {
                            id = t.Id,
                            name = t.Name,
                            description = t.Description,
                            statusCode = t.StatusCode,
                            isSystem = t.IsSystem,
                            category = t.Category
                        })
                    })
                    .OrderBy(g => g.category);
                
                return Ok(groupedTemplates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero dei template di risposta");
                return StatusCode(500, new { error = "Errore nel recupero dei template di risposta" });
            }
        }

        /// <summary>
        /// Ottiene i dettagli di un template specifico
        /// </summary>
        [HttpGet("templates/{id}")]
        public async Task<IActionResult> GetTemplate(int id)
        {
            try
            {
                var templates = await _mockLibrary.GetAvailableTemplatesAsync();
                var template = templates.FirstOrDefault(t => t.Id == id);
                
                if (template == null)
                {
                    return NotFound(new { error = $"Template con ID {id} non trovato" });
                }
                
                return Ok(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore nel recupero del template con ID {id}");
                return StatusCode(500, new { error = "Errore nel recupero del template" });
            }
        }

        /// <summary>
        /// Crea un nuovo template personalizzato o aggiorna un template esistente
        /// </summary>
        [HttpPost("templates")]
        public async Task<IActionResult> SaveTemplate([FromBody] ResponseTemplate template)
        {
            try
            {
                if (template == null)
                {
                    return BadRequest(new { error = "Dati del template mancanti" });
                }
                
                if (string.IsNullOrEmpty(template.Name))
                {
                    return BadRequest(new { error = "Il nome del template è obbligatorio" });
                }
                
                // Preveniamo la modifica dell'attributo IsSystem
                template.IsSystem = false;
                
                await _mockLibrary.SaveCustomTemplateAsync(template);
                
                return Ok(new { success = true, message = "Template salvato con successo" });
            }
            catch (InvalidOperationException ex)
            {
                // Gestione specifica per tentativi di modifica di template di sistema
                _logger.LogWarning(ex, "Tentativo di modifica di un template di sistema");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel salvataggio del template");
                return StatusCode(500, new { error = "Errore nel salvataggio del template" });
            }
        }

        /// <summary>
        /// Elimina un template personalizzato
        /// </summary>
        [HttpDelete("templates/{name}")]
        public async Task<IActionResult> DeleteTemplate(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    return BadRequest(new { error = "Nome del template mancante" });
                }
                
                await _mockLibrary.DeleteCustomTemplateAsync(name);
                
                return Ok(new { success = true, message = "Template eliminato con successo" });
            }
            catch (InvalidOperationException ex)
            {
                // Gestione specifica per tentativi di eliminazione di template di sistema
                _logger.LogWarning(ex, "Tentativo di eliminazione di un template di sistema");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore nell'eliminazione del template '{name}'");
                return StatusCode(500, new { error = "Errore nell'eliminazione del template" });
            }
        }

        /// <summary>
        /// Crea una risposta HTTP basata su un template specificato
        /// </summary>
        [HttpPost("create-response")]
        public async Task<IActionResult> CreateResponse([FromBody] CreateResponseRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.TemplateName))
                {
                    return BadRequest(new { error = "Nome del template mancante" });
                }
                
                var response = await _mockLibrary.CreateResponseFromTemplateAsync(
                    request.TemplateName, 
                    request.Customization);
                
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                // Template non trovato
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore nella creazione della risposta dal template '{request.TemplateName}'");
                return StatusCode(500, new { error = "Errore nella creazione della risposta" });
            }
        }
    }

    /// <summary>
    /// Richiesta per la creazione di una risposta da un template
    /// </summary>
    public class CreateResponseRequest
    {
        /// <summary>
        /// Nome del template da utilizzare
        /// </summary>
        public string TemplateName { get; set; }
        
        /// <summary>
        /// Personalizzazioni da applicare al template
        /// </summary>
        public ResponseCustomization Customization { get; set; }
    }
}