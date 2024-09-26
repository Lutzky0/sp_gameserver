using NSubstitute;
using NUnit.Framework;
using System.Net.WebSockets;

namespace GameServer.Tests
{
    public class PlayerManagerTests
    {
        private IPlayerManager _playerManager;
        private WebSocket _mockWebSocket;

        [SetUp]
        public void Setup()
        {
            _playerManager = new PlayerManager();
            _mockWebSocket = Substitute.For<WebSocket>();  // Mocking WebSocket
        }

        [Test]
        public void AddPlayer_Should_AddPlayerSuccessfully()
        {
            // Act
            bool added = _playerManager.AddPlayer("device-123", "player-1", _mockWebSocket);

            // Assert
            Assert.That(added, Is.True);
            Assert.That(_playerManager.GetPlayer("player-1"), Is.Not.Null);
        }

        [Test]
        public void AddPlayer_Should_ReturnFalse_IfPlayerAlreadyExists()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "player-1", _mockWebSocket);

            // Act
            bool addedAgain = _playerManager.AddPlayer("device-123", "player-1", _mockWebSocket);

            // Assert
            Assert.That(addedAgain, Is.False);
        }

        [Test]
        public void GetPlayer_Should_ReturnNull_IfPlayerNotExists()
        {
            // Act
            var player = _playerManager.GetPlayer("player-999");

            // Assert
            Assert.That(player, Is.Null);
        }

        [Test]
        public void IsPlayerConnected_Should_ReturnTrue_IfPlayerIsConnected()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "player-1", _mockWebSocket);

            // Act
            bool isConnected = _playerManager.IsPlayerConnected("device-123");

            // Assert
            Assert.That(isConnected, Is.True);
        }

        [Test]
        public void IsPlayerConnected_Should_ReturnFalse_IfPlayerIsNotConnected()
        {
            // Act
            bool isConnected = _playerManager.IsPlayerConnected("device-999");

            // Assert
            Assert.That(isConnected, Is.False);
        }
    }
}