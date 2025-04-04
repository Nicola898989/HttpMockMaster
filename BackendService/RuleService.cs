using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BackendService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackendService
{
    public class RuleService
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<RuleService> _logger;

        public RuleService(DatabaseContext dbContext, ILogger<RuleService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IEnumerable<Rule>> GetAllRulesAsync()
        {
            return await _dbContext.Rules
                .Include(r => r.Response)
                .ToListAsync();
        }

        public async Task<Rule?> GetRuleByIdAsync(int id)
        {
            return await _dbContext.Rules
                .Include(r => r.Response)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Rule> CreateRuleAsync(Rule rule)
        {
            if (rule.Response == null)
            {
                rule.Response = new Models.HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = "{\"message\": \"Mocked response\"}",
                    Timestamp = DateTime.UtcNow
                };
            }

            _dbContext.Rules.Add(rule);
            await _dbContext.SaveChangesAsync();
            return rule;
        }

        public async Task<Rule> UpdateRuleAsync(Rule rule)
        {
            _dbContext.Rules.Update(rule);
            await _dbContext.SaveChangesAsync();
            return rule;
        }

        public async Task<bool> DeleteRuleAsync(int id)
        {
            var rule = await _dbContext.Rules.FindAsync(id);
            if (rule != null)
            {
                _dbContext.Rules.Remove(rule);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<Rule?> FindMatchingRuleAsync(HttpListenerRequest request)
        {
            // Load rules without directly accessing 'UrlPattern' in the LINQ query
            var allRules = await _dbContext.Rules
                .Include(r => r.Response)
                .OrderByDescending(r => r.Priority)
                .AsNoTracking()  // Per migliorare le prestazioni quando facciamo solo lettura
                .Where(r => r.IsActive)  // Filtra solo regole attive
                .ToListAsync();

            foreach (var rule in allRules)
            {
                if (MatchesRule(request, rule))
                {
                    _logger.LogInformation($"Found matching rule: {rule.Name} for request: {request.Url}");
                    return rule;
                }
            }

            _logger.LogInformation($"No matching rule found for request: {request.Url}");
            return null;
        }

        private bool MatchesRule(HttpListenerRequest request, Rule rule)
        {
            // Must match HTTP method if specified
            if (!string.IsNullOrEmpty(rule.Method) && 
                !rule.Method.Equals(request.HttpMethod, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Path matching
            if (!string.IsNullOrEmpty(rule.PathPattern))
            {
                // Check if it's regex
                if (rule.PathPattern.StartsWith("^") || rule.PathPattern.EndsWith("$"))
                {
                    try
                    {
                        var regex = new Regex(rule.PathPattern);
                        if (!regex.IsMatch(request.Url.AbsolutePath))
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Invalid regex pattern in rule: {rule.PathPattern}");
                        return false;
                    }
                }
                // Simple string comparison
                else if (!request.Url.AbsolutePath.Contains(rule.PathPattern))
                {
                    return false;
                }
            }
            
            // Query parameter matching
            if (!string.IsNullOrEmpty(rule.QueryPattern))
            {
                string query = request.Url.Query.TrimStart('?');
                
                // Check if it's regex
                if (rule.QueryPattern.StartsWith("^") || rule.QueryPattern.EndsWith("$"))
                {
                    try
                    {
                        var regex = new Regex(rule.QueryPattern);
                        if (!regex.IsMatch(query))
                        {
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Invalid regex pattern in rule: {rule.QueryPattern}");
                        return false;
                    }
                }
                // Split into key-value pairs and check if all required params are present
                else
                {
                    var requiredParams = rule.QueryPattern.Split('&');
                    var requestParams = query.Split('&');
                    
                    foreach (var requiredParam in requiredParams)
                    {
                        if (!requestParams.Any(p => p.StartsWith(requiredParam)))
                        {
                            return false;
                        }
                    }
                }
            }

            // Header matching
            if (!string.IsNullOrEmpty(rule.HeaderPattern))
            {
                var headerPatterns = rule.HeaderPattern.Split(';');
                foreach (var pattern in headerPatterns)
                {
                    var parts = pattern.Split(':');
                    if (parts.Length == 2)
                    {
                        var headerName = parts[0].Trim();
                        var headerValue = parts[1].Trim();
                        
                        if (!request.Headers.AllKeys.Contains(headerName, StringComparer.OrdinalIgnoreCase) ||
                            !request.Headers[headerName].Contains(headerValue))
                        {
                            return false;
                        }
                    }
                }
            }

            // Body matching - we would need to read the request body here,
            // but that would consume the stream. This should be done with proper stream copying.
            // For simplicity, we'll skip this check in this implementation.

            // All checks passed, this rule matches
            return true;
        }
    }
}
