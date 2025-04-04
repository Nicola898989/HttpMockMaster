using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BackendService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelHttpResponse = BackendService.Models.HttpResponse;
using ModelHttpRequest = BackendService.Models.HttpRequest;

namespace BackendService
{
    /// <summary>
    /// Servizio per la comparazione di richieste e risposte HTTP
    /// </summary>
    public class ComparisonService : IComparisonService
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<ComparisonService> _logger;

        public ComparisonService(DatabaseContext dbContext, ILogger<ComparisonService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Confronta le richieste HTTP specificate
        /// </summary>
        /// <param name="requestIds">Lista di ID delle richieste da confrontare</param>
        /// <param name="config">Configurazione per la comparazione</param>
        /// <returns>Risultato della comparazione</returns>
        public async Task<ComparisonResult> CompareRequests(List<int> requestIds, ComparisonConfig config = null)
        {
            if (requestIds == null || !requestIds.Any())
            {
                throw new ArgumentException("Devi fornire almeno due ID di richieste da confrontare", nameof(requestIds));
            }

            if (requestIds.Count < 2)
            {
                throw new ArgumentException("Devi fornire almeno due ID di richieste da confrontare", nameof(requestIds));
            }

            config ??= new ComparisonConfig();
            
            var requests = await _dbContext.GetRequestsById(requestIds);
            
            if (requests.Count != requestIds.Count)
            {
                var missingIds = requestIds.Except(requests.Select(r => r.Id)).ToList();
                throw new ArgumentException($"Non è stato possibile trovare le richieste con gli ID: {string.Join(", ", missingIds)}");
            }

            var result = new ComparisonResult
            {
                Requests = requests,
                Differences = new Dictionary<string, object>()
            };

            // Compara le URL
            if (requests.Select(r => r.Url).Distinct().Count() > 1)
            {
                result.Differences["Url"] = requests.Select(r => r.Url).ToArray();
            }

            // Compara i metodi HTTP
            if (requests.Select(r => r.Method).Distinct().Count() > 1)
            {
                result.Differences["Method"] = requests.Select(r => r.Method).ToArray();
            }

            // Compara le intestazioni delle richieste (se richiesto)
            if (config.CompareRequestHeaders)
            {
                var headersComparison = CompareHeaders(requests.Select(r => r.Headers).ToList(), config.IgnoreHeaders);
                if (headersComparison.Count > 0)
                {
                    result.Differences["RequestHeaders"] = headersComparison;
                }
            }

            // Compara i corpi delle richieste (se richiesto)
            if (config.CompareRequestBodies)
            {
                var requestBodies = requests.Select(r => r.Body).ToList();
                if (requestBodies.Distinct().Count() > 1)
                {
                    if (config.DetailedJsonComparison && IsAllJson(requestBodies))
                    {
                        result.Differences["RequestBodyDetails"] = GetMultipleJsonDiff(requestBodies);
                    }
                    else
                    {
                        result.Differences["RequestBody"] = requestBodies.ToArray();
                    }
                }
            }

            // Compara le risposte
            var responses = requests.Select(r => r.Response).ToList();
            if (responses.Any(r => r == null))
            {
                result.Differences["ResponseMissing"] = requests.Select(r => r.Response != null).ToArray();
            }
            else
            {
                // Compara i codici di stato
                if (responses.Select(r => r.StatusCode).Distinct().Count() > 1)
                {
                    result.Differences["StatusCode"] = responses.Select(r => r.StatusCode).ToArray();
                }

                // Compara le intestazioni delle risposte (se richiesto)
                if (config.CompareResponseHeaders)
                {
                    var headersComparison = CompareHeaders(responses.Select(r => r.Headers).ToList(), config.IgnoreHeaders);
                    if (headersComparison.Count > 0)
                    {
                        result.Differences["ResponseHeaders"] = headersComparison;
                    }
                }

                // Compara i corpi delle risposte
                var responseBodies = responses.Select(r => r.Body).ToList();
                if (responseBodies.Distinct().Count() > 1)
                {
                    if (config.DetailedJsonComparison && IsAllJson(responseBodies))
                    {
                        result.Differences["ResponseBodyDetails"] = GetMultipleJsonDiff(responseBodies);
                    }
                    else
                    {
                        result.Differences["ResponseBody"] = responseBodies.ToArray();
                    }
                }

                // Compara i timestamp (se richiesto)
                if (config.CompareTimestamps)
                {
                    var timestamps = responses.Select(r => r.Timestamp).ToList();
                    if (timestamps.Distinct().Count() > 1)
                    {
                        result.Differences["ResponseTimestamp"] = timestamps.Select(t => t.ToString("o")).ToArray();
                    }
                }
            }

            // Calcola la percentuale di similarità
            int totalComparisons = 5; // Url, Method, RequestHeaders, RequestBody, StatusCode
            if (responses.All(r => r != null))
            {
                totalComparisons += 2; // ResponseHeaders, ResponseBody
            }

            int differentFields = result.Differences.Count;
            result.SimilarityPercentage = (int)Math.Round(100.0 * (totalComparisons - differentFields) / totalComparisons);
            result.HasSignificantDifferences = differentFields > 0;

            return result;
        }

        /// <summary>
        /// Confronta due risposte HTTP e rileva le differenze
        /// </summary>
        /// <param name="response1">Prima risposta HTTP</param>
        /// <param name="response2">Seconda risposta HTTP</param>
        /// <returns>Elenco delle differenze rilevate</returns>
        public Dictionary<string, object> CompareResponses(ModelHttpResponse response1, ModelHttpResponse response2)
        {
            if (response1 == null || response2 == null)
            {
                throw new ArgumentNullException(response1 == null ? nameof(response1) : nameof(response2));
            }

            var differences = new Dictionary<string, object>();

            // Confronta i codici di stato
            if (response1.StatusCode != response2.StatusCode)
            {
                differences["StatusCode"] = new[] { response1.StatusCode, response2.StatusCode };
            }

            // Confronta le intestazioni
            var headers1 = ParseHeaders(response1.Headers);
            var headers2 = ParseHeaders(response2.Headers);
            var uniqueHeaders = headers1.Keys.Union(headers2.Keys).ToList();

            var headerDifferences = new Dictionary<string, object>();
            foreach (var header in uniqueHeaders)
            {
                if (!headers1.TryGetValue(header, out var value1))
                {
                    headerDifferences[header] = new[] { null, headers2[header] };
                }
                else if (!headers2.TryGetValue(header, out var value2))
                {
                    headerDifferences[header] = new[] { value1, null };
                }
                else if (value1 != value2)
                {
                    headerDifferences[header] = new[] { value1, value2 };
                }
            }

            if (headerDifferences.Count > 0)
            {
                differences["ResponseHeaders"] = headerDifferences;
            }

            // Confronta i corpi
            if (response1.Body != response2.Body)
            {
                // Prova a confrontare come JSON
                if (IsJson(response1.Body) && IsJson(response2.Body))
                {
                    var jsonDiff = GetJsonDiff(response1.Body, response2.Body);
                    if (jsonDiff.Count > 0)
                    {
                        differences["ResponseBodyJson"] = jsonDiff;
                    }
                }
                else
                {
                    differences["ResponseBody"] = new[] { response1.Body, response2.Body };
                }
            }

            return differences;
        }

        /// <summary>
        /// Confronta due stringhe JSON e rileva le differenze
        /// </summary>
        /// <param name="json1">Prima stringa JSON</param>
        /// <param name="json2">Seconda stringa JSON</param>
        /// <returns>Dizionario con le differenze rilevate</returns>
        public Dictionary<string, object> GetJsonDiff(string json1, string json2)
        {
            var differences = new Dictionary<string, object>();
            
            try
            {
                // Parsing JSON
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                using var doc1 = JsonDocument.Parse(json1, options);
                using var doc2 = JsonDocument.Parse(json2, options);

                // Confronta gli oggetti JSON
                CompareJsonElements(doc1.RootElement, doc2.RootElement, "", differences);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Errore durante il parsing JSON");
                differences["_error"] = "Impossibile comparare JSON non validi: " + ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore imprevisto durante la comparazione JSON");
                differences["_error"] = "Errore durante la comparazione: " + ex.Message;
            }

            return differences;
        }

        /// <summary>
        /// Verifica se una stringa è un JSON valido
        /// </summary>
        private bool IsJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            
            // Verifica se inizia con { o [
            if (!(text.StartsWith("{") && text.EndsWith("}")) && 
                !(text.StartsWith("[") && text.EndsWith("]")))
            {
                return false;
            }

            try
            {
                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };
                
                using var doc = JsonDocument.Parse(text, options);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Verifica che tutte le stringhe siano JSON validi
        /// </summary>
        private bool IsAllJson(List<string> jsonTexts)
        {
            return jsonTexts.All(IsJson);
        }

        /// <summary>
        /// Confronta elementi JSON e raccoglie le differenze
        /// </summary>
        private void CompareJsonElements(JsonElement element1, JsonElement element2, string path, Dictionary<string, object> differences)
        {
            if (element1.ValueKind != element2.ValueKind)
            {
                differences[path.TrimStart('.')] = new[] { GetJsonValue(element1), GetJsonValue(element2) };
                return;
            }

            switch (element1.ValueKind)
            {
                case JsonValueKind.Object:
                    // Ottieni tutte le chiavi da entrambi gli oggetti
                    var props1 = element1.EnumerateObject().Select(p => p.Name).ToHashSet();
                    var props2 = element2.EnumerateObject().Select(p => p.Name).ToHashSet();
                    var allProps = props1.Union(props2).ToList();

                    foreach (var prop in allProps)
                    {
                        var newPath = string.IsNullOrEmpty(path) ? prop : $"{path}.{prop}";
                        
                        if (!props1.Contains(prop))
                        {
                            // Proprietà presente solo nel secondo oggetto
                            differences[newPath] = new[] { null, GetJsonValue(element2.GetProperty(prop)) };
                        }
                        else if (!props2.Contains(prop))
                        {
                            // Proprietà presente solo nel primo oggetto
                            differences[newPath] = new[] { GetJsonValue(element1.GetProperty(prop)), null };
                        }
                        else
                        {
                            // Proprietà presente in entrambi, confronta ricorsivamente
                            CompareJsonElements(element1.GetProperty(prop), element2.GetProperty(prop), newPath, differences);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    // Confronta gli array
                    var array1 = element1.EnumerateArray().ToList();
                    var array2 = element2.EnumerateArray().ToList();

                    if (array1.Count != array2.Count)
                    {
                        differences[path.TrimStart('.')] = new object[] { 
                            $"Array di {array1.Count} elementi", 
                            $"Array di {array2.Count} elementi" 
                        };
                    }
                    else
                    {
                        // Confronta elemento per elemento
                        for (int i = 0; i < array1.Count; i++)
                        {
                            var itemPath = string.IsNullOrEmpty(path) ? $"[{i}]" : $"{path}[{i}]";
                            CompareJsonElements(array1[i], array2[i], itemPath, differences);
                        }
                    }
                    break;

                default:
                    // Tipi primitivi
                    if (!JsonValueEquals(element1, element2))
                    {
                        differences[path.TrimStart('.')] = new[] { GetJsonValue(element1), GetJsonValue(element2) };
                    }
                    break;
            }
        }

        /// <summary>
        /// Ottiene il valore di un elemento JSON
        /// </summary>
        private object GetJsonValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Object:
                    return "{...}";
                case JsonValueKind.Array:
                    return "[...]";
                default:
                    return element.ToString();
            }
        }

        /// <summary>
        /// Confronta se due elementi JSON sono uguali
        /// </summary>
        private bool JsonValueEquals(JsonElement element1, JsonElement element2)
        {
            if (element1.ValueKind != element2.ValueKind)
                return false;

            switch (element1.ValueKind)
            {
                case JsonValueKind.String:
                    return element1.GetString() == element2.GetString();
                case JsonValueKind.Number:
                    return element1.GetRawText() == element2.GetRawText();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element1.ValueKind == element2.ValueKind;
                case JsonValueKind.Null:
                    return true;
                default:
                    return element1.GetRawText() == element2.GetRawText();
            }
        }

        /// <summary>
        /// Confronta le intestazioni HTTP
        /// </summary>
        private Dictionary<string, object> CompareHeaders(List<string> headersList, List<string> ignoreHeaders)
        {
            var differences = new Dictionary<string, object>();
            var parsedHeadersList = headersList.Select(ParseHeaders).ToList();
            
            // Ottieni tutte le chiavi di intestazione uniche
            var allHeaderKeys = new HashSet<string>();
            foreach (var headers in parsedHeadersList)
            {
                allHeaderKeys.UnionWith(headers.Keys);
            }

            // Rimuovi le intestazioni da ignorare
            foreach (var header in ignoreHeaders)
            {
                allHeaderKeys.Remove(header);
            }

            // Confronta ogni intestazione
            foreach (var header in allHeaderKeys)
            {
                var values = new List<string>();
                bool isDifferent = false;
                string previousValue = null;

                foreach (var headers in parsedHeadersList)
                {
                    if (headers.TryGetValue(header, out var value))
                    {
                        values.Add(value);
                        if (previousValue != null && previousValue != value)
                        {
                            isDifferent = true;
                        }
                        previousValue = value;
                    }
                    else
                    {
                        values.Add(null);
                        isDifferent = true;
                    }
                }

                if (isDifferent)
                {
                    differences[header] = values.ToArray();
                }
            }

            return differences;
        }

        /// <summary>
        /// Confronta più stringhe JSON
        /// </summary>
        private Dictionary<string, object> GetMultipleJsonDiff(List<string> jsonTexts)
        {
            var differences = new Dictionary<string, object>();
            
            try
            {
                // Parsing di tutti i JSON
                var jsonDocs = new List<JsonDocument>();
                foreach (var json in jsonTexts)
                {
                    var options = new JsonDocumentOptions
                    {
                        AllowTrailingCommas = true,
                        CommentHandling = JsonCommentHandling.Skip
                    };
                    
                    jsonDocs.Add(JsonDocument.Parse(json, options));
                }

                // Ottieni tutte le proprietà da tutti i documenti
                var allProperties = new HashSet<string>();
                foreach (var doc in jsonDocs)
                {
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            allProperties.Add(prop.Name);
                        }
                    }
                }

                // Confronta ogni proprietà
                foreach (var prop in allProperties)
                {
                    var values = new List<object>();
                    bool isDifferent = false;
                    object previousValue = null;

                    foreach (var doc in jsonDocs)
                    {
                        if (doc.RootElement.ValueKind == JsonValueKind.Object && 
                            doc.RootElement.TryGetProperty(prop, out var propValue))
                        {
                            var value = GetJsonValue(propValue);
                            values.Add(value);
                            
                            if (previousValue != null && !Equals(previousValue, value))
                            {
                                isDifferent = true;
                            }
                            
                            previousValue = value;
                        }
                        else
                        {
                            values.Add(null);
                            isDifferent = true;
                        }
                    }

                    if (isDifferent)
                    {
                        differences[prop] = values.ToArray();
                    }
                }

                // Pulizia
                foreach (var doc in jsonDocs)
                {
                    doc.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la comparazione di più JSON");
                differences["_error"] = "Errore durante la comparazione: " + ex.Message;
            }

            return differences;
        }

        /// <summary>
        /// Analizza le intestazioni HTTP in un dizionario
        /// </summary>
        private Dictionary<string, string> ParseHeaders(string headersText)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrWhiteSpace(headersText))
                return headers;

            try
            {
                var headerLines = Regex.Split(headersText, "\r\n|\r|\n")
                    .Where(line => !string.IsNullOrWhiteSpace(line));

                foreach (var line in headerLines)
                {
                    var separatorIndex = line.IndexOf(':');
                    if (separatorIndex > 0)
                    {
                        var key = line.Substring(0, separatorIndex).Trim();
                        var value = line.Substring(separatorIndex + 1).Trim();
                        headers[key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Errore nell'analisi delle intestazioni: {Headers}", headersText);
            }

            return headers;
        }
    }

    /// <summary>
    /// Estensioni per DatabaseContext
    /// </summary>
    public static class DatabaseContextExtensions
    {
        /// <summary>
        /// Ottiene le richieste HTTP in base ai loro ID
        /// </summary>
        /// <param name="dbContext">Contesto del database</param>
        /// <param name="requestIds">ID delle richieste da ottenere</param>
        /// <returns>Lista di richieste HTTP</returns>
        public static async Task<List<ModelHttpRequest>> GetRequestsById(this DatabaseContext dbContext, List<int> requestIds)
        {
            return await dbContext.Requests
                .Where(r => requestIds.Contains(r.Id))
                .Include(r => r.Response)
                .ToListAsync();
        }
    }
}