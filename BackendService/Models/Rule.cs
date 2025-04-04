namespace BackendService.Models
{
    public class Rule
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string PathPattern { get; set; } = string.Empty;
        public string QueryPattern { get; set; } = string.Empty;
        public string HeaderPattern { get; set; } = string.Empty;
        public string BodyPattern { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        
        public int ResponseId { get; set; }
        public HttpResponse Response { get; set; } = null!;
        
        // Added for compatibility with tests
        public string UrlPattern 
        { 
            get => PathPattern;
            set => PathPattern = value;
        }
    }
}
