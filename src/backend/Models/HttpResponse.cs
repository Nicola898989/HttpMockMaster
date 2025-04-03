using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class HttpResponse
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int StatusCode { get; set; }
    
    public string Headers { get; set; }
    
    public string Body { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [Required]
    public int RequestId { get; set; }
    
    public bool IsFromRule { get; set; }
    
    public int? RuleId { get; set; }
    
    // Navigation properties
    [ForeignKey("RequestId")]
    public virtual HttpRequest Request { get; set; }
    
    [ForeignKey("RuleId")]
    public virtual Rule Rule { get; set; }
}
