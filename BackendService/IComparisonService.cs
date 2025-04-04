using System.Collections.Generic;
using System.Threading.Tasks;
using BackendService.Models;
using ModelHttpResponse = BackendService.Models.HttpResponse;

namespace BackendService
{
    /// <summary>
    /// Interfaccia per il servizio di comparazione tra richieste/risposte HTTP
    /// </summary>
    public interface IComparisonService
    {
        /// <summary>
        /// Confronta le richieste HTTP specificate tramite i loro ID
        /// </summary>
        /// <param name="requestIds">Lista di ID delle richieste da confrontare</param>
        /// <param name="config">Configurazione opzionale per il confronto</param>
        /// <returns>Risultato della comparazione</returns>
        Task<ComparisonResult> CompareRequests(List<int> requestIds, ComparisonConfig config = null);
        
        /// <summary>
        /// Confronta due risposte HTTP e rileva le differenze
        /// </summary>
        /// <param name="response1">Prima risposta HTTP</param>
        /// <param name="response2">Seconda risposta HTTP</param>
        /// <returns>Elenco delle differenze rilevate</returns>
        Dictionary<string, object> CompareResponses(ModelHttpResponse response1, ModelHttpResponse response2);
        
        /// <summary>
        /// Confronta due stringhe JSON e rileva le differenze
        /// </summary>
        /// <param name="json1">Prima stringa JSON</param>
        /// <param name="json2">Seconda stringa JSON</param>
        /// <returns>Dizionario con le differenze rilevate</returns>
        Dictionary<string, object> GetJsonDiff(string json1, string json2);
    }
}