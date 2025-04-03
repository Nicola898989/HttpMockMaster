using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ProxyController : ControllerBase
{
    private readonly ProxyService _proxyService;

    public ProxyController(ProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    // POST api/proxy/config
    [HttpPost("config")]
    public IActionResult SetProxyConfig(ProxyConfig config)
    {
        _proxyService.SetTargetDomain(config.TargetDomain);
        _proxyService.SetMockMode(config.MockMode);
        
        return Ok(new { message = "Proxy configuration updated", config });
    }
}

public class ProxyConfig
{
    public string TargetDomain { get; set; }
    public bool MockMode { get; set; }
}
