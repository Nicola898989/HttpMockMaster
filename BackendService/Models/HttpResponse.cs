using System;

namespace BackendService.Models
{
    public class HttpResponse
    {
        public int Id { get; set; }
        public int? RequestId { get; set; }
        public int StatusCode { get; set; }
        public string Headers { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
