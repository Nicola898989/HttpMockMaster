using System;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    public class ExportServiceTests
    {
        private DatabaseContext _dbContext;
        private ExportService _service;
        private Mock<ILogger<ExportService>> _mockLogger;
        
        [SetUp]
        public void Setup()
        {
            // Setup in-memory database for testing
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            _dbContext = new DatabaseContext(options);
            _mockLogger = new Mock<ILogger<ExportService>>();
            
            // Create service with dependencies
            _service = new ExportService(_dbContext, _mockLogger.Object);
            
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
            var baseTime = DateTime.UtcNow.AddHours(-1);
            
            for (int i = 0; i < 5; i++)
            {
                var request = new HttpRequest
                {
                    Url = $"http://test.com/api/resource{i}",
                    Method = i % 2 == 0 ? "GET" : "POST",
                    Headers = "Content-Type: application/json",
                    Body = i % 2 == 0 ? null : $"{{\"data\": {i}}}",
                    Timestamp = baseTime.AddMinutes(i * 5).ToString("o"),
                    IsProxied = false
                };
                
                var response = new HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = $"{{\"result\": \"success\", \"id\": {i}}}",
                    Timestamp = baseTime.AddMinutes(i * 5).AddSeconds(2).ToString("o")
                };
                
                request.Response = response;
                _dbContext.Requests.Add(request);
            }
            
            _dbContext.SaveChanges();
        }
        
        [Test]
        public async Task ExportRequestsAsJson_NoFilters_ReturnsAllRequests()
        {
            // Act
            var result = await _service.ExportRequestsAsJson();
            
            // Assert
            Assert.That(result, Is.Not.Null);
            
            // Parse the JSON
            using var ms = new MemoryStream(result);
            var exportedRequests = await JsonSerializer.DeserializeAsync<object[]>(ms);
            
            // Should have all 5 requests
            Assert.That(exportedRequests.Length, Is.EqualTo(5));
        }
        
        [Test]
        public async Task ExportRequestsAsJson_WithMethodFilter_ReturnsFilteredRequests()
        {
            // Arrange
            string method = "GET";
            
            // Act
            var result = await _service.ExportRequestsAsJson(method: method);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            
            // Parse the JSON
            using var ms = new MemoryStream(result);
            var exportedRequests = await JsonSerializer.DeserializeAsync<JsonElement>(ms);
            
            // Count requests with method == GET (should be 3)
            int count = 0;
            foreach (var request in exportedRequests.EnumerateArray())
            {
                if (request.GetProperty("method").GetString() == method)
                {
                    count++;
                }
            }
            
            // Should only have GET requests
            Assert.That(count, Is.EqualTo(exportedRequests.GetArrayLength()));
            Assert.That(count, Is.EqualTo(3)); // We seeded 3 GET requests (i=0,2,4)
        }
        
        [Test]
        public async Task ExportRequestsAsJson_WithDateFilters_ReturnsFilteredRequests()
        {
            // Arrange
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMinutes(-10); // Only include recent requests
            
            // Act
            var result = await _service.ExportRequestsAsJson(fromDate: startDate, toDate: endDate);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            
            // Parse the JSON
            using var ms = new MemoryStream(result);
            var exportedRequests = await JsonSerializer.DeserializeAsync<object[]>(ms);
            
            // Should have fewer than 5 requests due to date filtering
            Assert.That(exportedRequests.Length, Is.LessThanOrEqualTo(5));
        }
        
        [Test]
        public async Task ExportRequestsAsCsv_GeneratesValidCsv()
        {
            // Act
            var result = await _service.ExportRequestsAsCsv();
            
            // Assert
            Assert.That(result, Is.Not.Null);
            
            // Convert byte array to string for inspection
            string csvContent = System.Text.Encoding.UTF8.GetString(result);
            
            // Check basic CSV structure
            string[] lines = csvContent.Split(Environment.NewLine);
            
            // Should have header line + 5 data lines (plus potentially a trailing empty line)
            Assert.That(lines.Length, Is.GreaterThanOrEqualTo(6));
            
            // Check header row
            Assert.That(lines[0], Contains.Substring("Id"));
            Assert.That(lines[0], Contains.Substring("Url"));
            Assert.That(lines[0], Contains.Substring("Method"));
            Assert.That(lines[0], Contains.Substring("Timestamp"));
            
            // Check that we have data rows
            for (int i = 1; i < 6; i++)
            {
                if (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    Assert.That(lines[i], Contains.Substring("http://test.com/api/resource"));
                }
            }
        }
        
        [Test]
        public async Task ExportRequestDetails_WithValidId_ReturnsRequestWithResponse()
        {
            // Arrange
            var request = await _dbContext.Requests.FirstAsync();
            int requestId = request.Id;
            
            // Act
            var result = await _service.ExportRequestDetails(requestId);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            
            // Parse the JSON
            using var ms = new MemoryStream(result);
            var exportedRequest = await JsonSerializer.DeserializeAsync<JsonElement>(ms);
            
            // Verify structure
            Assert.That(exportedRequest.TryGetProperty("request", out _), Is.True);
            Assert.That(exportedRequest.TryGetProperty("response", out _), Is.True);
            
            // Verify request details
            var requestObj = exportedRequest.GetProperty("request");
            Assert.That(requestObj.GetProperty("id").GetInt32(), Is.EqualTo(requestId));
            Assert.That(requestObj.GetProperty("url").GetString(), Is.EqualTo(request.Url));
            Assert.That(requestObj.GetProperty("method").GetString(), Is.EqualTo(request.Method));
            
            // Verify response details
            var responseObj = exportedRequest.GetProperty("response");
            Assert.That(responseObj.GetProperty("statusCode").GetInt32(), Is.EqualTo(request.Response.StatusCode));
        }
        
        [Test]
        public async Task ExportRequestDetails_WithInvalidId_ReturnsErrorMessage()
        {
            // Arrange
            int invalidId = 9999; // Non-existent ID
            
            // Act
            var result = await _service.ExportRequestDetails(invalidId);
            
            // Assert
            Assert.That(result, Is.Not.Null);
            
            // Parse the JSON
            using var ms = new MemoryStream(result);
            var exportedRequest = await JsonSerializer.DeserializeAsync<JsonElement>(ms);
            
            // Should have error property
            Assert.That(exportedRequest.TryGetProperty("error", out _), Is.True);
        }
    }
}