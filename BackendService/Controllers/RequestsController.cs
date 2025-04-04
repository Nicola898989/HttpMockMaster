using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly DatabaseContext _dbContext;
        private readonly ILogger<RequestsController> _logger;

        public RequestsController(DatabaseContext dbContext, ILogger<RequestsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Models.HttpRequest>>> GetRequests(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] string? method = null,
            [FromQuery] string? url = null,
            [FromQuery] bool? isProxied = null,
            [FromQuery] string? sortBy = "timestamp",
            [FromQuery] bool descending = true)
        {
            try
            {
                // Start with a simple basic query for better performance
                IQueryable<Models.HttpRequest> query = _dbContext.Requests.AsNoTracking();

                // Apply filters
                if (!string.IsNullOrEmpty(method))
                {
                    query = query.Where(r => r.Method.ToLower() == method.ToLower());
                }

                if (!string.IsNullOrEmpty(url))
                {
                    query = query.Where(r => r.Url.Contains(url));
                }

                if (isProxied.HasValue)
                {
                    query = query.Where(r => r.IsProxied == isProxied.Value);
                }

                // Get total count with a separate efficient query
                // This prevents having to materialize the entire result set just to count
                var countQuery = query;
                var totalCount = await countQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Apply sorting
                query = ApplySorting(query, sortBy, descending);

                // Apply pagination efficiently
                var requests = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Add pagination and metadata headers
                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Total-Pages", totalPages.ToString());
                Response.Headers.Add("X-Current-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving requests");
                return StatusCode(500, "Internal server error");
            }
        }
        
        // Helper method to apply sorting based on dynamic parameters
        private IQueryable<Models.HttpRequest> ApplySorting(
            IQueryable<Models.HttpRequest> query, 
            string? sortBy, 
            bool descending)
        {
            // Default sort is by timestamp
            if (string.IsNullOrEmpty(sortBy))
            {
                sortBy = "timestamp";
            }
            
            // Apply the appropriate sorting based on the field
            switch (sortBy.ToLower())
            {
                case "method":
                    return descending 
                        ? query.OrderByDescending(r => r.Method) 
                        : query.OrderBy(r => r.Method);
                
                case "url":
                    return descending 
                        ? query.OrderByDescending(r => r.Url) 
                        : query.OrderBy(r => r.Url);
                
                case "isproxied":
                    return descending 
                        ? query.OrderByDescending(r => r.IsProxied) 
                        : query.OrderBy(r => r.IsProxied);
                
                case "targetdomain":
                    return descending 
                        ? query.OrderByDescending(r => r.TargetDomain) 
                        : query.OrderBy(r => r.TargetDomain);
                
                case "timestamp":
                default:
                    return descending 
                        ? query.OrderByDescending(r => r.Timestamp) 
                        : query.OrderBy(r => r.Timestamp);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Models.HttpRequest>> GetRequest(int id)
        {
            try
            {
                var request = await _dbContext.Requests.FindAsync(id);

                if (request == null)
                {
                    return NotFound();
                }

                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving request with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}/response")]
        public async Task<ActionResult<Models.HttpResponse>> GetRequestResponse(int id)
        {
            try
            {
                var response = await _dbContext.Responses
                    .FirstOrDefaultAsync(r => r.RequestId == id);

                if (response == null)
                {
                    return NotFound();
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving response for request with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            try
            {
                var request = await _dbContext.Requests.FindAsync(id);
                if (request == null)
                {
                    return NotFound();
                }

                // Also delete associated response
                var response = await _dbContext.Responses
                    .FirstOrDefaultAsync(r => r.RequestId == id);
                if (response != null)
                {
                    _dbContext.Responses.Remove(response);
                }

                _dbContext.Requests.Remove(request);
                await _dbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting request with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> ClearAllRequests()
        {
            try
            {
                // Delete all responses first (due to potential foreign key constraints)
                _dbContext.Responses.RemoveRange(_dbContext.Responses);
                
                // Delete all requests
                _dbContext.Requests.RemoveRange(_dbContext.Requests);
                
                await _dbContext.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all requests");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
