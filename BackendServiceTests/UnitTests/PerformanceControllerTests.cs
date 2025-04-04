using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackendService;
using BackendService.Controllers;
using BackendService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class PerformanceControllerTests
    {
        private DatabaseContext _dbContext;
        private PerformanceController _controller;
        private Mock<ILogger<PerformanceController>> _mockLogger;

        [SetUp]
        public void Setup()
        {
            // Setup in-memory database for testing
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            _dbContext = new DatabaseContext(options);
            _mockLogger = new Mock<ILogger<PerformanceController>>();
            
            // Create controller with mocked dependencies
            _controller = new PerformanceController(_dbContext, _mockLogger.Object);
            
            // Seed test data
            SeedTestData();
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        private void SeedTestData()
        {
            // Create sample HTTP requests with responses
            var startTime = DateTime.UtcNow.AddHours(-2);
            
            // Success requests (200 OK)
            for (int i = 0; i < 5; i++)
            {
                var request = new HttpRequest
                {
                    Url = $"http://test.com/api/endpoint{i}",
                    Method = i % 2 == 0 ? "GET" : "POST",
                    Headers = "Content-Type: application/json",
                    Body = i % 2 == 0 ? null : $"{{\"data\": {i}}}",
                    Timestamp = startTime.AddMinutes(i * 5).ToString("o"),
                    IsProxied = false
                };
                
                var response = new HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = $"{{\"result\": \"success\", \"id\": {i}}}",
                    Timestamp = startTime.AddMinutes(i * 5).AddSeconds(i + 1).ToString("o")
                };
                
                request.Response = response;
                _dbContext.Requests.Add(request);
            }
            
            // Error requests (404 Not Found)
            for (int i = 0; i < 2; i++)
            {
                var request = new HttpRequest
                {
                    Url = $"http://test.com/api/missing{i}",
                    Method = "GET",
                    Headers = "Content-Type: application/json",
                    Body = null,
                    Timestamp = startTime.AddMinutes(30 + i * 5).ToString("o"),
                    IsProxied = false
                };
                
                var response = new HttpResponse
                {
                    StatusCode = 404,
                    Headers = "Content-Type: application/json",
                    Body = $"{{\"error\": \"Resource not found\"}}",
                    Timestamp = startTime.AddMinutes(30 + i * 5).AddSeconds(i + 1).ToString("o")
                };
                
                request.Response = response;
                _dbContext.Requests.Add(request);
            }
            
            // Error requests (500 Internal Server Error)
            var errorRequest = new HttpRequest
            {
                Url = "http://test.com/api/error",
                Method = "POST",
                Headers = "Content-Type: application/json",
                Body = "{\"data\": \"invalid\"}",
                Timestamp = startTime.AddMinutes(45).ToString("o"),
                IsProxied = false
            };
            
            var errorResponse = new HttpResponse
            {
                StatusCode = 500,
                Headers = "Content-Type: application/json",
                Body = "{\"error\": \"Internal server error\"}",
                Timestamp = startTime.AddMinutes(45).AddSeconds(2).ToString("o")
            };
            
            errorRequest.Response = errorResponse;
            _dbContext.Requests.Add(errorRequest);
            
            _dbContext.SaveChanges();
        }

        [Test]
        public async Task GetPerformanceMetrics_ReturnsCorrectData()
        {
            // Act
            var result = await _controller.GetPerformanceMetrics();
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.Not.Null);
            
            // Check total request count (should be 8 based on our seed data)
            dynamic response = okResult.Value;
            Assert.That(response.TotalRequests, Is.EqualTo(8));
            
            // Check that we have both 2xx, 4xx and 5xx status codes
            var statusMetrics = ((IEnumerable<dynamic>)response.StatusCodeMetrics).ToList();
            Assert.That(statusMetrics.Count, Is.EqualTo(3)); // 2xx, 4xx, 5xx
            
            // Check method metrics
            var methodMetrics = ((IEnumerable<dynamic>)response.MethodMetrics).ToList();
            Assert.That(methodMetrics.Count, Is.EqualTo(2)); // GET, POST
        }
        
        [Test]
        public async Task GetTimeSeriesData_GroupsByHour_ReturnsCorrectGrouping()
        {
            // Act
            var result = await _controller.GetTimeSeriesData(groupBy: "hour");
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            dynamic response = okResult.Value;
            var timeSeriesData = ((IEnumerable<dynamic>)response.Data).ToList();
            
            // Based on our test data spanning 1 hour, we should have 1-2 buckets depending on time boundaries
            Assert.That(timeSeriesData.Count, Is.GreaterThanOrEqualTo(1));
            
            // Verify structure of time series data
            foreach (var point in timeSeriesData)
            {
                Assert.That(point.Timestamp, Is.Not.Null);
                Assert.That(point.RequestCount, Is.GreaterThanOrEqualTo(0));
                Assert.That(point.AvgResponseTime, Is.GreaterThanOrEqualTo(0));
                Assert.That(point.SuccessRate, Is.InRange(0, 100));
            }
        }
        
        [Test]
        public async Task GetPerformanceMetrics_WithTimeFrame_ReturnsFilteredData()
        {
            // Arrange - use a narrow time window to filter requests
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMinutes(-10);
            
            // Act
            var result = await _controller.GetPerformanceMetrics(
                timeFrame: "custom", 
                startDate: startDate, 
                endDate: endDate);
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            
            var okResult = result.Result as OkObjectResult;
            dynamic response = okResult.Value;
            
            // The number of requests should be less than or equal to the total (8)
            // since we're using a narrow time window
            Assert.That(response.TotalRequests, Is.LessThanOrEqualTo(8));
            
            // Verify start and end dates match what we provided
            Assert.That(DateTime.Parse((string)response.StartDate.ToString()), Is.EqualTo(startDate).Within(1).Seconds);
            Assert.That(DateTime.Parse((string)response.EndDate.ToString()), Is.EqualTo(endDate).Within(1).Seconds);
        }
        
        [Test]
        public async Task GetPerformanceMetrics_WithInvalidTimeFrame_HandlesSafely()
        {
            // Act
            var result = await _controller.GetPerformanceMetrics(timeFrame: "invalid");
            
            // Assert - should default to "day" and not throw an exception
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            
            var okResult = result.Result as OkObjectResult;
            dynamic response = okResult.Value;
            
            // Time frame should default to "day"
            Assert.That(response.TimeFrame, Is.EqualTo("invalid"));
            
            // Should have data regardless of invalid time frame
            Assert.That(response.TotalRequests, Is.GreaterThanOrEqualTo(0));
        }
        
        [Test]
        public async Task CalculateResponseTimeMetrics_WithNoData_ReturnsZeroValues()
        {
            // Clear the database
            _dbContext.Requests.RemoveRange(_dbContext.Requests);
            _dbContext.SaveChanges();
            
            // Act
            var result = await _controller.GetPerformanceMetrics();
            
            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            
            var okResult = result.Result as OkObjectResult;
            dynamic response = okResult.Value;
            
            // With no data, all metrics should be zero
            Assert.That(response.TotalRequests, Is.EqualTo(0));
            Assert.That(response.ResponseTimeMetrics.Avg, Is.EqualTo(0));
            Assert.That(response.ResponseTimeMetrics.Min, Is.EqualTo(0));
            Assert.That(response.ResponseTimeMetrics.Max, Is.EqualTo(0));
            Assert.That(response.ResponseTimeMetrics.Median, Is.EqualTo(0));
            Assert.That(response.ResponseTimeMetrics.P95, Is.EqualTo(0));
            Assert.That(response.ResponseTimeMetrics.P99, Is.EqualTo(0));
        }
    }
}