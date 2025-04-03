using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class HttpRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Method { get; set; }
    
    [Required]
    public string Url { get; set; }
    
    [Required]
    public string Path { get; set; }
    
    public string Headers { get; set; }
    
    public string Body { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    // Navigation property
    public virtual ICollection<HttpResponse> Responses { get; set; } = new List<HttpResponse>();
}
