using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendService;
using BackendService.Controllers;
using BackendService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class MockResponseLibraryControllerTests
    {
        private MockResponseLibraryController _controller;
        private Mock<MockResponseLibrary> _mockLibrary;
        private Mock<ILogger<MockResponseLibraryController>> _mockLogger;
        
        [SetUp]
        public void Setup()
        {
            _mockLibrary = new Mock<MockResponseLibrary>(
                Mock.Of<DatabaseContext>(),
                Mock.Of<ILogger<MockResponseLibrary>>());
                
            _mockLogger = new Mock<ILogger<MockResponseLibraryController>>();
            
            _controller = new MockResponseLibraryController(
                _mockLibrary.Object,
                _mockLogger.Object);
        }
        
        [Test]
        public async Task GetTemplates_ReturnsOkResultWithGroupedTemplates()
        {
            // Arrange
            var templates = new List<ResponseTemplate>
            {
                new ResponseTemplate { 
                    Id = 1, 
                    Name = "Template 1", 
                    Category = "Success", 
                    StatusCode = 200,
                    Description = "Test template 1" 
                },
                new ResponseTemplate { 
                    Id = 2, 
                    Name = "Template 2", 
                    Category = "Error", 
                    StatusCode = 400,
                    Description = "Test template 2" 
                },
                new ResponseTemplate { 
                    Id = 3, 
                    Name = "Template 3", 
                    Category = "Success", 
                    StatusCode = 201,
                    Description = "Test template 3" 
                }
            };
            
            _mockLibrary.Setup(m => m.GetAvailableTemplatesAsync())
                .ReturnsAsync(templates);
            
            // Act
            var result = await _controller.GetTemplates();
            
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            // Verifica che i template siano raggruppati per categoria
            var groupedTemplates = okResult.Value as IEnumerable<dynamic>;
            Assert.That(groupedTemplates, Is.Not.Null);
            Assert.That(groupedTemplates.Count(), Is.EqualTo(2)); // Due categorie: Success e Error
        }
        
        [Test]
        public async Task GetTemplates_HandleExceptions_ReturnsServerError()
        {
            // Arrange
            _mockLibrary.Setup(m => m.GetAvailableTemplatesAsync())
                .ThrowsAsync(new Exception("Test exception"));
            
            // Act
            var result = await _controller.GetTemplates();
            
            // Assert
            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var statusCodeResult = result as ObjectResult;
            Assert.That(statusCodeResult.StatusCode, Is.EqualTo(500));
        }
        
        [Test]
        public async Task GetTemplate_WithValidId_ReturnsTemplate()
        {
            // Arrange
            int templateId = 1;
            var templates = new List<ResponseTemplate>
            {
                new ResponseTemplate { 
                    Id = templateId, 
                    Name = "Test Template", 
                    StatusCode = 200 
                }
            };
            
            _mockLibrary.Setup(m => m.GetAvailableTemplatesAsync())
                .ReturnsAsync(templates);
            
            // Act
            var result = await _controller.GetTemplate(templateId);
            
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var template = okResult.Value as ResponseTemplate;
            Assert.That(template, Is.Not.Null);
            Assert.That(template.Id, Is.EqualTo(templateId));
        }
        
        [Test]
        public async Task GetTemplate_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int templateId = 999; // Non-existent ID
            var templates = new List<ResponseTemplate>
            {
                new ResponseTemplate { Id = 1, Name = "Test Template" }
            };
            
            _mockLibrary.Setup(m => m.GetAvailableTemplatesAsync())
                .ReturnsAsync(templates);
            
            // Act
            var result = await _controller.GetTemplate(templateId);
            
            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }
        
        [Test]
        public async Task SaveTemplate_WithValidTemplate_ReturnsOk()
        {
            // Arrange
            var template = new ResponseTemplate
            {
                Name = "New Template",
                Description = "A new test template",
                StatusCode = 200,
                Headers = "Content-Type: application/json",
                Body = "{\"status\":\"success\"}"
            };
            
            _mockLibrary.Setup(m => m.SaveCustomTemplateAsync(It.IsAny<ResponseTemplate>()))
                .Returns(Task.CompletedTask);
            
            // Act
            var result = await _controller.SaveTemplate(template);
            
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            
            // Verify template was saved with IsSystem = false
            _mockLibrary.Verify(m => m.SaveCustomTemplateAsync(It.Is<ResponseTemplate>(
                t => t.Name == template.Name && t.IsSystem == false)), Times.Once);
        }
        
        [Test]
        public async Task SaveTemplate_WithNullTemplate_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.SaveTemplate(null);
            
            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        
        [Test]
        public async Task SaveTemplate_WithEmptyName_ReturnsBadRequest()
        {
            // Arrange
            var template = new ResponseTemplate
            {
                Name = "",
                StatusCode = 200
            };
            
            // Act
            var result = await _controller.SaveTemplate(template);
            
            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        
        [Test]
        public async Task SaveTemplate_SystemTemplateProtection_ReturnsBadRequest()
        {
            // Arrange
            var template = new ResponseTemplate
            {
                Name = "System Template",
                StatusCode = 200
            };
            
            _mockLibrary.Setup(m => m.SaveCustomTemplateAsync(It.IsAny<ResponseTemplate>()))
                .ThrowsAsync(new InvalidOperationException("Cannot modify system template"));
            
            // Act
            var result = await _controller.SaveTemplate(template);
            
            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        
        [Test]
        public async Task DeleteTemplate_WithValidName_ReturnsOk()
        {
            // Arrange
            string templateName = "Custom Template";
            
            _mockLibrary.Setup(m => m.DeleteCustomTemplateAsync(templateName))
                .Returns(Task.CompletedTask);
            
            // Act
            var result = await _controller.DeleteTemplate(templateName);
            
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
        }
        
        [Test]
        public async Task DeleteTemplate_WithEmptyName_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteTemplate("");
            
            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        
        [Test]
        public async Task DeleteTemplate_SystemTemplateProtection_ReturnsBadRequest()
        {
            // Arrange
            string templateName = "System Template";
            
            _mockLibrary.Setup(m => m.DeleteCustomTemplateAsync(templateName))
                .ThrowsAsync(new InvalidOperationException("Cannot delete system template"));
            
            // Act
            var result = await _controller.DeleteTemplate(templateName);
            
            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        
        [Test]
        public async Task CreateResponse_WithValidRequest_ReturnsResponse()
        {
            // Arrange
            var request = new CreateResponseRequest
            {
                TemplateName = "Test Template",
                Customization = new ResponseCustomization
                {
                    StatusCode = 201,
                    BodyReplacements = new Dictionary<string, string>
                    {
                        { "{{id}}", "123" }
                    }
                }
            };
            
            var response = new HttpResponse
            {
                StatusCode = 201,
                Headers = "Content-Type: application/json",
                Body = "{\"id\":\"123\"}",
                Timestamp = DateTime.UtcNow.ToString("o")
            };
            
            _mockLibrary.Setup(m => m.CreateResponseFromTemplateAsync(
                    request.TemplateName, 
                    request.Customization))
                .ReturnsAsync(response);
            
            // Act
            var result = await _controller.CreateResponse(request);
            
            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var returnedResponse = okResult.Value as HttpResponse;
            Assert.That(returnedResponse, Is.Not.Null);
            Assert.That(returnedResponse.StatusCode, Is.EqualTo(201));
            Assert.That(returnedResponse.Body, Is.EqualTo("{\"id\":\"123\"}"));
        }
        
        [Test]
        public async Task CreateResponse_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CreateResponse(null);
            
            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        
        [Test]
        public async Task CreateResponse_WithEmptyTemplateName_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateResponseRequest
            {
                TemplateName = "",
                Customization = new ResponseCustomization()
            };
            
            // Act
            var result = await _controller.CreateResponse(request);
            
            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }
        
        [Test]
        public async Task CreateResponse_WithInvalidTemplate_ReturnsNotFound()
        {
            // Arrange
            var request = new CreateResponseRequest
            {
                TemplateName = "Non-existent Template",
                Customization = new ResponseCustomization()
            };
            
            _mockLibrary.Setup(m => m.CreateResponseFromTemplateAsync(
                    request.TemplateName, 
                    request.Customization))
                .ThrowsAsync(new ArgumentException("Template not found"));
            
            // Act
            var result = await _controller.CreateResponse(request);
            
            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }
    }
}