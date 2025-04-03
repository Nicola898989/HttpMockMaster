using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackendService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RulesController : ControllerBase
    {
        private readonly RuleService _ruleService;
        private readonly ILogger<RulesController> _logger;

        public RulesController(RuleService ruleService, ILogger<RulesController> logger)
        {
            _ruleService = ruleService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Rule>>> GetRules()
        {
            try
            {
                var rules = await _ruleService.GetAllRulesAsync();
                return Ok(rules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rules");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Rule>> GetRule(int id)
        {
            try
            {
                var rule = await _ruleService.GetRuleByIdAsync(id);

                if (rule == null)
                {
                    return NotFound();
                }

                return Ok(rule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving rule with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Rule>> CreateRule(Rule rule)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdRule = await _ruleService.CreateRuleAsync(rule);
                return CreatedAtAction(nameof(GetRule), new { id = createdRule.Id }, createdRule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rule");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRule(int id, Rule rule)
        {
            try
            {
                if (id != rule.Id)
                {
                    return BadRequest("Rule ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingRule = await _ruleService.GetRuleByIdAsync(id);
                if (existingRule == null)
                {
                    return NotFound();
                }

                await _ruleService.UpdateRuleAsync(rule);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating rule with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            try
            {
                var rule = await _ruleService.GetRuleByIdAsync(id);
                if (rule == null)
                {
                    return NotFound();
                }

                await _ruleService.DeleteRuleAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting rule with ID {id}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
