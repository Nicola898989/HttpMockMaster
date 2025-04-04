using System;

namespace BackendService.Models
{
    public class HttpRequest
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public bool IsProxied { get; set; }
        public string TargetDomain { get; set; } = string.Empty;
        
        // Navigational property per la risposta
        public virtual HttpResponse? Response { get; set; }
    }
}
