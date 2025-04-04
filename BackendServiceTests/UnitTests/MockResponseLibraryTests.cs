using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendService;
using BackendService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class MockResponseLibraryTests
    {
        private DatabaseContext _dbContext;
        private MockResponseLibrary _mockLibrary;
        private Mock<ILogger<MockResponseLibrary>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            // Setup in-memory database for testing
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            _dbContext = new DatabaseContext(options);
            _mockLogger = new Mock<ILogger<MockResponseLibrary>>();
            
            // Create library service with mocked dependencies
            _mockLibrary = new MockResponseLibrary(_dbContext, _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        [Test]
        public async Task GetAvailableTemplates_ReturnsInitialTemplates()
        {
            // Act
            var templates = await _mockLibrary.GetAvailableTemplatesAsync();
            
            // Assert
            Assert.That(templates, Is.Not.Null);
            Assert.That(templates.Count, Is.GreaterThan(0), "Should contain predefined templates");
            
            // Verify essential templates exist
            var templateNames = templates.Select(t => t.Name).ToList();
            Assert.That(templateNames, Contains.Item("JSON Success (200)"));
            Assert.That(templateNames, Contains.Item("Not Found (404)"));
            Assert.That(templateNames, Contains.Item("Server Error (500)"));
        }
        
        [Test]
        public async Task CreateResponseFromTemplate_ValidTemplate_ReturnsResponse()
        {
            // Act
            var response = await _mockLibrary.CreateResponseFromTemplateAsync("JSON Success (200)");
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.Headers, Contains.Substring("Content-Type: application/json"));
        }
        
        [Test]
        public async Task CreateResponseFromTemplate_WithCustomization_AppliesChanges()
        {
            // Arrange
            var customization = new ResponseCustomization
            {
                StatusCode = 201,
                Headers = new Dictionary<string, string>
                {
                    { "Location", "/api/resource/123" }
                },
                BodyReplacements = new Dictionary<string, string>
                {
                    { "{{id}}", "123" },
                    { "{{message}}", "Resource created" }
                }
            };
            
            // Act
            var response = await _mockLibrary.CreateResponseFromTemplateAsync(
                "JSON Success (200)", customization);
            
            // Assert
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(201));
            Assert.That(response.Headers, Contains.Substring("Location: /api/resource/123"));
            Assert.That(response.Body, Contains.Substring("123"));
            Assert.That(response.Body, Contains.Substring("Resource created"));
        }
        
        [Test]
        public async Task CreateResponseFromTemplate_InvalidTemplate_ThrowsException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await _mockLibrary.CreateResponseFromTemplateAsync("Non-existent Template"));
            
            Assert.That(ex.Message, Contains.Substring("template"));
        }
        
        [Test]
        public async Task SaveCustomTemplateAsync_SavesNewTemplate()
        {
            // Arrange
            var template = new ResponseTemplate
            {
                Name = "Custom XML Response",
                Description = "A custom XML response template",
                StatusCode = 200,
                Headers = "Content-Type: application/xml",
                Body = "<?xml version=\"1.0\"?><response><status>success</status><message>{{message}}</message></response>"
            };
            
            // Act
            await _mockLibrary.SaveCustomTemplateAsync(template);
            var templates = await _mockLibrary.GetAvailableTemplatesAsync();
            
            // Assert
            Assert.That(templates.Any(t => t.Name == template.Name), Is.True);
            
            // Verify the template can be used
            var response = await _mockLibrary.CreateResponseFromTemplateAsync(
                template.Name, 
                new ResponseCustomization { 
                    BodyReplacements = new Dictionary<string, string> { 
                        { "{{message}}", "It works!" } 
                    }
                });
            
            Assert.That(response.Body, Contains.Substring("It works!"));
        }
        
        [Test]
        public async Task DeleteCustomTemplateAsync_RemovesTemplate()
        {
            // Arrange
            var template = new ResponseTemplate
            {
                Name = "Template To Delete",
                Description = "This template will be deleted",
                StatusCode = 200,
                Headers = "Content-Type: text/plain",
                Body = "This is a test template"
            };
            
            await _mockLibrary.SaveCustomTemplateAsync(template);
            
            // Act
            await _mockLibrary.DeleteCustomTemplateAsync(template.Name);
            var templates = await _mockLibrary.GetAvailableTemplatesAsync();
            
            // Assert
            Assert.That(templates.Any(t => t.Name == template.Name), Is.False);
        }
        
        [Test]
        public async Task ApplyCustomization_WithNullCustomization_ReturnsOriginalResponse()
        {
            // Arrange
            var original = new HttpResponse
            {
                StatusCode = 200,
                Headers = "Content-Type: application/json",
                Body = "{\"status\":\"success\"}",
                Timestamp = DateTime.Now.ToString("o")
            };
            
            // Act
            var result = await _mockLibrary.ApplyCustomizationAsync(original, null);
            
            // Assert
            Assert.That(result.StatusCode, Is.EqualTo(original.StatusCode));
            Assert.That(result.Headers, Is.EqualTo(original.Headers));
            Assert.That(result.Body, Is.EqualTo(original.Body));
        }
        
        [Test]
        public async Task GetPredefinedResponseTemplates_ReturnsBuiltInTemplates()
        {
            // Act
            var templates = _mockLibrary.GetPredefinedResponseTemplates();
            
            // Assert
            Assert.That(templates, Is.Not.Null);
            Assert.That(templates.Count, Is.GreaterThan(0));
            
            // Check some common response types
            Assert.That(templates.Any(t => t.StatusCode == 200), Is.True);
            Assert.That(templates.Any(t => t.StatusCode == 201), Is.True);
            Assert.That(templates.Any(t => t.StatusCode == 400), Is.True);
            Assert.That(templates.Any(t => t.StatusCode == 401), Is.True);
            Assert.That(templates.Any(t => t.StatusCode == 403), Is.True);
            Assert.That(templates.Any(t => t.StatusCode == 404), Is.True);
            Assert.That(templates.Any(t => t.StatusCode == 500), Is.True);
        }
    }
}