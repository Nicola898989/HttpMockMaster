using System;
using System.Collections.Generic;

namespace BackendService.Models
{
    /// <summary>
    /// Rappresenta un template per una risposta HTTP preconfigurata
    /// </summary>
    public class ResponseTemplate
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Nome univoco del template
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Descrizione del template e del suo utilizzo
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Codice di stato HTTP
        /// </summary>
        public int StatusCode { get; set; } = 200;
        
        /// <summary>
        /// Intestazioni HTTP (formato: "Nome: Valore")
        /// </summary>
        public string Headers { get; set; } = string.Empty;
        
        /// <summary>
        /// Corpo della risposta con placeholders per personalizzazione
        /// I placeholders sono nel formato {{nome_placeholder}}
        /// </summary>
        public string Body { get; set; } = string.Empty;
        
        /// <summary>
        /// Indica se è un template predefinito di sistema (non eliminabile)
        /// </summary>
        public bool IsSystem { get; set; }
        
        /// <summary>
        /// Data di creazione del template
        /// </summary>
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        
        /// <summary>
        /// Ultima modifica del template
        /// </summary>
        public string? UpdatedAt { get; set; }
        
        /// <summary>
        /// Categoria del template (es. "Success", "Error", "Redirect", ecc.)
        /// </summary>
        public string Category { get; set; } = "Uncategorized";
    }
    
    /// <summary>
    /// Parametri di personalizzazione per un template di risposta
    /// </summary>
    public class ResponseCustomization
    {
        public ResponseCustomization()
        {
            Headers = new Dictionary<string, string>();
            BodyReplacements = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Codice di stato HTTP personalizzato
        /// </summary>
        public int? StatusCode { get; set; }
        
        /// <summary>
        /// Headers HTTP personalizzati da aggiungere/sovrascrivere
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }
        
        /// <summary>
        /// Sostituzioni da applicare al corpo della risposta
        /// Chiave: Placeholder (es. {{nome}}), Valore: valore da sostituire
        /// </summary>
        public Dictionary<string, string> BodyReplacements { get; set; }
    }
}