using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BackendService.Models;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace BackendService
{
    /// <summary>
    /// Servizio per la gestione degli scenari di test, che include la registrazione
    /// e la riproduzione delle interazioni HTTP per il testing automatizzato.
    /// </summary>
    public class TestScenarioService
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<TestScenarioService> _logger;
        private readonly HttpClient _httpClient;
        private int? _recordingScenarioId;
        private DateTime _recordingStartTime;

        /// <summary>
        /// Indica se la registrazione è attiva.
        /// </summary>
        public bool IsRecording => _recordingScenarioId.HasValue;
        
        /// <summary>
        /// Ottiene l'ID dello scenario attualmente in registrazione, o null se non in registrazione.
        /// </summary>
        public int? RecordingScenarioId => _recordingScenarioId;
        
        /// <summary>
        /// Ottiene il timestamp di inizio della registrazione corrente, o DateTime.MinValue se non in registrazione.
        /// </summary>
        public DateTime RecordingStartTime => IsRecording ? _recordingStartTime : DateTime.MinValue;

        /// <summary>
        /// Inizializza una nuova istanza di TestScenarioService.
        /// </summary>
        /// <param name="context">Il contesto del database per accedere ai dati</param>
        /// <param name="logger">Il logger per registrare eventi e diagnostica</param>
        public TestScenarioService(DatabaseContext context, ILogger<TestScenarioService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient();
            _recordingScenarioId = null;
            _recordingStartTime = DateTime.MinValue;
        }
        
        /// <summary>
        /// Inizia la registrazione per uno scenario specifico.
        /// </summary>
        /// <param name="scenarioId">L'ID dello scenario per cui avviare la registrazione</param>
        /// <returns>True se l'operazione ha avuto successo, altrimenti False</returns>
        public async Task<bool> StartRecordingAsync(int scenarioId)
        {
            // Verifica che lo scenario esista
            var scenario = await _context.TestScenarios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == scenarioId);
                
            if (scenario == null)
            {
                _logger.LogWarning($"Tentativo di avviare la registrazione per uno scenario inesistente: {scenarioId}");
                return false;
            }
            
            // Se c'è già una registrazione attiva, fermala
            if (IsRecording)
            {
                _logger.LogWarning($"Fermata registrazione esistente per lo scenario {_recordingScenarioId} prima di avviare una nuova registrazione");
                await StopRecordingAsync();
            }
            
            _recordingScenarioId = scenarioId;
            _recordingStartTime = DateTime.UtcNow;
            _logger.LogInformation($"Avviata registrazione per lo scenario ID: {scenarioId} alle {_recordingStartTime}");
            return true;
        }
        
        /// <summary>
        /// Ferma la registrazione corrente.
        /// </summary>
        /// <returns>True se l'operazione ha avuto successo, altrimenti False</returns>
        public async Task<bool> StopRecordingAsync()
        {
            if (!IsRecording)
            {
                _logger.LogWarning("Tentativo di fermare la registrazione quando non è attiva");
                return false;
            }
            
            var duration = DateTime.UtcNow - _recordingStartTime;
            _logger.LogInformation($"Fermata registrazione per lo scenario {_recordingScenarioId}. Durata: {duration.TotalSeconds:F2} secondi");
            
            _recordingScenarioId = null;
            _recordingStartTime = DateTime.MinValue;
            return true;
        }
        
        /// <summary>
        /// Ottiene lo stato corrente della registrazione.
        /// </summary>
        /// <returns>
        /// Un oggetto che contiene:
        /// - isRecording: Boolean che indica se la registrazione è attiva
        /// - scenarioId: ID dello scenario in registrazione, o null se non in registrazione
        /// - recordingStartTime: Orario di avvio della registrazione
        /// - recordingDuration: Durata della registrazione corrente in secondi
        /// - timestamp: Timestamp della query
        /// </returns>
        public async Task<object> GetRecordingStatusAsync()
        {
            var now = DateTime.UtcNow;
            var duration = IsRecording ? (now - _recordingStartTime).TotalSeconds : 0;
            
            // Carica il nome dello scenario se stiamo registrando
            string? scenarioName = null;
            int? stepCount = null;
            
            if (IsRecording)
            {
                var scenario = await _context.TestScenarios
                    .Where(s => s.Id == _recordingScenarioId)
                    .Select(s => new { s.Name, StepCount = s.Steps.Count })
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                    
                if (scenario != null)
                {
                    scenarioName = scenario.Name;
                    stepCount = scenario.StepCount;
                }
            }
            
            return new 
            { 
                isRecording = IsRecording, 
                scenarioId = RecordingScenarioId,
                scenarioName = scenarioName,
                stepCount = stepCount,
                recordingStartTime = IsRecording ? _recordingStartTime : (DateTime?)null,
                recordingDurationSeconds = Math.Round(duration, 2),
                timestamp = now
            };
        }

        /// <summary>
        /// Ottiene tutti gli scenari di test, con opzioni di paginazione e filtro.
        /// </summary>
        /// <param name="page">Numero di pagina (1-based)</param>
        /// <param name="pageSize">Dimensione della pagina</param>
        /// <param name="includeInactive">Se includere scenari inattivi</param>
        /// <returns>Lista di scenari di test</returns>
        public async Task<(List<TestScenario> Scenarios, int TotalCount)> GetAllScenariosAsync(
            int page = 1, 
            int pageSize = 50,
            bool includeInactive = false)
        {
            pageSize = Math.Min(Math.Max(1, pageSize), 100); // Limita pageSize tra 1 e 100
            page = Math.Max(1, page); // Assicura che page sia >= 1
            
            var query = _context.TestScenarios.AsQueryable();
            
            if (!includeInactive)
            {
                query = query.Where(ts => ts.IsActive);
            }
            
            var totalCount = await query.CountAsync();
            
            var scenarios = await query
                .OrderByDescending(ts => ts.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ts => new TestScenario
                {
                    Id = ts.Id,
                    Name = ts.Name,
                    Description = ts.Description,
                    CreatedAt = ts.CreatedAt,
                    IsActive = ts.IsActive,
                    Steps = ts.Steps.Select(s => new ScenarioStep
                    {
                        Id = s.Id,
                        Order = s.Order,
                        Name = s.Name,
                        Description = s.Description,
                        IsActive = s.IsActive
                    }).ToList()
                })
                .AsNoTracking()
                .ToListAsync();
                
            _logger.LogInformation($"Recuperati {scenarios.Count} scenari di test (pagina {page}, totale {totalCount})");
            
            return (scenarios, totalCount);
        }

        /// <summary>
        /// Ottiene uno scenario specifico per ID, con tutte le informazioni associate.
        /// </summary>
        /// <param name="id">ID dello scenario da recuperare</param>
        /// <returns>Lo scenario trovato o null</returns>
        public async Task<TestScenario?> GetScenarioByIdAsync(int id)
        {
            var scenario = await _context.TestScenarios
                .Include(ts => ts.Steps.OrderBy(s => s.Order))
                    .ThenInclude(ss => ss.HttpRequest)
                .Include(ts => ts.Steps)
                    .ThenInclude(ss => ss.HttpResponse)
                .AsNoTracking()
                .FirstOrDefaultAsync(ts => ts.Id == id);
                
            if (scenario == null)
            {
                _logger.LogWarning($"Scenario di test con ID {id} non trovato");
            }
            else
            {
                _logger.LogInformation($"Recuperato scenario di test {id} con {scenario.Steps.Count} step");
            }
            
            return scenario;
        }

        /// <summary>
        /// Crea un nuovo scenario di test.
        /// </summary>
        /// <param name="scenario">Lo scenario da creare</param>
        /// <returns>Lo scenario creato con l'ID assegnato dal database</returns>
        public async Task<TestScenario> CreateScenarioAsync(TestScenario scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }
            
            // Validazione minima
            if (string.IsNullOrWhiteSpace(scenario.Name))
            {
                throw new ArgumentException("Il nome dello scenario non può essere vuoto", nameof(scenario));
            }
            
            scenario.CreatedAt = DateTime.UtcNow;
            
            _context.TestScenarios.Add(scenario);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Creato scenario di test: {scenario.Id} - {scenario.Name}");
            
            return scenario;
        }

        /// <summary>
        /// Aggiorna uno scenario esistente.
        /// </summary>
        /// <param name="scenario">Lo scenario aggiornato</param>
        /// <returns>True se l'aggiornamento ha avuto successo, altrimenti False</returns>
        public async Task<bool> UpdateScenarioAsync(TestScenario scenario)
        {
            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }
            
            var existingScenario = await _context.TestScenarios
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == scenario.Id);
            
            if (existingScenario == null)
            {
                _logger.LogWarning($"Scenario di test non trovato per l'aggiornamento: {scenario.Id}");
                return false;
            }
            
            // Aggiorna solo i campi modificabili
            _context.Entry(existingScenario).State = EntityState.Detached;
            
            _context.TestScenarios.Attach(scenario);
            _context.Entry(scenario).Property(s => s.Name).IsModified = true;
            _context.Entry(scenario).Property(s => s.Description).IsModified = true;
            _context.Entry(scenario).Property(s => s.IsActive).IsModified = true;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Aggiornato scenario di test: {scenario.Id} - {scenario.Name}");
            
            return true;
        }

        /// <summary>
        /// Elimina uno scenario di test e tutti i suoi step.
        /// </summary>
        /// <param name="id">ID dello scenario da eliminare</param>
        /// <returns>True se l'eliminazione ha avuto successo, altrimenti False</returns>
        public async Task<bool> DeleteScenarioAsync(int id)
        {
            // Per maggiore efficienza, utilizziamo una transazione
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Controlliamo prima che esista
                var scenarioExists = await _context.TestScenarios.AnyAsync(s => s.Id == id);
                
                if (!scenarioExists)
                {
                    _logger.LogWarning($"Scenario di test non trovato per l'eliminazione: {id}");
                    return false;
                }
                
                // Elimina tutti gli step associati (esecuzione di query raw SQL per efficienza)
                int stepsDeleted = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM ScenarioSteps WHERE TestScenarioId = {0}", id);
                
                // Elimina lo scenario
                int scenariosDeleted = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM TestScenarios WHERE Id = {0}", id);
                    
                await transaction.CommitAsync();
                
                _logger.LogInformation($"Eliminato scenario di test {id} con {stepsDeleted} step");
                
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Errore durante l'eliminazione dello scenario {id}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Aggiunge uno step a uno scenario esistente.
        /// </summary>
        /// <param name="scenarioId">ID dello scenario</param>
        /// <param name="step">Lo step da aggiungere</param>
        /// <returns>Lo step creato o null se lo scenario non esiste</returns>
        public async Task<ScenarioStep?> AddStepToScenarioAsync(int scenarioId, ScenarioStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }
            
            // Verifica che lo scenario esista
            bool scenarioExists = await _context.TestScenarios
                .AsNoTracking()
                .AnyAsync(s => s.Id == scenarioId);
                
            if (!scenarioExists)
            {
                _logger.LogWarning($"Scenario di test non trovato per l'aggiunta di uno step: {scenarioId}");
                return null;
            }
            
            // Determina il prossimo ordine disponibile
            int nextOrder = 1;
            
            var maxOrder = await _context.ScenarioSteps
                .Where(s => s.TestScenarioId == scenarioId)
                .Select(s => (int?)s.Order)
                .MaxAsync() ?? 0;
                
            nextOrder = maxOrder + 1;
            
            // Imposta i valori dello step
            step.TestScenarioId = scenarioId;
            step.Order = nextOrder;
            step.IsActive = step.IsActive; // Mantieni il valore assegnato o usa il default
            
            _context.ScenarioSteps.Add(step);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Aggiunto step {step.Id} (ordine {step.Order}) allo scenario {scenarioId}");
            
            return step;
        }

        /// <summary>
        /// Aggiorna uno step esistente.
        /// </summary>
        /// <param name="step">Lo step aggiornato</param>
        /// <returns>True se l'aggiornamento ha avuto successo, altrimenti False</returns>
        public async Task<bool> UpdateStepAsync(ScenarioStep step)
        {
            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }
            
            var existingStep = await _context.ScenarioSteps
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == step.Id);
                
            if (existingStep == null)
            {
                _logger.LogWarning($"Step di scenario non trovato per l'aggiornamento: {step.Id}");
                return false;
            }
            
            // Aggiorna solo i campi modificabili
            _context.Entry(existingStep).State = EntityState.Detached;
            
            _context.ScenarioSteps.Attach(step);
            _context.Entry(step).Property(s => s.Name).IsModified = true;
            _context.Entry(step).Property(s => s.Description).IsModified = true;
            _context.Entry(step).Property(s => s.Order).IsModified = true;
            _context.Entry(step).Property(s => s.IsActive).IsModified = true;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Aggiornato step: {step.Id}");
            
            return true;
        }

        /// <summary>
        /// Rimuove uno step da uno scenario.
        /// </summary>
        /// <param name="stepId">ID dello step da rimuovere</param>
        /// <returns>True se la rimozione ha avuto successo, altrimenti False</returns>
        public async Task<bool> RemoveStepFromScenarioAsync(int stepId)
        {
            var step = await _context.ScenarioSteps
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == stepId);
                
            if (step == null)
            {
                _logger.LogWarning($"Step di scenario non trovato per la rimozione: {stepId}");
                return false;
            }
            
            int scenarioId = step.TestScenarioId;
            int stepOrder = step.Order;
            
            // Rimuovi lo step
            _context.ScenarioSteps.Remove(step);
            
            // Riordina gli step successivi
            var stepsToUpdate = await _context.ScenarioSteps
                .Where(s => s.TestScenarioId == scenarioId && s.Order > stepOrder)
                .ToListAsync();
                
            foreach (var s in stepsToUpdate)
            {
                s.Order -= 1;
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Rimosso step {stepId} (ordine {stepOrder}) dallo scenario {scenarioId} e riordinati {stepsToUpdate.Count} step");
            
            return true;
        }

        /// <summary>
        /// Registra una coppia richiesta/risposta in uno scenario specifico.
        /// </summary>
        /// <param name="scenarioId">ID dello scenario</param>
        /// <param name="request">La richiesta HTTP</param>
        /// <param name="response">La risposta HTTP</param>
        /// <returns>Lo step creato o null se lo scenario non esiste</returns>
        public async Task<ScenarioStep?> RecordRequestResponseAsync(
            int scenarioId, 
            BackendService.Models.HttpRequest request, 
            BackendService.Models.HttpResponse response)
        {
            // Verifica che lo scenario esista
            bool scenarioExists = await _context.TestScenarios
                .AsNoTracking()
                .AnyAsync(s => s.Id == scenarioId);
                
            if (!scenarioExists)
            {
                _logger.LogWarning($"Scenario di test non trovato per la registrazione: {scenarioId}");
                return null;
            }
            
            // Salva la richiesta e la risposta in un'unica transazione
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Salva la richiesta e la risposta
                _context.Requests.Add(request);
                await _context.SaveChangesAsync();
                
                response.RequestId = request.Id;
                _context.Responses.Add(response);
                await _context.SaveChangesAsync();
                
                // Crea un nuovo step
                var step = new ScenarioStep
                {
                    TestScenarioId = scenarioId,
                    HttpRequestId = request.Id,
                    HttpResponseId = response.Id,
                    Name = $"Step {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    Description = $"Registrato {request.Method} {request.Url}",
                    IsActive = true
                };
                
                // Determina il prossimo ordine
                int nextOrder = 1;
                var maxOrder = await _context.ScenarioSteps
                    .Where(s => s.TestScenarioId == scenarioId)
                    .Select(s => (int?)s.Order)
                    .MaxAsync() ?? 0;
                    
                nextOrder = maxOrder + 1;
                step.Order = nextOrder;
                
                _context.ScenarioSteps.Add(step);
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                
                _logger.LogInformation($"Registrata interazione {request.Method} {request.Url} nello step {step.Id} (ordine {step.Order}) dello scenario {scenarioId}");
                
                return step;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Errore durante la registrazione della richiesta/risposta nello scenario {scenarioId}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Registra un'interazione quando la registrazione è attiva.
        /// </summary>
        /// <param name="request">La richiesta HTTP</param>
        /// <param name="response">La risposta HTTP</param>
        /// <returns>True se la registrazione ha avuto successo, altrimenti False</returns>
        public async Task<bool> RecordInteractionAsync(
            BackendService.Models.HttpRequest request, 
            BackendService.Models.HttpResponse response)
        {
            if (!IsRecording || !_recordingScenarioId.HasValue)
            {
                _logger.LogInformation("Registrazione non attiva, interazione ignorata");
                return false;
            }
            
            try
            {
                await RecordRequestResponseAsync(_recordingScenarioId.Value, request, response);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Errore durante la registrazione dell'interazione nello scenario {_recordingScenarioId.Value}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Recupera tutti gli step di uno scenario per la riproduzione.
        /// </summary>
        /// <param name="scenarioId">ID dello scenario</param>
        /// <returns>Lista degli step ordinati</returns>
        public async Task<List<ScenarioStep>> ReplayScenarioAsync(int scenarioId)
        {
            var steps = await _context.ScenarioSteps
                .Where(s => s.TestScenarioId == scenarioId && s.IsActive)
                .Include(s => s.HttpRequest)
                .Include(s => s.HttpResponse)
                .OrderBy(s => s.Order)
                .AsNoTracking()
                .ToListAsync();
                
            _logger.LogInformation($"Recuperati {steps.Count} step attivi per la riproduzione dello scenario {scenarioId}");
            
            return steps;
        }

        /// <summary>
        /// Esegue uno scenario di test, eseguendo effettivamente le richieste HTTP.
        /// </summary>
        /// <param name="scenarioId">ID dello scenario da eseguire</param>
        /// <param name="executeRequests">Se eseguire effettivamente le richieste HTTP (true) o solo simulare (false)</param>
        /// <returns>Lista di coppie richiesta/risposta eseguite</returns>
        public async Task<List<(BackendService.Models.HttpRequest Request, BackendService.Models.HttpResponse Response, bool Success)>> 
            ExecuteScenarioAsync(int scenarioId, bool executeRequests = false)
        {
            var steps = await ReplayScenarioAsync(scenarioId);
            
            if (steps.Count == 0)
            {
                _logger.LogWarning($"Nessuno step attivo trovato per l'esecuzione dello scenario {scenarioId}");
                return new List<(BackendService.Models.HttpRequest, BackendService.Models.HttpResponse, bool)>();
            }
            
            var results = new List<(BackendService.Models.HttpRequest, BackendService.Models.HttpResponse, bool)>();
            
            foreach (var step in steps)
            {
                if (step.HttpRequest == null)
                {
                    _logger.LogWarning($"Step {step.Id} senza richiesta, saltato");
                    continue;
                }
                
                var request = step.HttpRequest;
                
                if (executeRequests)
                {
                    try
                    {
                        _logger.LogInformation($"Esecuzione reale della richiesta per lo step {step.Id}: {request.Method} {request.Url}");
                        
                        // Crea una richiesta HTTP effettiva
                        var httpRequest = new HttpRequestMessage
                        {
                            Method = new HttpMethod(request.Method),
                            RequestUri = new Uri(request.Url)
                        };
                        
                        // Aggiungi gli header
                        if (!string.IsNullOrEmpty(request.Headers))
                        {
                            foreach (var headerLine in request.Headers.Split('\n'))
                            {
                                if (string.IsNullOrWhiteSpace(headerLine)) continue;
                                
                                var parts = headerLine.Split(':', 2);
                                if (parts.Length == 2)
                                {
                                    var name = parts[0].Trim();
                                    var value = parts[1].Trim();
                                    
                                    // Salta gli header che non possono essere impostati dal client
                                    if (name.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                                        name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                                        continue;
                                        
                                    httpRequest.Headers.TryAddWithoutValidation(name, value);
                                }
                            }
                        }
                        
                        // Aggiungi il corpo della richiesta se presente
                        if (!string.IsNullOrEmpty(request.Body))
                        {
                            httpRequest.Content = new StringContent(request.Body, Encoding.UTF8);
                        }
                        
                        // Esegui la richiesta
                        var httpResponse = await _httpClient.SendAsync(httpRequest);
                        
                        // Crea un oggetto risposta
                        var actualResponse = new BackendService.Models.HttpResponse
                        {
                            StatusCode = (int)httpResponse.StatusCode,
                            Headers = string.Join("\n", httpResponse.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")),
                            Body = await httpResponse.Content.ReadAsStringAsync(),
                            Timestamp = DateTime.UtcNow.ToString("o"),
                            RequestId = request.Id
                        };
                        
                        results.Add((request, actualResponse, true));
                        
                        _logger.LogInformation($"Eseguito con successo lo step {step.Id} dello scenario {scenarioId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Errore durante l'esecuzione dello step {step.Id}: {ex.Message}");
                        
                        // In caso di errore, crea una risposta con codice 500
                        var errorResponse = new BackendService.Models.HttpResponse
                        {
                            StatusCode = 500,
                            Headers = "Content-Type: application/json",
                            Body = $"{{ \"error\": \"{ex.Message}\" }}",
                            Timestamp = DateTime.UtcNow.ToString("o"),
                            RequestId = request.Id
                        };
                        
                        results.Add((request, errorResponse, false));
                    }
                }
                else
                {
                    // Modalità simulazione - usa solo le risposte salvate
                    var response = step.HttpResponse;
                    
                    if (response != null)
                    {
                        results.Add((request, response, true));
                        _logger.LogInformation($"Simulato lo step {step.Id} dello scenario {scenarioId} (non eseguito realmente)");
                    }
                    else
                    {
                        _logger.LogWarning($"Step {step.Id} senza risposta, saltato");
                    }
                }
            }
            
            _logger.LogInformation($"Esecuzione completata dello scenario {scenarioId}: {results.Count} step eseguiti");
            
            return results;
        }
        
        /// <summary>
        /// Esporta uno scenario in formato JSON.
        /// </summary>
        /// <param name="scenarioId">ID dello scenario da esportare</param>
        /// <returns>Stringa JSON con lo scenario completo</returns>
        public async Task<string> ExportScenarioAsync(int scenarioId)
        {
            var scenario = await GetScenarioByIdAsync(scenarioId);
            
            if (scenario == null)
            {
                _logger.LogWarning($"Scenario {scenarioId} non trovato per l'esportazione");
                throw new ArgumentException($"Scenario con ID {scenarioId} non trovato", nameof(scenarioId));
            }
            
            // Questa operazione potrebbe essere migliorata con System.Text.Json o Newtonsoft.Json
            // ma per ora usiamo un semplice approccio di stringhe per evitare dipendenze aggiuntive
            
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"id\": {scenario.Id},");
            sb.AppendLine($"  \"name\": \"{scenario.Name.Replace("\"", "\\\"")}\",");
            sb.AppendLine($"  \"description\": \"{scenario.Description?.Replace("\"", "\\\"") ?? ""}\",");
            sb.AppendLine($"  \"createdAt\": \"{scenario.CreatedAt:yyyy-MM-ddTHH:mm:ss.fffZ}\",");
            sb.AppendLine($"  \"isActive\": {scenario.IsActive.ToString().ToLowerInvariant()},");
            sb.AppendLine("  \"steps\": [");
            
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                var step = scenario.Steps[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"id\": {step.Id},");
                sb.AppendLine($"      \"order\": {step.Order},");
                sb.AppendLine($"      \"name\": \"{step.Name?.Replace("\"", "\\\"") ?? ""}\",");
                sb.AppendLine($"      \"description\": \"{step.Description?.Replace("\"", "\\\"") ?? ""}\",");
                sb.AppendLine($"      \"isActive\": {step.IsActive.ToString().ToLowerInvariant()},");
                
                if (step.HttpRequest != null)
                {
                    sb.AppendLine("      \"request\": {");
                    sb.AppendLine($"        \"method\": \"{step.HttpRequest.Method}\",");
                    sb.AppendLine($"        \"url\": \"{step.HttpRequest.Url.Replace("\"", "\\\"")}\",");
                    sb.AppendLine($"        \"headers\": \"{step.HttpRequest.Headers?.Replace("\"", "\\\"") ?? ""}\",");
                    sb.AppendLine($"        \"body\": \"{step.HttpRequest.Body?.Replace("\"", "\\\"") ?? ""}\",");
                    sb.AppendLine($"        \"timestamp\": \"{step.HttpRequest.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}\"");
                    sb.AppendLine("      },");
                }
                
                if (step.HttpResponse != null)
                {
                    sb.AppendLine("      \"response\": {");
                    sb.AppendLine($"        \"statusCode\": {step.HttpResponse.StatusCode},");
                    sb.AppendLine($"        \"headers\": \"{step.HttpResponse.Headers?.Replace("\"", "\\\"") ?? ""}\",");
                    sb.AppendLine($"        \"body\": \"{step.HttpResponse.Body?.Replace("\"", "\\\"") ?? ""}\",");
                    sb.AppendLine($"        \"timestamp\": \"{step.HttpResponse.Timestamp:yyyy-MM-ddTHH:mm:ss.fffZ}\"");
                    sb.AppendLine("      }");
                }
                
                sb.Append("    }");
                if (i < scenario.Steps.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            
            _logger.LogInformation($"Esportato scenario {scenarioId} in formato JSON");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Registra una richiesta HTTP come uno step di uno scenario di test
        /// </summary>
        /// <param name="scenarioId">ID dello scenario</param>
        /// <param name="request">La richiesta HTTP da registrare</param>
        /// <param name="clientRequest">Se true, è una richiesta dal client, altrimenti è dal server</param>
        /// <returns>Lo step creato o null se lo scenario non esiste</returns>
        public async Task<ScenarioStep?> AddRequestToScenarioAsync(int scenarioId, Models.HttpRequest request, bool clientRequest = true)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            
            // Verifica che lo scenario esista
            bool scenarioExists = await _context.TestScenarios
                .AsNoTracking()
                .AnyAsync(s => s.Id == scenarioId);
                
            if (!scenarioExists)
            {
                _logger.LogWarning($"Scenario di test non trovato per l'aggiunta della richiesta: {scenarioId}");
                return null;
            }
            
            // Determina il prossimo ordine disponibile
            int nextOrder = 1;
            
            var maxOrder = await _context.ScenarioSteps
                .Where(s => s.TestScenarioId == scenarioId)
                .Select(s => (int?)s.Order)
                .MaxAsync() ?? 0;
                
            nextOrder = maxOrder + 1;
            
            // Crea un nuovo step con questa richiesta
            var step = new ScenarioStep
            {
                TestScenarioId = scenarioId,
                Order = nextOrder,
                HttpRequestId = request.Id,
                HttpRequest = request,
                Name = $"Richiesta {request.Method} {request.Url}",
                Description = $"Registrata il {DateTime.Now}",
                IsActive = true,
                IsClientRequest = clientRequest,
                ParameterizedUrl = request.Url,
                ParameterizedHeaders = request.Headers,
                ParameterizedBody = request.Body
            };
            
            _context.ScenarioSteps.Add(step);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Aggiunta richiesta come step {step.Id} (ordine {step.Order}) allo scenario {scenarioId}");
            
            return step;
        }
        
        /// <summary>
        /// Registra una risposta HTTP come parte di uno step esistente di uno scenario di test
        /// </summary>
        /// <param name="stepId">ID dello step a cui associare la risposta</param>
        /// <param name="response">La risposta HTTP da registrare</param>
        /// <returns>Lo step aggiornato o null se lo step non esiste</returns>
        public async Task<ScenarioStep?> AddResponseToScenarioStepAsync(int stepId, Models.HttpResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            
            // Verifica che lo step esista
            var step = await _context.ScenarioSteps
                .FirstOrDefaultAsync(s => s.Id == stepId);
                
            if (step == null)
            {
                _logger.LogWarning($"Step di scenario non trovato per l'aggiunta della risposta: {stepId}");
                return null;
            }
            
            // Aggiorna lo step con questa risposta
            step.HttpResponseId = response.Id;
            step.Description = $"{step.Description} - Risposta {response.StatusCode} ricevuta";
            
            _context.Entry(step).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Aggiunta risposta {response.Id} allo step {stepId} dello scenario {step.TestScenarioId}");
            
            return step;
        }
        
        /// <summary>
        /// Registra una coppia richiesta/risposta da una chiamata proxy
        /// </summary>
        /// <param name="scenarioId">ID dello scenario</param>
        /// <param name="clientRequest">La richiesta dal client</param>
        /// <param name="clientResponse">La risposta al client</param>
        /// <param name="serverRequest">La richiesta al server (può essere null per risposte da regole)</param>
        /// <param name="serverResponse">La risposta dal server (può essere null per risposte da regole)</param>
        /// <returns>Una tupla contenente gli ID degli step creati (clientStepId, serverStepId)</returns>
        public async Task<(int? ClientStepId, int? ServerStepId)> RecordProxyExchangeAsync(
            int scenarioId, 
            Models.HttpRequest clientRequest, 
            Models.HttpResponse clientResponse,
            Models.HttpRequest? serverRequest = null,
            Models.HttpResponse? serverResponse = null)
        {
            // Registra la richiesta dal client
            var clientStep = await AddRequestToScenarioAsync(scenarioId, clientRequest, true);
            if (clientStep == null)
            {
                return (null, null);
            }
            
            // Aggiungi la risposta al client
            await AddResponseToScenarioStepAsync(clientStep.Id, clientResponse);
            
            // Se c'è stata una richiesta al server, registrala come step separato
            int? serverStepId = null;
            if (serverRequest != null)
            {
                var serverStep = await AddRequestToScenarioAsync(scenarioId, serverRequest, false);
                if (serverStep != null && serverResponse != null)
                {
                    await AddResponseToScenarioStepAsync(serverStep.Id, serverResponse);
                    serverStepId = serverStep.Id;
                }
            }
            
            return (clientStep.Id, serverStepId);
        }

        /// <summary>
        /// Parametrizza uno step di scenario, sostituendo valori concreti con variabili
        /// </summary>
        /// <param name="stepId">ID dello step da parametrizzare</param>
        /// <param name="parameterizations">Dizionario di parametrizzazioni (chiave=valore da sostituire, valore=nome variabile)</param>
        /// <returns>Lo step aggiornato o null se lo step non esiste</returns>
        public async Task<ScenarioStep?> ParameterizeStepAsync(int stepId, Dictionary<string, string> parameterizations)
        {
            if (parameterizations == null || parameterizations.Count == 0)
            {
                throw new ArgumentException("Il dizionario di parametrizzazioni non può essere vuoto", nameof(parameterizations));
            }
            
            // Verifica che lo step esista
            var step = await _context.ScenarioSteps
                .Include(s => s.HttpRequest)
                .Include(s => s.HttpResponse)
                .FirstOrDefaultAsync(s => s.Id == stepId);
                
            if (step == null)
            {
                _logger.LogWarning($"Step di scenario non trovato per la parametrizzazione: {stepId}");
                return null;
            }
            
            // Assicurati che i campi parametrizzati siano inizializzati
            if (string.IsNullOrEmpty(step.ParameterizedUrl) && step.HttpRequest != null)
            {
                step.ParameterizedUrl = step.HttpRequest.Url;
            }
            
            if (string.IsNullOrEmpty(step.ParameterizedHeaders) && step.HttpRequest != null)
            {
                step.ParameterizedHeaders = step.HttpRequest.Headers;
            }
            
            if (string.IsNullOrEmpty(step.ParameterizedBody) && step.HttpRequest != null)
            {
                step.ParameterizedBody = step.HttpRequest.Body;
            }
            
            // Esegui le sostituzioni
            string paramUrl = step.ParameterizedUrl ?? string.Empty;
            string paramHeaders = step.ParameterizedHeaders ?? string.Empty;
            string paramBody = step.ParameterizedBody ?? string.Empty;
            
            // Prepara una copia del dizionario dei parametri esistenti
            var currentParams = step.Parameters ?? new Dictionary<string, string>();
            
            foreach (var param in parameterizations)
            {
                if (string.IsNullOrEmpty(param.Key))
                {
                    continue;
                }
                
                string pattern = Regex.Escape(param.Key);
                string replacement = $"{{{{{param.Value}}}}}";
                
                // Sostituisci nelle versioni parametrizzate
                paramUrl = Regex.Replace(paramUrl, pattern, replacement);
                paramHeaders = Regex.Replace(paramHeaders, pattern, replacement);
                paramBody = Regex.Replace(paramBody, pattern, replacement);
                
                // Aggiungi o aggiorna il parametro
                currentParams[param.Value] = param.Key;
            }
            
            // Aggiorna lo step
            step.ParameterizedUrl = paramUrl;
            step.ParameterizedHeaders = paramHeaders;
            step.ParameterizedBody = paramBody;
            step.Parameters = currentParams;
            
            _context.Entry(step).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Parametrizzato step {stepId} con {parameterizations.Count} variabili");
            
            return step;
        }
    }
}