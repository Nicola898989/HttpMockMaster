using System;
using System.Threading.Tasks;
using BackendService;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace BackendServiceTests.UnitTests
{
    [TestFixture]
    public class NetworkSimulationServiceTests
    {
        private NetworkSimulationService _service;
        private Mock<ILogger<NetworkSimulationService>> _mockLogger;
        
        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<NetworkSimulationService>>();
            _service = new NetworkSimulationService(_mockLogger.Object);
        }
        
        [Test]
        public void Constructor_SetsDefaultValues()
        {
            // Assert default values
            Assert.That(_service.GetSettings().IsEnabled, Is.False);
            Assert.That(_service.GetSettings().LatencyMs, Is.EqualTo(0));
            Assert.That(_service.GetSettings().LatencyVariationMs, Is.EqualTo(0));
            Assert.That(_service.GetSettings().PacketLossPercentage, Is.EqualTo(0));
            Assert.That(_service.GetSettings().PacketCorruptionPercentage, Is.EqualTo(0));
        }
        
        [Test]
        public void UpdateSettings_UpdatesAllProperties()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = true,
                LatencyMs = 200,
                LatencyVariationMs = 50,
                PacketLossPercentage = 5,
                PacketCorruptionPercentage = 2
            };
            
            // Act
            _service.UpdateSettings(settings);
            var updatedSettings = _service.GetSettings();
            
            // Assert
            Assert.That(updatedSettings.IsEnabled, Is.EqualTo(settings.IsEnabled));
            Assert.That(updatedSettings.LatencyMs, Is.EqualTo(settings.LatencyMs));
            Assert.That(updatedSettings.LatencyVariationMs, Is.EqualTo(settings.LatencyVariationMs));
            Assert.That(updatedSettings.PacketLossPercentage, Is.EqualTo(settings.PacketLossPercentage));
            Assert.That(updatedSettings.PacketCorruptionPercentage, Is.EqualTo(settings.PacketCorruptionPercentage));
        }
        
        [Test]
        public void ApplyLatency_WhenDisabled_DoesNotDelay()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = false,
                LatencyMs = 1000 // High latency that would be noticeable if applied
            };
            _service.UpdateSettings(settings);
            
            // Act
            var start = DateTime.UtcNow;
            _service.ApplyLatency().Wait();
            var elapsed = DateTime.UtcNow - start;
            
            // Assert
            // If disabled, it should return almost immediately
            Assert.That(elapsed.TotalMilliseconds, Is.LessThan(50));
        }
        
        [Test]
        public void ApplyLatency_WhenEnabled_DelaysExecution()
        {
            // Arrange
            int latencyMs = 100; // Use a small value for test efficiency
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = true,
                LatencyMs = latencyMs,
                LatencyVariationMs = 0 // No variation for predictable test
            };
            _service.UpdateSettings(settings);
            
            // Act
            var start = DateTime.UtcNow;
            _service.ApplyLatency().Wait();
            var elapsed = DateTime.UtcNow - start;
            
            // Assert
            // Should delay close to the specified latency (allowing some margin for task scheduling)
            Assert.That(elapsed.TotalMilliseconds, Is.GreaterThan(latencyMs * 0.8));
        }
        
        [Test]
        public void ShouldDropPacket_WhenDisabled_ReturnsFalse()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = false,
                PacketLossPercentage = 100 // Would always drop if enabled
            };
            _service.UpdateSettings(settings);
            
            // Act
            bool result = _service.ShouldDropPacket();
            
            // Assert
            Assert.That(result, Is.False);
        }
        
        [Test]
        public void ShouldDropPacket_With100Percent_ReturnsTrue()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = true,
                PacketLossPercentage = 100 // Always drop
            };
            _service.UpdateSettings(settings);
            
            // Act
            bool result = _service.ShouldDropPacket();
            
            // Assert
            Assert.That(result, Is.True);
        }
        
        [Test]
        public void ShouldDropPacket_With0Percent_ReturnsFalse()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = true,
                PacketLossPercentage = 0 // Never drop
            };
            _service.UpdateSettings(settings);
            
            // Act
            bool result = _service.ShouldDropPacket();
            
            // Assert
            Assert.That(result, Is.False);
        }
        
        [Test]
        public void CorruptData_WhenDisabled_ReturnsOriginalData()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = false,
                PacketCorruptionPercentage = 100 // Would always corrupt if enabled
            };
            _service.UpdateSettings(settings);
            
            string original = "Test data that should not be corrupted";
            
            // Act
            string result = _service.CorruptData(original);
            
            // Assert
            Assert.That(result, Is.EqualTo(original));
        }
        
        [Test]
        public void CorruptData_With100Percent_ChangesData()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = true,
                PacketCorruptionPercentage = 100 // Always corrupt
            };
            _service.UpdateSettings(settings);
            
            string original = "Test data that should be corrupted";
            
            // Act
            string result = _service.CorruptData(original);
            
            // Assert
            Assert.That(result, Is.Not.EqualTo(original));
            // Length should be the same
            Assert.That(result.Length, Is.EqualTo(original.Length));
        }
        
        [Test]
        public void CorruptData_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = true,
                PacketCorruptionPercentage = 100 // Always corrupt
            };
            _service.UpdateSettings(settings);
            
            string original = "";
            
            // Act
            string result = _service.CorruptData(original);
            
            // Assert
            Assert.That(result, Is.EqualTo(original));
        }
        
        [Test]
        public void CorruptData_WithNullString_ReturnsNull()
        {
            // Arrange
            var settings = new NetworkSimulationSettings
            {
                IsEnabled = true,
                PacketCorruptionPercentage = 100 // Always corrupt
            };
            _service.UpdateSettings(settings);
            
            string original = null;
            
            // Act
            string result = _service.CorruptData(original);
            
            // Assert
            Assert.That(result, Is.Null);
        }
    }
}