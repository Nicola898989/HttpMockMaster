using System;
using System.Collections.Generic;
using NUnit.Framework;
using BackendService;
using BackendService.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using System.Linq;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class ComparisonServiceTests
    {
        private Mock<ILogger<ComparisonService>> _mockLogger;
        private Mock<DatabaseContext> _mockDbContext;
        private ComparisonService _comparisonService;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<ComparisonService>>();
            _mockDbContext = new Mock<DatabaseContext>();
            _comparisonService = new ComparisonService(_mockDbContext.Object, _mockLogger.Object);
        }

        [Test]
        public void Compare_NullRequests_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _comparisonService.CompareRequests(null));
        }

        [Test]
        public void Compare_EmptyRequests_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _comparisonService.CompareRequests(new List<int>()));
        }

        [Test]
        public void Compare_SingleRequest_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _comparisonService.CompareRequests(new List<int> { 1 }));
        }

        [Test]
        public async Task CompareRequests_ValidIds_ReturnsComparisonResult()
        {
            // Arrange
            var requestIds = new List<int> { 1, 2 };
            var mockRequests = new List<HttpRequest>
            {
                new HttpRequest
                {
                    Id = 1,
                    Method = "GET",
                    Url = "http://api.example.com/resource",
                    Headers = "Accept: application/json",
                    Body = "",
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    Response = new HttpResponse
                    {
                        Id = 10,
                        StatusCode = 200,
                        Headers = "Content-Type: application/json",
                        Body = "{\"status\":\"success\",\"data\":{\"id\":1}}",
                        Timestamp = DateTime.UtcNow.AddMinutes(-5)
                    }
                },
                new HttpRequest
                {
                    Id = 2,
                    Method = "GET",
                    Url = "http://api.example.com/resource",
                    Headers = "Accept: application/json",
                    Body = "",
                    Timestamp = DateTime.UtcNow,
                    Response = new HttpResponse
                    {
                        Id = 20,
                        StatusCode = 404,
                        Headers = "Content-Type: application/json",
                        Body = "{\"status\":\"error\",\"message\":\"Not found\"}",
                        Timestamp = DateTime.UtcNow
                    }
                }
            };

            var mockQueryable = mockRequests.AsQueryable();
            _mockDbContext.Setup(db => db.GetRequestsById(requestIds)).ReturnsAsync(mockRequests);

            // Act
            var result = await _comparisonService.CompareRequests(requestIds);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(2, result.Requests.Count);
            Assert.AreEqual(6, result.Differences.Count);
        }

        [Test]
        public void CompareResponses_DifferentStatusCodes_DetectedAsDifference()
        {
            // Arrange
            var response1 = new HttpResponse { StatusCode = 200 };
            var response2 = new HttpResponse { StatusCode = 404 };

            // Act
            var differences = _comparisonService.CompareResponses(response1, response2);

            // Assert
            Assert.Contains("StatusCode", differences);
        }

        [Test]
        public void CompareResponses_DifferentResponseBodies_DetectedAsDifference()
        {
            // Arrange
            var response1 = new HttpResponse 
            { 
                StatusCode = 200,
                Body = "{\"status\":\"success\"}"
            };
            
            var response2 = new HttpResponse 
            { 
                StatusCode = 200,
                Body = "{\"status\":\"error\"}" 
            };

            // Act
            var differences = _comparisonService.CompareResponses(response1, response2);

            // Assert
            Assert.Contains("ResponseBody", differences);
        }

        [Test]
        public void CompareResponses_DifferentHeaders_DetectedAsDifference()
        {
            // Arrange
            var response1 = new HttpResponse 
            { 
                StatusCode = 200,
                Headers = "Content-Type: application/json"
            };
            
            var response2 = new HttpResponse 
            { 
                StatusCode = 200,
                Headers = "Content-Type: text/html" 
            };

            // Act
            var differences = _comparisonService.CompareResponses(response1, response2);

            // Assert
            Assert.Contains("ResponseHeaders", differences);
        }

        [Test]
        public void GetDiff_JsonObjects_ReturnsStructuredDiff()
        {
            // Arrange
            var json1 = "{\"name\":\"John\",\"age\":30,\"city\":\"New York\"}";
            var json2 = "{\"name\":\"John\",\"age\":31,\"city\":\"Boston\"}";

            // Act
            var result = _comparisonService.GetJsonDiff(json1, json2);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.Contains("age", result.Keys.ToList());
            Assert.Contains("city", result.Keys.ToList());
        }
        
        [Test]
        public void GetDiff_InvalidJson_HandlesGracefully()
        {
            // Arrange
            var json1 = "{\"name\":\"John\",\"age\":30}";
            var json2 = "Invalid JSON";

            // Act
            var result = _comparisonService.GetJsonDiff(json1, json2);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(1, result.Count);
            Assert.Contains("_error", result.Keys.ToList());
        }
    }
}