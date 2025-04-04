using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackendService.Controllers;
using BackendService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class ComparisonControllerTests
    {
        private Mock<IComparisonService> _mockComparisonService;
        private Mock<ILogger<ComparisonController>> _mockLogger;
        private Mock<DatabaseContext> _mockDbContext;
        private ComparisonController _controller;

        [SetUp]
        public void Setup()
        {
            _mockComparisonService = new Mock<IComparisonService>();
            _mockLogger = new Mock<ILogger<ComparisonController>>();
            _mockDbContext = new Mock<DatabaseContext>();
            _controller = new ComparisonController(_mockComparisonService.Object, _mockLogger.Object, _mockDbContext.Object);
        }

        [Test]
        public async Task Compare_ValidIds_ReturnsOkResult()
        {
            // Arrange
            var requestIds = new List<int> { 1, 2 };
            var comparisonResult = new ComparisonResult
            {
                Requests = new List<HttpRequest>
                {
                    new HttpRequest { Id = 1 },
                    new HttpRequest { Id = 2 }
                },
                Differences = new Dictionary<string, object>
                {
                    { "StatusCode", new object[] { 200, 404 } }
                }
            };

            _mockComparisonService.Setup(s => s.CompareRequests(requestIds))
                .ReturnsAsync(comparisonResult);

            // Act
            var result = await _controller.Compare(requestIds);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.AreEqual(comparisonResult, okResult.Value);
        }

        [Test]
        public async Task Compare_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var requestIds = new List<int> { 1, 2 };
            _mockComparisonService.Setup(s => s.CompareRequests(requestIds))
                .ThrowsAsync(new ArgumentException("Invalid request IDs"));

            // Act
            var result = await _controller.Compare(requestIds);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.IsInstanceOf<string>(badRequestResult.Value);
            Assert.That(badRequestResult.Value.ToString(), Contains.Substring("Invalid request IDs"));
        }

        [Test]
        public async Task Compare_ServiceThrowsOtherException_ReturnsInternalServerError()
        {
            // Arrange
            var requestIds = new List<int> { 1, 2 };
            _mockComparisonService.Setup(s => s.CompareRequests(requestIds))
                .ThrowsAsync(new Exception("Internal error"));

            // Act
            var result = await _controller.Compare(requestIds);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.AreEqual(500, objectResult.StatusCode);
        }
        
        [Test]
        public async Task CompareJson_ValidJson_ReturnsOkResult()
        {
            // Arrange
            var jsonDiffRequest = new JsonDiffRequest 
            { 
                Json1 = "{\"name\":\"John\"}", 
                Json2 = "{\"name\":\"Jane\"}" 
            };
            
            var jsonDiff = new Dictionary<string, object> 
            { 
                { "name", new[] { "John", "Jane" } } 
            };
            
            _mockComparisonService.Setup(s => s.GetJsonDiff(jsonDiffRequest.Json1, jsonDiffRequest.Json2))
                .Returns(jsonDiff);

            // Act
            var result = await _controller.CompareJson(jsonDiffRequest);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.AreEqual(jsonDiff, okResult.Value);
        }
        
        [Test]
        public async Task CompareJson_InvalidRequest_ReturnsBadRequest()
        {
            // Arrange
            _controller.ModelState.AddModelError("Json1", "Required");

            // Act
            var result = await _controller.CompareJson(new JsonDiffRequest());

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result.Result);
        }

        [Test]
        public async Task CompareResponses_ValidIds_ReturnsOkResult()
        {
            // Arrange
            var responseId1 = 10;
            var responseId2 = 20;
            var response1 = new HttpResponse { Id = responseId1, StatusCode = 200 };
            var response2 = new HttpResponse { Id = responseId2, StatusCode = 404 };
            var differences = new Dictionary<string, object> { { "StatusCode", new[] { 200, 404 } } };

            _mockDbContext.Setup(db => db.Responses.FindAsync(responseId1))
                .ReturnsAsync(response1);
            _mockDbContext.Setup(db => db.Responses.FindAsync(responseId2))
                .ReturnsAsync(response2);
            _mockComparisonService.Setup(s => s.CompareResponses(response1, response2))
                .Returns(differences);

            // Act
            var result = await _controller.CompareResponses(responseId1, responseId2);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result.Result);
            var okResult = result.Result as OkObjectResult;
            Assert.AreEqual(differences, okResult.Value);
        }

        [Test]
        public async Task CompareResponses_ResponseNotFound_ReturnsNotFound()
        {
            // Arrange
            var responseId1 = 10;
            var responseId2 = 20;
            
            _mockDbContext.Setup(db => db.Responses.FindAsync(responseId1))
                .ReturnsAsync((HttpResponse)null);

            // Act
            var result = await _controller.CompareResponses(responseId1, responseId2);

            // Assert
            Assert.IsInstanceOf<NotFoundObjectResult>(result.Result);
        }

        [Test]
        public async Task CompareResponses_Exception_ReturnsInternalServerError()
        {
            // Arrange
            var responseId1 = 10;
            var responseId2 = 20;
            var response1 = new HttpResponse { Id = responseId1 };
            var response2 = new HttpResponse { Id = responseId2 };

            _mockDbContext.Setup(db => db.Responses.FindAsync(responseId1))
                .ReturnsAsync(response1);
            _mockDbContext.Setup(db => db.Responses.FindAsync(responseId2))
                .ReturnsAsync(response2);
            _mockComparisonService.Setup(s => s.CompareResponses(response1, response2))
                .Throws(new Exception("Test exception"));

            // Act
            var result = await _controller.CompareResponses(responseId1, responseId2);

            // Assert
            Assert.IsInstanceOf<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.AreEqual(500, objectResult.StatusCode);
        }
    }
}