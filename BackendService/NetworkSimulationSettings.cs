using System;

namespace BackendService
{
    public class NetworkSimulationSettings
    {
        /// <summary>
        /// Latenza di base in millisecondi
        /// </summary>
        public int LatencyMs { get; set; } = 0;
        
        /// <summary>
        /// Variazione di latenza (±) in millisecondi
        /// </summary>
        public int LatencyVariationMs { get; set; } = 0;
        
        /// <summary>
        /// Percentuale di perdita di pacchetti (0-100)
        /// </summary>
        public int PacketLossPercentage { get; set; } = 0;
        
        /// <summary>
        /// Percentuale di corruzione di pacchetti (0-100)
        /// </summary>
        public int PacketCorruptionPercentage { get; set; } = 0;
        
        /// <summary>
        /// Indica se la simulazione di rete è attiva
        /// </summary>
        public bool SimulationEnabled { get; set; } = false;
    }
}