using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BackendService.Models;

namespace BackendService
{
    public class TestScenarioService
    {
        private readonly DatabaseContext _context;
        private readonly ILogger<TestScenarioService> _logger;
        private int? _recordingScenarioId;

        public bool IsRecording => _recordingScenarioId.HasValue;

        public TestScenarioService(DatabaseContext context, ILogger<TestScenarioService> logger)
        {
            _context = context;
            _logger = logger;
            _recordingScenarioId = null;
        }
        
        // Start recording a scenario
        public async Task StartRecordingAsync(int scenarioId)
        {
            _recordingScenarioId = scenarioId;
            _logger.LogInformation($"Started recording to scenario ID: {scenarioId}");
        }
        
        // Stop recording
        public async Task StopRecordingAsync()
        {
            _recordingScenarioId = null;
            _logger.LogInformation("Stopped recording to test scenario");
        }
        
        // Get current recording status
        public object GetRecordingStatus()
        {
            if (_recordingScenarioId.HasValue)
            {
                return new 
                { 
                    isRecording = true, 
                    scenarioId = _recordingScenarioId.Value 
                };
            }
            else
            {
                return new 
                { 
                    isRecording = false, 
                    scenarioId = (int?)null 
                };
            }
        }

        // Get all scenarios
        public async Task<List<TestScenario>> GetAllScenariosAsync()
        {
            return await _context.TestScenarios
                .Include(ts => ts.Steps)
                .OrderByDescending(ts => ts.CreatedAt)
                .ToListAsync();
        }

        // Get scenario by Id
        public async Task<TestScenario> GetScenarioByIdAsync(int id)
        {
            return await _context.TestScenarios
                .Include(ts => ts.Steps)
                    .ThenInclude(ss => ss.HttpRequest)
                .Include(ts => ts.Steps)
                    .ThenInclude(ss => ss.HttpResponse)
                .FirstOrDefaultAsync(ts => ts.Id == id);
        }

        // Create a new scenario
        public async Task<TestScenario> CreateScenarioAsync(TestScenario scenario)
        {
            scenario.CreatedAt = DateTime.UtcNow;
            
            _context.TestScenarios.Add(scenario);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Created test scenario: {scenario.Id} - {scenario.Name}");
            
            return scenario;
        }

        // Update a scenario
        public async Task<bool> UpdateScenarioAsync(TestScenario scenario)
        {
            var existingScenario = await _context.TestScenarios.FindAsync(scenario.Id);
            
            if (existingScenario == null)
            {
                _logger.LogWarning($"Test scenario not found for update: {scenario.Id}");
                return false;
            }
            
            existingScenario.Name = scenario.Name;
            existingScenario.Description = scenario.Description;
            existingScenario.IsActive = scenario.IsActive;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Updated test scenario: {scenario.Id} - {scenario.Name}");
            
            return true;
        }

        // Delete a scenario
        public async Task<bool> DeleteScenarioAsync(int id)
        {
            var scenario = await _context.TestScenarios
                .Include(ts => ts.Steps)
                .FirstOrDefaultAsync(ts => ts.Id == id);
            
            if (scenario == null)
            {
                _logger.LogWarning($"Test scenario not found for deletion: {id}");
                return false;
            }
            
            _context.ScenarioSteps.RemoveRange(scenario.Steps);
            _context.TestScenarios.Remove(scenario);
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Deleted test scenario: {id}");
            
            return true;
        }

        // Add a step to a scenario
        public async Task<ScenarioStep> AddStepToScenarioAsync(int scenarioId, ScenarioStep step)
        {
            var scenario = await _context.TestScenarios
                .Include(ts => ts.Steps)
                .FirstOrDefaultAsync(ts => ts.Id == scenarioId);
            
            if (scenario == null)
            {
                _logger.LogWarning($"Test scenario not found when adding step: {scenarioId}");
                return null;
            }
            
            // Set the order to be the next in sequence
            step.Order = scenario.Steps.Count > 0 ? scenario.Steps.Max(s => s.Order) + 1 : 1;
            step.TestScenarioId = scenarioId;
            
            _context.ScenarioSteps.Add(step);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Added step {step.Id} to scenario {scenarioId}");
            
            return step;
        }

        // Update a step
        public async Task<bool> UpdateStepAsync(ScenarioStep step)
        {
            var existingStep = await _context.ScenarioSteps.FindAsync(step.Id);
            
            if (existingStep == null)
            {
                _logger.LogWarning($"Scenario step not found for update: {step.Id}");
                return false;
            }
            
            existingStep.Name = step.Name;
            existingStep.Description = step.Description;
            existingStep.Order = step.Order;
            existingStep.IsActive = step.IsActive;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Updated step: {step.Id}");
            
            return true;
        }

        // Remove a step from a scenario
        public async Task<bool> RemoveStepFromScenarioAsync(int stepId)
        {
            var step = await _context.ScenarioSteps.FindAsync(stepId);
            
            if (step == null)
            {
                _logger.LogWarning($"Scenario step not found for removal: {stepId}");
                return false;
            }
            
            _context.ScenarioSteps.Remove(step);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Removed step: {stepId}");
            
            return true;
        }

        // Record a request/response pair to a scenario
        public async Task<ScenarioStep> RecordRequestResponseAsync(int scenarioId, BackendService.Models.HttpRequest request, BackendService.Models.HttpResponse response)
        {
            var scenario = await _context.TestScenarios.FindAsync(scenarioId);
            
            if (scenario == null)
            {
                _logger.LogWarning($"Test scenario not found for recording: {scenarioId}");
                return null;
            }
            
            // Save the request and response
            _context.HttpRequests.Add(request);
            _context.HttpResponses.Add(response);
            await _context.SaveChangesAsync();
            
            // Create a new step
            var step = new ScenarioStep
            {
                TestScenarioId = scenarioId,
                HttpRequestId = request.Id,
                HttpResponseId = response.Id,
                Name = $"Step {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                Description = $"Recorded {request.Method} {request.Url}",
                IsActive = true
            };
            
            return await AddStepToScenarioAsync(scenarioId, step);
        }
        
        // Record an interaction when recording is active
        public async Task RecordInteractionAsync(BackendService.Models.HttpRequest request, BackendService.Models.HttpResponse response)
        {
            if (!IsRecording || !_recordingScenarioId.HasValue)
            {
                _logger.LogInformation("Not recording, ignoring interaction");
                return;
            }
            
            await RecordRequestResponseAsync(_recordingScenarioId.Value, request, response);
            _logger.LogInformation($"Recorded interaction to scenario {_recordingScenarioId.Value}");
        }
        
        // Replay a scenario
        public async Task<List<ScenarioStep>> ReplayScenarioAsync(int scenarioId)
        {
            var scenario = await _context.TestScenarios
                .Include(ts => ts.Steps)
                    .ThenInclude(s => s.HttpRequest)
                .Include(ts => ts.Steps)
                    .ThenInclude(s => s.HttpResponse)
                .FirstOrDefaultAsync(ts => ts.Id == scenarioId);
                
            if (scenario == null)
            {
                _logger.LogWarning($"Test scenario not found for replay: {scenarioId}");
                return new List<ScenarioStep>();
            }
            
            return scenario.Steps.OrderBy(s => s.Order).ToList();
        }

        // Execute a test scenario (replay all steps)
        public async Task<List<(BackendService.Models.HttpRequest, BackendService.Models.HttpResponse)>> ExecuteScenarioAsync(int scenarioId)
        {
            var scenario = await GetScenarioByIdAsync(scenarioId);
            
            if (scenario == null)
            {
                _logger.LogWarning($"Test scenario not found for execution: {scenarioId}");
                return null;
            }
            
            var results = new List<(BackendService.Models.HttpRequest, BackendService.Models.HttpResponse)>();
            
            // Order steps by the 'Order' property
            var orderedSteps = scenario.Steps.OrderBy(s => s.Order).ToList();
            
            foreach (var step in orderedSteps)
            {
                if (!step.IsActive || step.HttpRequest == null)
                {
                    continue;
                }
                
                var request = step.HttpRequest;
                var response = step.HttpResponse;
                
                // TODO: We could actually execute the request here if needed
                // For now, just return the stored request/response pairs
                
                results.Add((request, response));
                
                _logger.LogInformation($"Executed step {step.Id} of scenario {scenarioId}");
            }
            
            return results;
        }
    }
}