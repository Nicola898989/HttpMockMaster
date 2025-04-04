using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NetworkSimulationController : ControllerBase
    {
        private readonly NetworkSimulationService _networkSimulation;
        private readonly ILogger<NetworkSimulationController> _logger;

        public NetworkSimulationController(
            NetworkSimulationService networkSimulation,
            ILogger<NetworkSimulationController> logger)
        {
            _networkSimulation = networkSimulation;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetSettings()
        {
            try
            {
                var settings = _networkSimulation.GetCurrentSettings();
                _logger.LogInformation("Impostazioni di simulazione di rete recuperate");
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero delle impostazioni di simulazione di rete");
                return StatusCode(500, $"Errore interno: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult UpdateSettings([FromBody] NetworkSimulationSettings settings)
        {
            try
            {
                _networkSimulation.UpdateSettings(settings);
                _logger.LogInformation("Impostazioni di simulazione di rete aggiornate: {Settings}", settings);
                
                return Ok(new { 
                    message = "Impostazioni aggiornate con successo", 
                    currentSettings = _networkSimulation.GetCurrentSettings() 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'aggiornamento delle impostazioni di simulazione di rete");
                return StatusCode(500, $"Errore interno: {ex.Message}");
            }
        }

        [HttpPost("reset")]
        public IActionResult ResetSettings()
        {
            try
            {
                _networkSimulation.ResetToDefaults();
                _logger.LogInformation("Impostazioni di simulazione di rete ripristinate ai valori predefiniti");
                
                return Ok(new { 
                    message = "Impostazioni ripristinate ai valori predefiniti", 
                    currentSettings = _networkSimulation.GetCurrentSettings() 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel ripristino delle impostazioni di simulazione di rete");
                return StatusCode(500, $"Errore interno: {ex.Message}");
            }
        }
    }
}