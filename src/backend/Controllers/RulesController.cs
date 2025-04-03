using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RulesController(AppDbContext context)
    {
        _context = context;
    }

    // GET api/rules
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Rule>>> GetRules()
    {
        return await _context.Rules.ToListAsync();
    }

    // GET api/rules/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Rule>> GetRule(int id)
    {
        var rule = await _context.Rules.FindAsync(id);

        if (rule == null)
        {
            return NotFound();
        }

        return rule;
    }

    // POST api/rules
    [HttpPost]
    public async Task<ActionResult<Rule>> CreateRule(Rule rule)
    {
        rule.Created = DateTime.UtcNow;
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, rule);
    }

    // PUT api/rules/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRule(int id, Rule rule)
    {
        if (id != rule.Id)
        {
            return BadRequest();
        }

        var existingRule = await _context.Rules.FindAsync(id);
        if (existingRule == null)
        {
            return NotFound();
        }

        existingRule.Name = rule.Name;
        existingRule.PathPattern = rule.PathPattern;
        existingRule.StatusCode = rule.StatusCode;
        existingRule.ContentType = rule.ContentType;
        existingRule.Headers = rule.Headers;
        existingRule.ResponseBody = rule.ResponseBody;
        existingRule.IsActive = rule.IsActive;
        existingRule.LastModified = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RuleExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE api/rules/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRule(int id)
    {
        var rule = await _context.Rules.FindAsync(id);
        if (rule == null)
        {
            return NotFound();
        }

        _context.Rules.Remove(rule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool RuleExists(int id)
    {
        return _context.Rules.Any(e => e.Id == id);
    }
}
