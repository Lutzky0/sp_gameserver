using NUnit.Framework;
using GameServer.Managers;
using System.Net.WebSockets;
using NSubstitute;

namespace GameServer.Tests
{
    public class PlayerStateTests
    {
        private PlayerState _playerState;

        [SetUp]
        public void Setup()
        {
            _playerState = new PlayerState("device-123","player-blabla", Substitute.For<WebSocket>());
        }

        [Test]
        public async Task GetResource_Should_ReturnZero_WhenResourceNotSet()
        {
            // Act
            int coins = await _playerState.GetResource("coins");

            // Assert
            Assert.That(coins, Is.EqualTo(0));  // Default value should be 0 if resource isn't set
        }

        [Test]
        public async Task UpdateResource_Should_ChangeResourceValue()
        {
            // Act
            await _playerState.UpdateResource("coins", 100);

            // Assert
            int coins = await _playerState.GetResource("coins");
            Assert.That(coins, Is.EqualTo(100));
        }

        [Test]
        public async Task UpdateResource_Should_NotAllowNegativeValue()
        {
            // Act
            await _playerState.UpdateResource("coins", 50);
            var canUpdate = await _playerState.CanUpdateResource("coins", -51);

            // Assert
            Assert.That(canUpdate, Is.False);  // Not enough coins to subtract
        }

        [Test]
        public async Task CanUpdateResource_Should_ReturnTrue_IfResourceUpdateIsValid()
        {
            // Arrange
            await _playerState.UpdateResource("coins", 100);
            var canUdate = await _playerState.CanUpdateResource("coins", -50);

            // Act & Assert
            Assert.That(canUdate, Is.True);  // Should allow removing 50 coins
        }

        [Test]
        public async Task CanUpdateResource_Should_ReturnFalse_IfUpdateWouldCauseNegative()
        {
            // Act
            await _playerState.UpdateResource("coins", 50);
            var canUpdate = await _playerState.CanUpdateResource("coins", -60);

            // Assert
            Assert.That(canUpdate, Is.False);  // Insufficient coins
        }
    }
}
