using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class RequestsController : ControllerBase
{
    private readonly AppDbContext _context;

    public RequestsController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/requests
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HttpRequest>>> GetRequests([FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        return await _context.Requests
            .OrderByDescending(r => r.Timestamp)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    // GET api/requests/5
    [HttpGet("{id}")]
    public async Task<ActionResult<HttpRequest>> GetRequest(int id)
    {
        var request = await _context.Requests
            .Include(r => r.Responses)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        return request;
    }

    // GET api/requests/search
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<HttpRequest>>> SearchRequests([FromQuery] string path, [FromQuery] string method, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _context.Requests.AsQueryable();

        if (!string.IsNullOrEmpty(path))
        {
            query = query.Where(r => r.Path.Contains(path));
        }

        if (!string.IsNullOrEmpty(method))
        {
            query = query.Where(r => r.Method.ToUpper() == method.ToUpper());
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.Timestamp >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(r => r.Timestamp <= to.Value);
        }

        return await query
            .OrderByDescending(r => r.Timestamp)
            .Take(100)
            .ToListAsync();
    }

    // DELETE api/requests/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        var request = await _context.Requests.FindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        _context.Requests.Remove(request);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE api/requests
    [HttpDelete]
    public async Task<IActionResult> DeleteAllRequests()
    {
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Responses");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Requests");
        
        return NoContent();
    }
}
