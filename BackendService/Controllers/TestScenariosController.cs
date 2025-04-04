using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using BackendService.Models;

namespace BackendService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ResponseCache(Duration = 15, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept")]
    public class TestScenariosController : ControllerBase
    {
        private readonly TestScenarioService _testScenarioService;
        private readonly InterceptorService _interceptorService;
        private readonly ILogger<TestScenariosController> _logger;

        public TestScenariosController(
            TestScenarioService testScenarioService,
            InterceptorService interceptorService,
            ILogger<TestScenariosController> logger)
        {
            _testScenarioService = testScenarioService;
            _interceptorService = interceptorService;
            _logger = logger;
        }

        // GET: api/TestScenarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TestScenario>>> GetTestScenarios()
        {
            try
            {
                var scenarios = await _testScenarioService.GetAllScenariosAsync();
                return Ok(scenarios);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving test scenarios: {ex.Message}");
                return StatusCode(500, "Internal server error occurred while retrieving test scenarios");
            }
        }

        // GET: api/TestScenarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TestScenario>> GetTestScenario(int id)
        {
            try
            {
                var scenario = await _testScenarioService.GetScenarioByIdAsync(id);

                if (scenario == null)
                {
                    return NotFound();
                }

                return Ok(scenario);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving test scenario {id}: {ex.Message}");
                return StatusCode(500, $"Internal server error occurred while retrieving test scenario {id}");
            }
        }

        // POST: api/TestScenarios
        [HttpPost]
        public async Task<ActionResult<TestScenario>> CreateTestScenario(TestScenario scenario)
        {
            try
            {
                if (scenario == null)
                {
                    return BadRequest("Scenario data is required");
                }

                var createdScenario = await _testScenarioService.CreateScenarioAsync(scenario);
                
                return CreatedAtAction(nameof(GetTestScenario), new { id = createdScenario.Id }, createdScenario);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating test scenario: {ex.Message}");
                return StatusCode(500, "Internal server error occurred while creating test scenario");
            }
        }

        // PUT: api/TestScenarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTestScenario(int id, TestScenario scenario)
        {
            try
            {
                if (id != scenario.Id)
                {
                    return BadRequest("Scenario ID mismatch");
                }

                var result = await _testScenarioService.UpdateScenarioAsync(scenario);
                
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating test scenario {id}: {ex.Message}");
                return StatusCode(500, $"Internal server error occurred while updating test scenario {id}");
            }
        }

        // DELETE: api/TestScenarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestScenario(int id)
        {
            try
            {
                var result = await _testScenarioService.DeleteScenarioAsync(id);
                
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting test scenario {id}: {ex.Message}");
                return StatusCode(500, $"Internal server error occurred while deleting test scenario {id}");
            }
        }

        // POST: api/TestScenarios/5/steps
        [HttpPost("{id}/steps")]
        public async Task<ActionResult<ScenarioStep>> AddStepToScenario(int id, ScenarioStep step)
        {
            try
            {
                if (step == null)
                {
                    return BadRequest("Step data is required");
                }

                var createdStep = await _testScenarioService.AddStepToScenarioAsync(id, step);
                
                if (createdStep == null)
                {
                    return NotFound($"Test scenario with ID {id} was not found");
                }
                
                return CreatedAtAction(nameof(GetTestScenario), new { id }, createdStep);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding step to test scenario {id}: {ex.Message}");
                return StatusCode(500, $"Internal server error occurred while adding step to test scenario {id}");
            }
        }

        // PUT: api/TestScenarios/steps/5
        [HttpPut("steps/{id}")]
        public async Task<IActionResult> UpdateStep(int id, ScenarioStep step)
        {
            try
            {
                if (id != step.Id)
                {
                    return BadRequest("Step ID mismatch");
                }

                var result = await _testScenarioService.UpdateStepAsync(step);
                
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating step {id}: {ex.Message}");
                return StatusCode(500, $"Internal server error occurred while updating step {id}");
            }
        }

        // DELETE: api/TestScenarios/steps/5
        [HttpDelete("steps/{id}")]
        public async Task<IActionResult> DeleteStep(int id)
        {
            try
            {
                var result = await _testScenarioService.RemoveStepFromScenarioAsync(id);
                
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting step {id}: {ex.Message}");
                return StatusCode(500, $"Internal server error occurred while deleting step {id}");
            }
        }

        // POST: api/TestScenarios/5/execute
        [HttpPost("{id}/execute")]
        public async Task<ActionResult<IEnumerable<object>>> ExecuteScenario(int id)
        {
            try
            {
                var results = await _testScenarioService.ExecuteScenarioAsync(id);
                
                if (results == null)
                {
                    return NotFound($"Test scenario with ID {id} was not found");
                }
                
                // Convert to a more serializable format
                var responseData = results.Select(pair => new 
                {
                    Request = pair.Item1,
                    Response = pair.Item2
                }).ToList();
                
                return Ok(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error executing test scenario {id}: {ex.Message}");
                return StatusCode(500, $"Internal server error occurred while executing test scenario {id}");
            }
        }
        
        /// <summary>
        /// Starts recording HTTP traffic to a specific test scenario.
        /// </summary>
        /// <param name="id">ID of the test scenario to record to</param>
        /// <returns>Success or error message</returns>
        [HttpPost("{id}/record/start")]
        public async Task<IActionResult> StartRecording(int id)
        {
            try
            {
                // Verify scenario exists
                var scenario = await _testScenarioService.GetScenarioByIdAsync(id);
                if (scenario == null)
                {
                    return NotFound($"Test scenario with ID {id} was not found");
                }
                
                // Start recording in TestScenarioService
                bool testScenarioResult = await _testScenarioService.StartRecordingAsync(id);
                if (!testScenarioResult)
                {
                    return StatusCode(500, $"Failed to start recording in test scenario service for scenario {id}");
                }
                
                // Start recording in InterceptorService
                bool interceptorResult = await _interceptorService.StartRecordingAsync(id);
                if (!interceptorResult)
                {
                    // If interceptor failed but test scenario succeeded, try to stop test scenario recording
                    await _testScenarioService.StopRecordingAsync();
                    return StatusCode(500, $"Failed to start recording in interceptor service for scenario {id}");
                }
                
                return Ok(new { message = $"Started recording to scenario '{scenario.Name}' (ID: {id})" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting recording for scenario {id}: {ex.Message}");
                return StatusCode(500, $"Internal server error occurred while starting recording for scenario {id}");
            }
        }
        
        /// <summary>
        /// Stops the current recording of HTTP traffic to a test scenario.
        /// </summary>
        /// <returns>Success or error message</returns>
        [HttpPost("record/stop")]
        public async Task<IActionResult> StopRecording()
        {
            try
            {
                // Stop recording in both services, and track results
                bool testScenarioResult = await _testScenarioService.StopRecordingAsync();
                bool interceptorResult = await _interceptorService.StopRecordingAsync();
                
                // If either failed, return a warning but don't throw exception
                if (!testScenarioResult || !interceptorResult)
                {
                    string failureMessage = !testScenarioResult 
                        ? "Failed to stop recording in test scenario service. " 
                        : "";
                    failureMessage += !interceptorResult 
                        ? "Failed to stop recording in interceptor service." 
                        : "";
                    
                    _logger.LogWarning(failureMessage);
                    return StatusCode(207, new { message = "Recording partially stopped", details = failureMessage });
                }
                
                return Ok(new { message = "Recording stopped successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error stopping recording: {ex.Message}");
                return StatusCode(500, "Internal server error occurred while stopping recording");
            }
        }
        
        /// <summary>
        /// Gets the current recording status, including whether recording is active
        /// and which scenario is being recorded to.
        /// </summary>
        /// <returns>Recording status information</returns>
        [HttpGet("record/status")]
        public async Task<IActionResult> GetRecordingStatusAsync()
        {
            try
            {
                // Get status objects from both services
                dynamic testScenarioStatus = await _testScenarioService.GetRecordingStatusAsync();
                bool interceptorIsRecording = await _interceptorService.IsRecordingAsync();
                
                // Create a clean response object with properties from both services
                var result = new
                {
                    isRecording = _testScenarioService.IsRecording,
                    scenarioId = _testScenarioService.RecordingScenarioId,
                    interceptorIsRecording = interceptorIsRecording,
                    
                    // Add the timestamp for when the status was retrieved
                    timestamp = DateTime.UtcNow
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting recording status: {ex.Message}");
                return StatusCode(500, "Internal server error occurred while getting recording status");
            }
        }
    }
}