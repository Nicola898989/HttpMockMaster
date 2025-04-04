namespace BackendService.Models
{
    /// <summary>
    /// Rappresenta una regola per il pattern matching delle richieste HTTP
    /// </summary>
    public class Rule
    {
        /// <summary>
        /// Identificatore unico della regola
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Nome della regola
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Descrizione della regola
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Metodo HTTP da abbinare (GET, POST, PUT, DELETE, ecc.)
        /// </summary>
        public string Method { get; set; } = string.Empty;
        
        /// <summary>
        /// Pattern per abbinare i path URL (supporta anche regex)
        /// </summary>
        public string PathPattern { get; set; } = string.Empty;
        
        /// <summary>
        /// Pattern per abbinare i parametri di query (supporta anche regex)
        /// </summary>
        public string QueryPattern { get; set; } = string.Empty;
        
        /// <summary>
        /// Pattern per abbinare gli header HTTP
        /// </summary>
        public string HeaderPattern { get; set; } = string.Empty;
        
        /// <summary>
        /// Pattern per abbinare il corpo della richiesta
        /// </summary>
        public string BodyPattern { get; set; } = string.Empty;
        
        /// <summary>
        /// Priorità della regola (più alto = viene controllato prima)
        /// </summary>
        public int Priority { get; set; }
        
        /// <summary>
        /// Se la regola è attualmente attiva
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// ID della risposta collegata a questa regola
        /// </summary>
        public int ResponseId { get; set; }
        
        /// <summary>
        /// Risposta HTTP da restituire quando questa regola viene abbinata
        /// </summary>
        public HttpResponse Response { get; set; } = null!;
        
        // Proprietà alias per compatibilità con i test esistenti
        // Non è mappato direttamente a una colonna del database
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string UrlPattern 
        { 
            get => PathPattern;
            set => PathPattern = value;
        }
    }
}
