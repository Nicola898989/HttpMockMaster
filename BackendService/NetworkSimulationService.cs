using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;

namespace BackendService
{
    public class NetworkSimulationService
    {
        private readonly ILogger<NetworkSimulationService> _logger;
        private NetworkSimulationSettings _settings;
        private readonly Random _random;
        
        public NetworkSimulationService(ILogger<NetworkSimulationService> logger)
        {
            _logger = logger;
            _settings = new NetworkSimulationSettings();
            _random = new Random();
        }
        
        /// <summary>
        /// Restituisce le impostazioni di simulazione di rete correnti
        /// </summary>
        public NetworkSimulationSettings GetCurrentSettings()
        {
            return _settings;
        }
        
        /// <summary>
        /// Aggiorna le impostazioni di simulazione di rete
        /// </summary>
        public void UpdateSettings(NetworkSimulationSettings newSettings)
        {
            // Validazione dei dati in input
            if (newSettings.LatencyMs < 0) newSettings.LatencyMs = 0;
            if (newSettings.LatencyMs > 10000) newSettings.LatencyMs = 10000;
            
            if (newSettings.LatencyVariationMs < 0) newSettings.LatencyVariationMs = 0;
            if (newSettings.LatencyVariationMs > 5000) newSettings.LatencyVariationMs = 5000;
            
            if (newSettings.PacketLossPercentage < 0) newSettings.PacketLossPercentage = 0;
            if (newSettings.PacketLossPercentage > 100) newSettings.PacketLossPercentage = 100;
            
            if (newSettings.PacketCorruptionPercentage < 0) newSettings.PacketCorruptionPercentage = 0;
            if (newSettings.PacketCorruptionPercentage > 100) newSettings.PacketCorruptionPercentage = 100;
            
            _settings = newSettings;
            _logger.LogInformation("Impostazioni di simulazione di rete aggiornate: Latenza={Latency}ms, Variazione={Variation}ms, Perdita={Loss}%, Corruzione={Corruption}%, Abilitata={Enabled}", 
                _settings.LatencyMs, _settings.LatencyVariationMs, _settings.PacketLossPercentage, _settings.PacketCorruptionPercentage, _settings.SimulationEnabled);
        }
        
        /// <summary>
        /// Reimposta le impostazioni di simulazione di rete ai valori predefiniti
        /// </summary>
        public void ResetToDefaults()
        {
            _settings = new NetworkSimulationSettings();
            _logger.LogInformation("Impostazioni di simulazione di rete ripristinate ai valori predefiniti");
        }
        
        /// <summary>
        /// Applica la latenza simulata (se abilitata)
        /// </summary>
        public async Task ApplyLatencyAsync()
        {
            if (!_settings.SimulationEnabled || _settings.LatencyMs <= 0)
                return;
            
            int actualLatency = _settings.LatencyMs;
            
            // Applica la variazione di latenza se configurata
            if (_settings.LatencyVariationMs > 0)
            {
                int variation = _random.Next(-_settings.LatencyVariationMs, _settings.LatencyVariationMs + 1);
                actualLatency = Math.Max(0, actualLatency + variation);
            }
            
            if (actualLatency > 0)
            {
                _logger.LogDebug("Applicazione di latenza simulata: {Latency}ms", actualLatency);
                await Task.Delay(actualLatency);
            }
        }
        
        /// <summary>
        /// Determina se un pacchetto dovrebbe essere perso in base alla percentuale configurata
        /// </summary>
        public bool ShouldDropPacket(string requestId)
        {
            if (!_settings.SimulationEnabled || _settings.PacketLossPercentage <= 0)
                return false;
            
            int randomValue = _random.Next(1, 101); // 1-100
            bool shouldDrop = randomValue <= _settings.PacketLossPercentage;
            
            if (shouldDrop)
            {
                _logger.LogDebug("Simulazione perdita pacchetto: ID={RequestId}, Probabilità={Percentage}%", 
                    requestId, _settings.PacketLossPercentage);
            }
            
            return shouldDrop;
        }
        
        /// <summary>
        /// Determina se un pacchetto dovrebbe essere corrotto in base alla percentuale configurata
        /// </summary>
        public bool ShouldCorruptPacket()
        {
            if (!_settings.SimulationEnabled || _settings.PacketCorruptionPercentage <= 0)
                return false;
            
            int randomValue = _random.Next(1, 101); // 1-100
            return randomValue <= _settings.PacketCorruptionPercentage;
        }
        
        /// <summary>
        /// Applica la corruzione al contenuto del pacchetto
        /// </summary>
        public string CorruptContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
            
            StringBuilder corrupted = new StringBuilder(content);
            
            // Calcola quanti caratteri corrompere (tra il 5% e il 15% del contenuto)
            int contentLength = content.Length;
            int corruptionAmount = Math.Max(1, contentLength * _random.Next(5, 16) / 100);
            
            // Corrompi caratteri casuali
            for (int i = 0; i < corruptionAmount; i++)
            {
                int position = _random.Next(contentLength);
                char randomChar = (char)_random.Next(32, 127); // ASCII printable characters
                corrupted[position] = randomChar;
            }
            
            return corrupted.ToString();
        }
    }
}