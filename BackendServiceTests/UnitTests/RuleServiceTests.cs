using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using BackendService;
using BackendService.Models;
using BackendServiceTests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class RuleServiceTests
    {
        private DatabaseContext _context;
        private RuleService _service;
        
        [SetUp]
        public void Setup()
        {
            // Creare un nuovo database in memoria per ogni test
            _context = DatabaseHelper.CreateInMemoryDatabase();
            var logger = DatabaseHelper.CreateLogger<RuleService>();
            _service = new RuleService(_context, logger);
        }
        
        [TearDown]
        public void Cleanup()
        {
            _context.Dispose();
        }
        
        [Test]
        public async Task GetAllRulesAsync_ReturnsAllRules()
        {
            // Arrange
            await SeedTestRules();
            
            // Act
            var rules = await _service.GetAllRulesAsync();
            
            // Assert
            Assert.IsNotNull(rules);
            Assert.AreEqual(3, rules.Count);
            // Le regole devono essere ordinate per priorità (ascendente)
            Assert.AreEqual(1, rules[0].Priority);
            Assert.AreEqual(2, rules[1].Priority);
            Assert.AreEqual(3, rules[2].Priority);
        }
        
        [Test]
        public async Task GetRuleByIdAsync_ReturnsCorrectRule()
        {
            // Arrange
            var rules = await SeedTestRules();
            var targetId = rules[1].Id; // Rule 2
            
            // Act
            var result = await _service.GetRuleByIdAsync(targetId);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(targetId, result.Id);
            Assert.AreEqual("Rule 2", result.Name);
        }
        
        [Test]
        public async Task GetRuleByIdAsync_ReturnsNullForInvalidId()
        {
            // Arrange
            await SeedTestRules();
            var invalidId = 9999;
            
            // Act
            var result = await _service.GetRuleByIdAsync(invalidId);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public async Task CreateRuleAsync_SavesNewRule()
        {
            // Arrange
            var rule = new Rule 
            { 
                Name = "Test Rule",
                Description = "Test Description",
                UrlPattern = "https://api.example.com/test",
                Method = "GET",
                IsActive = true,
                Priority = 1,
                Response = new HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = "{ \"result\": \"success\" }",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
            
            // Act
            var result = await _service.CreateRuleAsync(rule);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Id, 0);
            Assert.AreEqual(rule.Name, result.Name);
            Assert.AreEqual(rule.Description, result.Description);
            Assert.AreEqual(rule.UrlPattern, result.UrlPattern);
            Assert.AreEqual(rule.Method, result.Method);
            Assert.AreEqual(rule.IsActive, result.IsActive);
            Assert.AreEqual(rule.Priority, result.Priority);
            Assert.IsNotNull(result.Response);
            Assert.Greater(result.Response.Id, 0);
            
            // Verificare che sia stato salvato nel database
            var savedRule = await _context.Rules.Include(r => r.Response).FirstOrDefaultAsync(r => r.Id == result.Id);
            Assert.IsNotNull(savedRule);
            Assert.IsNotNull(savedRule.Response);
        }
        
        [Test]
        public async Task UpdateRuleAsync_UpdatesExistingRule()
        {
            // Arrange
            var rules = await SeedTestRules();
            var rule = await _context.Rules.Include(r => r.Response).FirstOrDefaultAsync(r => r.Id == rules[0].Id);
            
            rule.Name = "Updated Name";
            rule.Description = "Updated Description";
            rule.UrlPattern = "https://updated.example.com";
            rule.Method = "POST";
            rule.IsActive = !rule.IsActive;
            rule.Priority = 10;
            rule.Response.StatusCode = 201;
            rule.Response.Body = "Updated body";
            
            // Act
            var result = await _service.UpdateRuleAsync(rule);
            
            // Assert
            Assert.IsTrue(result);
            
            // Verificare che sia stato aggiornato nel database
            var updated = await _context.Rules.Include(r => r.Response).FirstOrDefaultAsync(r => r.Id == rule.Id);
            Assert.IsNotNull(updated);
            Assert.AreEqual("Updated Name", updated.Name);
            Assert.AreEqual("Updated Description", updated.Description);
            Assert.AreEqual("https://updated.example.com", updated.UrlPattern);
            Assert.AreEqual("POST", updated.Method);
            Assert.AreEqual(!rules[0].IsActive, updated.IsActive);
            Assert.AreEqual(10, updated.Priority);
            Assert.AreEqual(201, updated.Response.StatusCode);
            Assert.AreEqual("Updated body", updated.Response.Body);
        }
        
        [Test]
        public async Task UpdateRuleAsync_ReturnsFalseForInvalidId()
        {
            // Arrange
            var rule = new Rule 
            { 
                Id = 9999,
                Name = "Invalid Rule", 
                Description = "Does not exist",
                UrlPattern = "https://invalid.example.com",
                Method = "GET",
                IsActive = true,
                Priority = 1,
                Response = new HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = "{ \"result\": \"success\" }",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
            
            // Act
            var result = await _service.UpdateRuleAsync(rule);
            
            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public async Task DeleteRuleAsync_RemovesRuleAndItsResponse()
        {
            // Arrange
            var rules = await SeedTestRules();
            var targetId = rules[0].Id;
            var rule = await _context.Rules.Include(r => r.Response).FirstOrDefaultAsync(r => r.Id == targetId);
            var responseId = rule.Response.Id;
            
            // Act
            await _service.DeleteRuleAsync(targetId);
            
            // Assert
            var ruleStillExists = await _context.Rules.AnyAsync(r => r.Id == targetId);
            Assert.IsFalse(ruleStillExists);
            
            // Verificare che la regola sia stata rimossa
            var deletedRule = await _context.Rules.FindAsync(targetId);
            Assert.IsNull(deletedRule);
            
            // Verificare che la risposta sia stata rimossa
            var deletedResponse = await _context.Responses.FindAsync(responseId);
            Assert.IsNull(deletedResponse);
        }
        
        [Test]
        public async Task DeleteRuleAsync_ReturnsFalseForInvalidId()
        {
            // Arrange
            await SeedTestRules();
            var invalidId = 9999;
            
            // Act
            var result = await _service.DeleteRuleAsync(invalidId);
            
            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public async Task FindMatchingRuleAsync_ReturnsMatchingRule()
        {
            // Arrange
            await SeedTestRules();
            
            // Aggiungere regole specifiche per questo test
            var rule = new Rule
            {
                Name = "Exact Match Rule",
                Description = "Matches exact URL and method",
                UrlPattern = "https://api.example.com/exact",
                Method = "GET",
                IsActive = true,
                Priority = 1,
                Response = new HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = "{ \"result\": \"exact match\" }",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
            
            await _service.CreateRuleAsync(rule);
            
            // Creare una simulazione di HttpListenerRequest
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("GET");
            mockRequest.Setup(r => r.Url).Returns(new Uri("https://api.example.com/exact"));
            
            // Act
            var result = await _service.FindMatchingRuleAsync(mockRequest.Object);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Exact Match Rule", result.Name);
        }
        
        [Test]
        public async Task FindMatchingRuleAsync_ReturnsPartialMatchRule()
        {
            // Arrange
            await SeedTestRules();
            
            // Aggiungere regole specifiche per questo test
            var rule = new Rule
            {
                Name = "Partial Match Rule",
                Description = "Matches partial URL",
                UrlPattern = "api.example.com", // Pattern parziale
                Method = "GET",
                IsActive = true,
                Priority = 1,
                Response = new HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = "{ \"result\": \"partial match\" }",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
            
            await _service.CreateRuleAsync(rule);
            
            // Creare una simulazione di HttpListenerRequest
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("GET");
            mockRequest.Setup(r => r.Url).Returns(new Uri("https://api.example.com/something/else"));
            
            // Act
            var result = await _service.FindMatchingRuleAsync(mockRequest.Object);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Partial Match Rule", result.Name);
        }
        
        [Test]
        public async Task FindMatchingRuleAsync_ReturnsNullForNoMatch()
        {
            // Arrange
            await SeedTestRules();
            
            // Creare una simulazione di HttpListenerRequest con URL che non corrisponde a nessuna regola
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("PUT"); // Metodo non corrispondente
            mockRequest.Setup(r => r.Url).Returns(new Uri("https://different.example.com"));
            
            // Act
            var result = await _service.FindMatchingRuleAsync(mockRequest.Object);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public async Task FindMatchingRuleAsync_RespectsPriority()
        {
            // Arrange
            await SeedTestRules();
            
            // Aggiungere due regole con priorità diverse che corrisponderanno entrambe alla richiesta
            var rule1 = new Rule
            {
                Name = "Low Priority Rule",
                Description = "Lower priority rule",
                UrlPattern = "api.example.com",
                Method = "GET",
                IsActive = true,
                Priority = 10, // Priorità più bassa
                Response = new HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = "{ \"result\": \"low priority\" }",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
            
            var rule2 = new Rule
            {
                Name = "High Priority Rule",
                Description = "Higher priority rule",
                UrlPattern = "api.example.com",
                Method = "GET",
                IsActive = true,
                Priority = 1, // Priorità più alta
                Response = new HttpResponse
                {
                    StatusCode = 200,
                    Headers = "Content-Type: application/json",
                    Body = "{ \"result\": \"high priority\" }",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
            
            await _service.CreateRuleAsync(rule1);
            await _service.CreateRuleAsync(rule2);
            
            // Creare una simulazione di HttpListenerRequest
            var mockRequest = new Mock<HttpListenerRequest>();
            mockRequest.Setup(r => r.HttpMethod).Returns("GET");
            mockRequest.Setup(r => r.Url).Returns(new Uri("https://api.example.com/test"));
            
            // Act
            var result = await _service.FindMatchingRuleAsync(mockRequest.Object);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("High Priority Rule", result.Name); // Dovrebbe restituire la regola con priorità più alta
        }
        
        // Helper per popolare il database con alcune regole di test
        private async Task<List<Rule>> SeedTestRules()
        {
            var rules = new List<Rule>();
            
            for (int i = 1; i <= 3; i++)
            {
                var rule = new Rule
                {
                    Name = $"Rule {i}",
                    Description = $"Description {i}",
                    UrlPattern = $"https://example.com/test{i}",
                    Method = "GET",
                    IsActive = true,
                    Priority = i,
                    Response = new HttpResponse
                    {
                        StatusCode = 200,
                        Headers = "Content-Type: application/json",
                        Body = $"{{ \"result\": \"test{i}\" }}",
                        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                };
                
                _context.Rules.Add(rule);
                rules.Add(rule);
            }
            
            await _context.SaveChangesAsync();
            return rules;
        }
    }
}