using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class ProxyController : ControllerBase
    {
        private readonly InterceptorService _interceptorService;
        private readonly ILogger<ProxyController> _logger;

        public ProxyController(InterceptorService interceptorService, ILogger<ProxyController> logger)
        {
            _interceptorService = interceptorService;
            _logger = logger;
        }
        
        [HttpGet]
        public IActionResult GetProxyConfiguration()
        {
            try 
            {
                var targetDomain = _interceptorService.GetProxyDomain();
                var isEnabled = !string.IsNullOrEmpty(targetDomain);
                
                return Ok(new 
                { 
                    isEnabled = isEnabled,
                    targetDomain = targetDomain 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving proxy configuration");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("configure")]
        public IActionResult ConfigureProxy([FromBody] ProxyConfiguration config)
        {
            try
            {
                if (string.IsNullOrEmpty(config.TargetDomain))
                {
                    return BadRequest("Target domain is required");
                }

                // Ensure the domain starts with http:// or https://
                var domain = config.TargetDomain;
                if (!domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    domain = "http://" + domain;
                }

                _interceptorService.SetProxyDomain(domain);
                _logger.LogInformation($"Proxy configured to target: {domain}");

                return Ok(new { message = "Proxy configuration updated" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring proxy");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("disable")]
        public IActionResult DisableProxy()
        {
            try
            {
                _interceptorService.SetProxyDomain(string.Empty);
                _logger.LogInformation("Proxy disabled");

                return Ok(new { message = "Proxy disabled" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling proxy");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class ProxyConfiguration
    {
        public string TargetDomain { get; set; } = string.Empty;
    }
}
