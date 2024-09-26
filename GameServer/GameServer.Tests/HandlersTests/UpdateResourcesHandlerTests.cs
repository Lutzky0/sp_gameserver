using NUnit.Framework;
using System.Text.Json;
using System.Net.WebSockets;
using NSubstitute;
using GameServer.Common.Messages;

namespace GameServer.Tests
{
    public class UpdateResourcesHandlerTests
    {
        private PlayerManager _playerManager;
        private UpdateResourcesHandler _handler;
        private WebSocket _mockWebSocket;

        [SetUp]
        public void Setup()
        {
            _playerManager = new PlayerManager();
            _handler = new UpdateResourcesHandler(_playerManager);
            _mockWebSocket = Substitute.For<WebSocket>();
        }

        [Test]
        public async Task HandleMessageAsync_Should_ReturnSuccess_WhenResourcesUpdated()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "player-1", _mockWebSocket);
            var player = _playerManager.GetPlayer("player-1");
            await player.UpdateResource("coins", 100);  // Set initial resource

            var message = new UpdateResourcesMessage { PlayerId = "player-1", ResourceType = "coins", ResourceValue = 50 };
            var payload = JsonSerializer.SerializeToElement(message);

            // Act
            var result = await _handler.HandleMessageAsync(payload, _mockWebSocket);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Success\""));
            var updatedPlayer = _playerManager.GetPlayer("player-1");
            var updatedPlayerCoins = await updatedPlayer.GetResource("coins");
            Assert.That(updatedPlayerCoins, Is.EqualTo(150));  // Updated to 150
        }

        [Test]
        public async Task HandleMessageAsync_Should_ReturnError_WhenPlayerNotFound()
        {
            // Arrange
            var message = new UpdateResourcesMessage { PlayerId = "non-existent-player", ResourceType = "coins", ResourceValue = 50 };
            var payload = JsonSerializer.SerializeToElement(message);

            // Act
            var result = await _handler.HandleMessageAsync(payload, _mockWebSocket);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Player not found\""));
        }

        [Test]
        public async Task HandleMessageAsync_Should_ReturnError_WhenInsufficientResources()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "player-1", _mockWebSocket);
            var player = _playerManager.GetPlayer("player-1");
            await player.UpdateResource("coins", 50);  // Player has only 50 coins

            var message = new UpdateResourcesMessage { PlayerId = "player-1", ResourceType = "coins", ResourceValue = -60 };
            var payload = JsonSerializer.SerializeToElement(message);

            // Act
            var result = await _handler.HandleMessageAsync(payload, _mockWebSocket);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Insufficient coins\""));
        }

        [Test]
        public async Task HandleMessageAsync_Should_ReturnError_WhenInvalidResourceType()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "player-1", _mockWebSocket);

            var message = new UpdateResourcesMessage { PlayerId = "player-1", ResourceType = "invalid_resource", ResourceValue = 50 };
            var payload = JsonSerializer.SerializeToElement(message);

            // Act
            var result = await _handler.HandleMessageAsync(payload, _mockWebSocket);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Invalid resource type\""));
        }
    }
}
