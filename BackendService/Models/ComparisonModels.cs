using System;
using System.Collections.Generic;

namespace BackendService.Models
{
    /// <summary>
    /// Risultato di una comparazione tra richieste HTTP
    /// </summary>
    public class ComparisonResult
    {
        /// <summary>
        /// Le richieste HTTP confrontate
        /// </summary>
        public List<HttpRequest> Requests { get; set; } = new List<HttpRequest>();

        /// <summary>
        /// Le differenze trovate tra le richieste/risposte
        /// </summary>
        public Dictionary<string, object> Differences { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Timestamp della comparazione
        /// </summary>
        public DateTime ComparisonTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indica se sono state trovate differenze significative
        /// </summary>
        public bool HasSignificantDifferences { get; set; }

        /// <summary>
        /// Metrica di similarità delle richieste (0-100%)
        /// </summary>
        public int SimilarityPercentage { get; set; }
    }

    /// <summary>
    /// Richiesta per confrontare due stringhe JSON
    /// </summary>
    public class JsonDiffRequest
    {
        /// <summary>
        /// Prima stringa JSON da confrontare
        /// </summary>
        public string Json1 { get; set; } = string.Empty;

        /// <summary>
        /// Seconda stringa JSON da confrontare
        /// </summary>
        public string Json2 { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configurazione per una comparazione
    /// </summary>
    public class ComparisonConfig
    {
        /// <summary>
        /// Se true, confronta anche le intestazioni delle richieste
        /// </summary>
        public bool CompareRequestHeaders { get; set; } = true;

        /// <summary>
        /// Se true, confronta anche i corpi delle richieste
        /// </summary>
        public bool CompareRequestBodies { get; set; } = true;

        /// <summary>
        /// Se true, confronta anche le intestazioni delle risposte
        /// </summary>
        public bool CompareResponseHeaders { get; set; } = true;

        /// <summary>
        /// Se true, confronta i timestamp
        /// </summary>
        public bool CompareTimestamps { get; set; } = false;

        /// <summary>
        /// Intestazioni da ignorare nel confronto (es. Date, Server, ecc.)
        /// </summary>
        public List<string> IgnoreHeaders { get; set; } = new List<string> { "Date", "Server", "Content-Length" };

        /// <summary>
        /// Confronto dettagliato dei corpi JSON
        /// </summary>
        public bool DetailedJsonComparison { get; set; } = true;
    }
}