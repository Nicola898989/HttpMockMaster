using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using BackendService;
using BackendService.Models;
using BackendServiceTests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class ProxyServiceTests
    {
        private DatabaseContext _context;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private HttpClient _httpClient;
        private ProxyService _service;
        
        [SetUp]
        public void Setup()
        {
            // Creare un nuovo database in memoria per ogni test
            _context = DatabaseHelper.CreateInMemoryDatabase();
            var logger = DatabaseHelper.CreateLogger<ProxyService>();
            
            // Configurare HttpClient mock per simulare le chiamate di rete
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            
            // Mock del RuleService
            var ruleServiceMock = new Mock<RuleService>();
            
            _service = new ProxyService(_httpClient, _context, logger, ruleServiceMock.Object);
        }
        
        [TearDown]
        public void Cleanup()
        {
            _httpClient.Dispose();
            _context.Dispose();
        }
        
        [Test]
        public async Task ForwardRequestAsync_ReturnsCorrectResponse()
        {
            // Arrange
            string targetDomain = "api.example.com";
            string responseContent = "{ \"success\": true }";
            
            // Configurare la risposta HTTP mock
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };
            responseMessage.Headers.Add("X-Test-Header", "TestValue");
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);
            
            // Creare una simulazione di HttpListenerRequest
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("GET");
            mockRequest.Setup(r => r.Url).Returns(new Uri("http://localhost:8888/api/test"));
            mockRequest.Setup(r => r.Headers).Returns(new WebHeaderCollection());
            
            // Act
            var result = await _service.ForwardRequestAsync(mockRequest.Object, targetDomain);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(responseContent, result.Body);
            StringAssert.Contains("X-Test-Header: TestValue", result.Headers);
            
            // Verificare che il modello di risposta sia stato salvato nel database
            var savedResponse = await _context.Responses.FindAsync(result.Id);
            Assert.IsNotNull(savedResponse);
        }
        
        [Test]
        public async Task ForwardRequestAsync_HandlesErrors()
        {
            // Arrange
            string targetDomain = "api.example.com";
            
            // Configurare la risposta HTTP mock per simulare un errore
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Simulated network error"));
            
            // Creare una simulazione di HttpListenerRequest
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("GET");
            mockRequest.Setup(r => r.Url).Returns(new Uri("http://localhost:8888/api/test"));
            mockRequest.Setup(r => r.Headers).Returns(new WebHeaderCollection());
            
            // Act
            var result = await _service.ForwardRequestAsync(mockRequest.Object, targetDomain);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(500, result.StatusCode);
            StringAssert.Contains("Error forwarding request", result.Body);
            
            // Verificare che il modello di risposta di errore sia stato salvato nel database
            var savedResponse = await _context.Responses.FindAsync(result.Id);
            Assert.IsNotNull(savedResponse);
            Assert.AreEqual(500, savedResponse.StatusCode);
        }
        
        [Test]
        public async Task ForwardRequestAsync_ForwardsRequestBody()
        {
            // Arrange
            string targetDomain = "api.example.com";
            string requestBody = "{ \"test\": \"data\" }";
            string responseContent = "{ \"success\": true }";
            HttpRequestMessage capturedRequest = null;
            
            // Configurare la risposta HTTP mock
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((request, token) => capturedRequest = request)
                .ReturnsAsync(responseMessage);
            
            // Creare una simulazione di HttpListenerRequest con corpo
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("POST");
            mockRequest.Setup(r => r.Url).Returns(new Uri("http://localhost:8888/api/test"));
            mockRequest.Setup(r => r.Headers).Returns(new WebHeaderCollection());
            mockRequest.Setup(r => r.HasEntityBody).Returns(true);
            
            // Creiamo un metodo per intercettare la chiamata a GetRequestBodyAsync
            _service.GetRequestBodyMethodForTest = (req) => Task.FromResult(requestBody);
            
            // Act
            var result = await _service.ForwardRequestAsync(mockRequest.Object, targetDomain);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            
            // Verificare che la richiesta sia stata inoltrata correttamente
            Assert.IsNotNull(capturedRequest);
            Assert.AreEqual(HttpMethod.Post, capturedRequest.Method);
            StringAssert.Contains("api.example.com", capturedRequest.RequestUri.ToString());
        }
        
        [Test]
        public async Task ForwardRequestAsync_SetsCorrectRequestUri()
        {
            // Arrange
            string targetDomain = "api.example.com";
            string path = "/api/test?param=value";
            HttpRequestMessage capturedRequest = null;
            
            // Configurare la risposta HTTP mock
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((request, token) => capturedRequest = request)
                .ReturnsAsync(responseMessage);
            
            // Creare una simulazione di HttpListenerRequest con percorso e parametri
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("GET");
            mockRequest.Setup(r => r.Url).Returns(new Uri($"http://localhost:8888{path}"));
            mockRequest.Setup(r => r.Headers).Returns(new WebHeaderCollection());
            
            // Act
            var result = await _service.ForwardRequestAsync(mockRequest.Object, targetDomain);
            
            // Assert
            Assert.IsNotNull(capturedRequest);
            StringAssert.Contains($"https://{targetDomain}{path}", capturedRequest.RequestUri.ToString());
        }
        
        [Test]
        public async Task ForwardRequestAsync_CopiesHeaders()
        {
            // Arrange
            string targetDomain = "api.example.com";
            HttpRequestMessage capturedRequest = null;
            
            // Configurare la risposta HTTP mock
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Callback<HttpRequestMessage, CancellationToken>((request, token) => capturedRequest = request)
                .ReturnsAsync(responseMessage);
            
            // Creare una simulazione di HttpListenerRequest con headers
            var headers = new WebHeaderCollection();
            headers.Add("User-Agent", "Test Agent");
            headers.Add("Authorization", "Bearer token123");
            headers.Add("Content-Type", "application/json");
            
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("GET");
            mockRequest.Setup(r => r.Url).Returns(new Uri("http://localhost:8888/api/test"));
            mockRequest.Setup(r => r.Headers).Returns(headers);
            
            // Act
            var result = await _service.ForwardRequestAsync(mockRequest.Object, targetDomain);
            
            // Assert
            Assert.IsNotNull(capturedRequest);
            
            // Verificare che gli headers siano stati copiati
            Assert.True(capturedRequest.Headers.Contains("User-Agent"));
            Assert.True(capturedRequest.Headers.Contains("Authorization"));
            Assert.True(capturedRequest.Content == null || 
                       capturedRequest.Content.Headers.Contains("Content-Type"));
        }
    }
}