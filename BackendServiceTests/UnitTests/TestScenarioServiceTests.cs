using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using BackendService;
using BackendService.Models;
using BackendServiceTests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class TestScenarioServiceTests
    {
        private DatabaseContext _context;
        private TestScenarioService _service;
        
        [SetUp]
        public void Setup()
        {
            // Creare un nuovo database in memoria per ogni test
            _context = DatabaseHelper.CreateInMemoryDatabase();
            var logger = DatabaseHelper.CreateLogger<TestScenarioService>();
            _service = new TestScenarioService(_context, logger);
        }
        
        [TearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }
        
        [Test]
        public async Task GetAllScenariosAsync_ReturnsAllScenarios()
        {
            // Arrange
            await SeedTestScenarios();
            
            // Act
            var result = await _service.GetAllScenariosAsync();
            
            // Assert
            Assert.IsNotNull(result.Scenarios);
            Assert.AreEqual(3, result.Scenarios.Count);
            Assert.AreEqual(3, result.TotalCount);
        }
        
        [Test]
        public async Task GetScenarioByIdAsync_ReturnsCorrectScenario()
        {
            // Arrange
            var scenarios = await SeedTestScenarios();
            var targetId = scenarios[1].Id;
            
            // Act
            var result = await _service.GetScenarioByIdAsync(targetId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(targetId, result.Id);
            Assert.AreEqual("Scenario 2", result.Name);
        }
        
        [Test]
        public async Task GetScenarioByIdAsync_ReturnsNullForInvalidId()
        {
            // Arrange
            await SeedTestScenarios();
            var invalidId = 9999;
            
            // Act
            var result = await _service.GetScenarioByIdAsync(invalidId);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public async Task CreateScenarioAsync_SavesNewScenario()
        {
            // Arrange
            var scenario = new TestScenario
            {
                Name = "Test Scenario",
                Description = "Test Description",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // Act
            var result = await _service.CreateScenarioAsync(scenario);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Id, 0);
            Assert.AreEqual(scenario.Name, result.Name);
            Assert.AreEqual(scenario.Description, result.Description);
            
            // Verificare che sia stato salvato nel database
            var savedScenario = await _context.TestScenarios.FindAsync(result.Id);
            Assert.IsNotNull(savedScenario);
        }
        
        [Test]
        public async Task UpdateScenarioAsync_UpdatesExistingScenario()
        {
            // Arrange
            var scenarios = await SeedTestScenarios();
            var scenario = await _context.TestScenarios.FindAsync(scenarios[0].Id);
            
            scenario.Name = "Updated Name";
            scenario.Description = "Updated Description";
            
            // Act
            var result = await _service.UpdateScenarioAsync(scenario);
            
            // Assert
            Assert.IsTrue(result);
            
            // Verificare che sia stato aggiornato nel database
            var updated = await _context.TestScenarios.FindAsync(scenario.Id);
            Assert.IsNotNull(updated);
            Assert.AreEqual("Updated Name", updated.Name);
            Assert.AreEqual("Updated Description", updated.Description);
        }
        
        [Test]
        public async Task UpdateScenarioAsync_ReturnsFalseForInvalidId()
        {
            // Arrange
            var scenario = new TestScenario
            {
                Id = 9999,
                Name = "Invalid Scenario",
                Description = "Does not exist",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // Act
            var result = await _service.UpdateScenarioAsync(scenario);
            
            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public async Task DeleteScenarioAsync_RemovesScenarioAndItsSteps()
        {
            // Arrange
            var scenarios = await SeedTestScenarios();
            var targetId = scenarios[0].Id;
            
            // Aggiungere alcuni step al scenario
            var steps = new List<ScenarioStep>
            {
                new ScenarioStep
                {
                    ScenarioId = targetId,
                    Order = 1,
                    HttpRequest = new HttpRequest
                    {
                        Url = "https://api.example.com/test1",
                        Method = "GET",
                        Headers = "Content-Type: application/json",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    },
                    HttpResponse = new HttpResponse
                    {
                        StatusCode = 200,
                        Headers = "Content-Type: application/json",
                        Body = "{ \"result\": \"test1\" }",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                },
                new ScenarioStep
                {
                    ScenarioId = targetId,
                    Order = 2,
                    HttpRequest = new HttpRequest
                    {
                        Url = "https://api.example.com/test2",
                        Method = "POST",
                        Headers = "Content-Type: application/json",
                        Body = "{ \"data\": \"test\" }",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    },
                    HttpResponse = new HttpResponse
                    {
                        StatusCode = 201,
                        Headers = "Content-Type: application/json",
                        Body = "{ \"result\": \"test2\" }",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                }
            };
            
            _context.ScenarioSteps.AddRange(steps);
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.DeleteScenarioAsync(targetId);
            
            // Assert
            Assert.IsTrue(result);
            
            // Verificare che lo scenario sia stato rimosso
            var deletedScenario = await _context.TestScenarios.FindAsync(targetId);
            Assert.IsNull(deletedScenario);
            
            // Verificare che gli step siano stati rimossi
            var remainingSteps = await _context.ScenarioSteps
                .Where(s => s.ScenarioId == targetId)
                .ToListAsync();
            Assert.IsEmpty(remainingSteps);
        }
        
        [Test]
        public async Task DeleteScenarioAsync_ReturnsFalseForInvalidId()
        {
            // Arrange
            await SeedTestScenarios();
            var invalidId = 9999;
            
            // Act
            var result = await _service.DeleteScenarioAsync(invalidId);
            
            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public async Task StartRecordingAsync_SetsRecordingStateCorrectly()
        {
            // Arrange
            var scenario = new TestScenario
            {
                Name = "Recording Scenario",
                Description = "For testing recording",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            await _context.TestScenarios.AddAsync(scenario);
            await _context.SaveChangesAsync();
            
            // Act
            await _service.StartRecordingAsync(scenario.Id);
            
            // Assert
            Assert.IsTrue(_service.IsRecording);
            Assert.AreEqual(scenario.Id, _service.RecordingScenarioId);
        }
        
        [Test]
        public async Task StopRecordingAsync_ClearsRecordingState()
        {
            // Arrange
            var scenario = new TestScenario
            {
                Name = "Recording Scenario",
                Description = "For testing recording",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            await _context.TestScenarios.AddAsync(scenario);
            await _context.SaveChangesAsync();
            
            await _service.StartRecordingAsync(scenario.Id);
            
            // Act
            await _service.StopRecordingAsync();
            
            // Assert
            Assert.IsFalse(_service.IsRecording);
            Assert.IsNull(_service.RecordingScenarioId);
        }
        
        [Test]
        public async Task RecordInteractionAsync_AddsStepToScenario()
        {
            // Arrange
            var scenario = new TestScenario
            {
                Name = "Recording Scenario",
                Description = "For testing recording",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            await _context.TestScenarios.AddAsync(scenario);
            await _context.SaveChangesAsync();
            
            await _service.StartRecordingAsync(scenario.Id);
            
            var request = new HttpRequest
            {
                Url = "https://api.example.com/test",
                Method = "GET",
                Headers = "Content-Type: application/json",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            var response = new HttpResponse
            {
                StatusCode = 200,
                Headers = "Content-Type: application/json",
                Body = "{ \"result\": \"success\" }",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // Salvare request e response nel DB perché hanno bisogno di Id validi
            _context.HttpRequests.Add(request);
            _context.HttpResponses.Add(response);
            await _context.SaveChangesAsync();
            
            // Act
            await _service.RecordInteractionAsync(request, response);
            
            // Assert
            var steps = await _context.ScenarioSteps
                .Where(s => s.ScenarioId == scenario.Id)
                .ToListAsync();
            
            Assert.IsNotEmpty(steps);
            Assert.AreEqual(1, steps.Count);
            Assert.AreEqual(request.Id, steps[0].HttpRequest.Id);
            Assert.AreEqual(response.Id, steps[0].HttpResponse.Id);
            Assert.AreEqual(1, steps[0].Order); // Dovrebbe essere il primo step
        }
        
        [Test]
        public async Task RecordInteractionAsync_DoesNothingWhenNotRecording()
        {
            // Arrange
            var request = new HttpRequest
            {
                Url = "https://api.example.com/test",
                Method = "GET",
                Headers = "Content-Type: application/json",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            var response = new HttpResponse
            {
                StatusCode = 200,
                Headers = "Content-Type: application/json",
                Body = "{ \"result\": \"success\" }",
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // Salvare request e response nel DB perché hanno bisogno di Id validi
            _context.HttpRequests.Add(request);
            _context.HttpResponses.Add(response);
            await _context.SaveChangesAsync();
            
            // Act
            await _service.RecordInteractionAsync(request, response);
            
            // Assert
            var steps = await _context.ScenarioSteps.ToListAsync();
            Assert.IsEmpty(steps); // Non dovrebbero essere stati registrati step
        }
        
        [Test]
        public async Task ReplayScenarioAsync_ReturnsScenarioSteps()
        {
            // Arrange
            var scenario = new TestScenario
            {
                Name = "Replay Scenario",
                Description = "For testing replay",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            await _context.TestScenarios.AddAsync(scenario);
            await _context.SaveChangesAsync();
            
            // Aggiungere alcuni step al scenario
            var steps = new List<ScenarioStep>
            {
                new ScenarioStep
                {
                    ScenarioId = scenario.Id,
                    Order = 1,
                    HttpRequest = new HttpRequest
                    {
                        Url = "https://api.example.com/test1",
                        Method = "GET",
                        Headers = "Content-Type: application/json",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    },
                    HttpResponse = new HttpResponse
                    {
                        StatusCode = 200,
                        Headers = "Content-Type: application/json",
                        Body = "{ \"result\": \"test1\" }",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                },
                new ScenarioStep
                {
                    ScenarioId = scenario.Id,
                    Order = 2,
                    HttpRequest = new HttpRequest
                    {
                        Url = "https://api.example.com/test2",
                        Method = "POST",
                        Headers = "Content-Type: application/json",
                        Body = "{ \"data\": \"test\" }",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    },
                    HttpResponse = new HttpResponse
                    {
                        StatusCode = 201,
                        Headers = "Content-Type: application/json",
                        Body = "{ \"result\": \"test2\" }",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                }
            };
            
            _context.ScenarioSteps.AddRange(steps);
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.ReplayScenarioAsync(scenario.Id);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Order);
            Assert.AreEqual(2, result[1].Order);
        }
        
        [Test]
        public async Task ReplayScenarioAsync_ReturnsEmptyListForInvalidId()
        {
            // Arrange
            var invalidId = 9999;
            
            // Act
            var result = await _service.ReplayScenarioAsync(invalidId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }
        
        // Helper per popolare il database con scenari di test
        private async Task<List<TestScenario>> SeedTestScenarios()
        {
            var scenarios = new List<TestScenario>();
            
            for (int i = 1; i <= 3; i++)
            {
                var scenario = new TestScenario
                {
                    Name = $"Scenario {i}",
                    Description = $"Description {i}",
                    CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                _context.TestScenarios.Add(scenario);
                scenarios.Add(scenario);
            }
            
            await _context.SaveChangesAsync();
            return scenarios;
        }
    }
}