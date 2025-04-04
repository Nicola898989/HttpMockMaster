using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BackendService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackendService
{
    /// <summary>
    /// Libreria di risposte mock predefinite che possono essere facilmente selezionate e personalizzate
    /// </summary>
    public class MockResponseLibrary
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<MockResponseLibrary> _logger;
        
        public MockResponseLibrary(DatabaseContext dbContext, ILogger<MockResponseLibrary> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            
            // Inizializza i template predefiniti nel database se non esistono
            EnsurePredefinedTemplatesAsync().Wait();
        }
        
        /// <summary>
        /// Ottiene tutti i template di risposta disponibili (predefiniti e personalizzati)
        /// </summary>
        public async Task<List<ResponseTemplate>> GetAvailableTemplatesAsync()
        {
            try
            {
                return await _dbContext.ResponseTemplates
                    .OrderBy(t => t.Category)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nel recupero dei template di risposta");
                return GetPredefinedResponseTemplates().ToList();
            }
        }
        
        /// <summary>
        /// Crea una risposta HTTP basata su un template
        /// </summary>
        /// <param name="templateName">Nome del template</param>
        /// <param name="customization">Opzioni di personalizzazione (opzionale)</param>
        /// <returns>La risposta HTTP creata</returns>
        public async Task<Models.HttpResponse> CreateResponseFromTemplateAsync(
            string templateName, 
            ResponseCustomization customization = null)
        {
            var template = await _dbContext.ResponseTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName);
                
            if (template == null)
            {
                throw new ArgumentException($"Template '{templateName}' non trovato", nameof(templateName));
            }
            
            var response = new Models.HttpResponse
            {
                StatusCode = template.StatusCode,
                Headers = template.Headers,
                Body = template.Body,
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            
            return await ApplyCustomizationAsync(response, customization);
        }
        
        /// <summary>
        /// Applica personalizzazioni a una risposta HTTP
        /// </summary>
        public async Task<Models.HttpResponse> ApplyCustomizationAsync(
            Models.HttpResponse response, 
            ResponseCustomization customization)
        {
            if (customization == null)
            {
                return response;
            }
            
            // Crea un nuovo oggetto risposta per non modificare l'originale
            var customizedResponse = new Models.HttpResponse
            {
                StatusCode = customization.StatusCode ?? response.StatusCode,
                Headers = response.Headers,
                Body = response.Body,
                Timestamp = response.Timestamp
            };
            
            // Applica headers personalizzati
            if (customization.Headers != null && customization.Headers.Count > 0)
            {
                var headers = ParseHeaders(customizedResponse.Headers);
                
                foreach (var header in customization.Headers)
                {
                    headers[header.Key] = header.Value;
                }
                
                customizedResponse.Headers = FormatHeaders(headers);
            }
            
            // Applica sostituzioni al body
            if (customization.BodyReplacements != null && customization.BodyReplacements.Count > 0)
            {
                var body = customizedResponse.Body;
                
                foreach (var replacement in customization.BodyReplacements)
                {
                    body = body.Replace(replacement.Key, replacement.Value);
                }
                
                customizedResponse.Body = body;
            }
            
            return customizedResponse;
        }
        
        /// <summary>
        /// Salva un nuovo template personalizzato o aggiorna un template esistente
        /// </summary>
        public async Task SaveCustomTemplateAsync(ResponseTemplate template)
        {
            if (string.IsNullOrEmpty(template.Name))
            {
                throw new ArgumentException("Il nome del template è obbligatorio", nameof(template));
            }
            
            var existing = await _dbContext.ResponseTemplates
                .FirstOrDefaultAsync(t => t.Name == template.Name);
                
            if (existing != null)
            {
                // Non consentire la modifica dei template di sistema
                if (existing.IsSystem)
                {
                    throw new InvalidOperationException($"Il template '{template.Name}' è un template di sistema e non può essere modificato");
                }
                
                // Aggiorna il template esistente
                existing.Description = template.Description;
                existing.StatusCode = template.StatusCode;
                existing.Headers = template.Headers;
                existing.Body = template.Body;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.Category = template.Category;
            }
            else
            {
                // Crea un nuovo template
                template.IsSystem = false;
                template.CreatedAt = DateTime.UtcNow;
                
                _dbContext.ResponseTemplates.Add(template);
            }
            
            await _dbContext.SaveChangesAsync();
        }
        
        /// <summary>
        /// Elimina un template personalizzato
        /// </summary>
        public async Task DeleteCustomTemplateAsync(string templateName)
        {
            var template = await _dbContext.ResponseTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName);
                
            if (template == null)
            {
                return;
            }
            
            // Non consentire l'eliminazione dei template di sistema
            if (template.IsSystem)
            {
                throw new InvalidOperationException($"Il template '{templateName}' è un template di sistema e non può essere eliminato");
            }
            
            _dbContext.ResponseTemplates.Remove(template);
            await _dbContext.SaveChangesAsync();
        }
        
        /// <summary>
        /// Garantisce che i template predefiniti siano presenti nel database
        /// </summary>
        private async Task EnsurePredefinedTemplatesAsync()
        {
            var predefinedTemplates = GetPredefinedResponseTemplates();
            
            foreach (var template in predefinedTemplates)
            {
                var exists = await _dbContext.ResponseTemplates
                    .AnyAsync(t => t.Name == template.Name);
                    
                if (!exists)
                {
                    template.IsSystem = true;
                    template.CreatedAt = DateTime.UtcNow;
                    
                    _dbContext.ResponseTemplates.Add(template);
                }
            }
            
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore nell'inizializzazione dei template predefiniti");
            }
        }
        
        /// <summary>
        /// Restituisce la collezione dei template di risposta predefiniti
        /// </summary>
        public IReadOnlyCollection<ResponseTemplate> GetPredefinedResponseTemplates()
        {
            return new List<ResponseTemplate>
            {
                // Template 2xx Success
                new ResponseTemplate
                {
                    Name = "JSON Success (200)",
                    Description = "Risposta di successo JSON standard",
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = "{\n  \"status\": \"success\",\n  \"message\": \"{{message}}\",\n  \"data\": {\n    \"id\": \"{{id}}\"\n  }\n}",
                    Category = "Success"
                },
                new ResponseTemplate
                {
                    Name = "Created (201)",
                    Description = "Risorsa creata con successo",
                    StatusCode = 201,
                    Headers = "Content-Type: application/json\nLocation: {{location}}",
                    Body = "{\n  \"status\": \"created\",\n  \"id\": \"{{id}}\",\n  \"message\": \"Risorsa creata con successo\"\n}",
                    Category = "Success"
                },
                new ResponseTemplate
                {
                    Name = "Accepted (202)",
                    Description = "Richiesta accettata per elaborazione asincrona",
                    StatusCode = 202,
                    Headers = "Content-Type: application/json",
                    Body = "{\n  \"status\": \"accepted\",\n  \"message\": \"La richiesta è stata accettata per l'elaborazione\",\n  \"jobId\": \"{{jobId}}\",\n  \"statusUrl\": \"{{statusUrl}}\"\n}",
                    Category = "Success"
                },
                new ResponseTemplate
                {
                    Name = "No Content (204)",
                    Description = "Operazione completata con successo, nessun contenuto restituito",
                    StatusCode = 204,
                    Headers = "",
                    Body = "",
                    Category = "Success"
                },
                
                // Template 4xx Client Error
                new ResponseTemplate
                {
                    Name = "Bad Request (400)",
                    Description = "Richiesta malformata o parametri mancanti",
                    StatusCode = 400,
                    Headers = "Content-Type: application/json",
                    Body = "{\n  \"error\": \"bad_request\",\n  \"message\": \"{{message}}\",\n  \"details\": \"{{details}}\"\n}",
                    Category = "Client Error"
                },
                new ResponseTemplate
                {
                    Name = "Unauthorized (401)",
                    Description = "Autenticazione richiesta",
                    StatusCode = 401,
                    Headers = "Content-Type: application/json\nWWW-Authenticate: Bearer",
                    Body = "{\n  \"error\": \"unauthorized\",\n  \"message\": \"Autenticazione richiesta\"\n}",
                    Category = "Client Error"
                },
                new ResponseTemplate
                {
                    Name = "Forbidden (403)",
                    Description = "Accesso negato",
                    StatusCode = 403,
                    Headers = "Content-Type: application/json",
                    Body = "{\n  \"error\": \"forbidden\",\n  \"message\": \"Non hai i permessi necessari per accedere a questa risorsa\"\n}",
                    Category = "Client Error"
                },
                new ResponseTemplate
                {
                    Name = "Not Found (404)",
                    Description = "Risorsa non trovata",
                    StatusCode = 404,
                    Headers = "Content-Type: application/json",
                    Body = "{\n  \"error\": \"not_found\",\n  \"message\": \"La risorsa richiesta non esiste\",\n  \"resource\": \"{{resource}}\"\n}",
                    Category = "Client Error"
                },
                new ResponseTemplate
                {
                    Name = "Method Not Allowed (405)",
                    Description = "Metodo HTTP non supportato",
                    StatusCode = 405,
                    Headers = "Content-Type: application/json\nAllow: {{allowed_methods}}",
                    Body = "{\n  \"error\": \"method_not_allowed\",\n  \"message\": \"Il metodo {{method}} non è supportato per questa risorsa\",\n  \"allowed_methods\": \"{{allowed_methods}}\"\n}",
                    Category = "Client Error"
                },
                new ResponseTemplate
                {
                    Name = "Conflict (409)",
                    Description = "Conflitto di risorse",
                    StatusCode = 409,
                    Headers = "Content-Type: application/json",
                    Body = "{\n  \"error\": \"conflict\",\n  \"message\": \"{{message}}\",\n  \"details\": \"{{details}}\"\n}",
                    Category = "Client Error"
                },
                new ResponseTemplate
                {
                    Name = "Too Many Requests (429)",
                    Description = "Limite di richieste superato",
                    StatusCode = 429,
                    Headers = "Content-Type: application/json\nRetry-After: {{retry_after}}",
                    Body = "{\n  \"error\": \"rate_limit_exceeded\",\n  \"message\": \"Hai superato il limite di richieste consentito\",\n  \"retry_after\": {{retry_after}}\n}",
                    Category = "Client Error"
                },
                
                // Template 5xx Server Error
                new ResponseTemplate
                {
                    Name = "Server Error (500)",
                    Description = "Errore interno del server",
                    StatusCode = 500,
                    Headers = "Content-Type: application/json",
                    Body = "{\n  \"error\": \"internal_server_error\",\n  \"message\": \"Si è verificato un errore interno del server\",\n  \"reference\": \"{{reference}}\"\n}",
                    Category = "Server Error"
                },
                new ResponseTemplate
                {
                    Name = "Service Unavailable (503)",
                    Description = "Servizio temporaneamente non disponibile",
                    StatusCode = 503,
                    Headers = "Content-Type: application/json\nRetry-After: {{retry_after}}",
                    Body = "{\n  \"error\": \"service_unavailable\",\n  \"message\": \"Il servizio è temporaneamente non disponibile\",\n  \"retry_after\": {{retry_after}}\n}",
                    Category = "Server Error"
                },
                
                // Altri formati
                new ResponseTemplate
                {
                    Name = "XML Success",
                    Description = "Risposta di successo in formato XML",
                    StatusCode = 200,
                    Headers = "Content-Type: application/xml",
                    Body = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<response>\n  <status>success</status>\n  <message>{{message}}</message>\n  <data>\n    <id>{{id}}</id>\n  </data>\n</response>",
                    Category = "Other Formats"
                },
                new ResponseTemplate
                {
                    Name = "Plain Text Response",
                    Description = "Risposta in testo semplice",
                    StatusCode = 200,
                    Headers = "Content-Type: text/plain",
                    Body = "{{message}}",
                    Category = "Other Formats"
                },
                new ResponseTemplate
                {
                    Name = "Binary Data",
                    Description = "Risposta con dati binari (esempio immagine)",
                    StatusCode = 200,
                    Headers = "Content-Type: {{content_type}}\nContent-Disposition: attachment; filename=\"{{filename}}\"",
                    Body = "{{binary_data_placeholder}}",
                    Category = "Other Formats"
                },
                new ResponseTemplate
                {
                    Name = "HTML Response",
                    Description = "Risposta HTML",
                    StatusCode = 200,
                    Headers = "Content-Type: text/html",
                    Body = "<!DOCTYPE html>\n<html>\n<head>\n  <title>{{title}}</title>\n</head>\n<body>\n  <h1>{{heading}}</h1>\n  <p>{{content}}</p>\n</body>\n</html>",
                    Category = "Other Formats"
                }
            };
        }
        
        /// <summary>
        /// Converti le intestazioni HTTP da stringa a dictionary
        /// </summary>
        private Dictionary<string, string> ParseHeaders(string headers)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (string.IsNullOrEmpty(headers))
            {
                return result;
            }
            
            // Dividi le righe di intestazioni
            var headerLines = headers.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in headerLines)
            {
                // Trova il primo ":" nella riga
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var name = line.Substring(0, separatorIndex).Trim();
                    var value = line.Substring(separatorIndex + 1).Trim();
                    
                    result[name] = value;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Converti le intestazioni HTTP da dictionary a stringa
        /// </summary>
        private string FormatHeaders(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
            {
                return string.Empty;
            }
            
            return string.Join("\n", headers.Select(h => $"{h.Key}: {h.Value}"));
        }
    }
}