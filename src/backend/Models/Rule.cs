using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class Rule
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; }
    
    [Required]
    [StringLength(255)]
    public string PathPattern { get; set; }
    
    [Required]
    public int StatusCode { get; set; } = 200;
    
    public string ContentType { get; set; }
    
    public string Headers { get; set; }
    
    public string ResponseBody { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    [Required]
    public DateTime Created { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastModified { get; set; }
    
    // Navigation property
    public virtual ICollection<HttpResponse> Responses { get; set; } = new List<HttpResponse>();
}
