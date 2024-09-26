using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using GameServer;
using GameServer.Common.Messages;
using NSubstitute;
using NUnit.Framework;

namespace GameServer.Tests
{
    public class LoginHandlerTests
    {
        [Test]
        public async Task HandleMessageAsync_ValidLoginMessage_ReturnsSuccess()
        {
            // Arrange
            var playerManager = Substitute.For<IPlayerManager>();
            playerManager.IsPlayerConnected(Arg.Any<string>()).Returns(false);
            playerManager.AddPlayer(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WebSocket>()).Returns(true);

            var handler = new LoginHandler(playerManager);
            var loginMessage = new LoginMessage { UDID = "device-123" };
            var payload = JsonSerializer.SerializeToElement(loginMessage);

            // Act
            var result = await handler.HandleMessageAsync(payload, null);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Success\""));
            playerManager.Received(1).AddPlayer("device-123", Arg.Any<string>(), Arg.Any<WebSocket>());
        }

        [Test]
        public async Task HandleMessageAsync_AlreadyConnected_ReturnsError()
        {
            // Arrange
            var playerManager = Substitute.For<IPlayerManager>();
            playerManager.IsPlayerConnected(Arg.Any<string>()).Returns(true);

            var handler = new LoginHandler(playerManager);
            var loginMessage = new LoginMessage { UDID = "device-123" };
            var payload = JsonSerializer.SerializeToElement(loginMessage);

            // Act
            var result = await handler.HandleMessageAsync(payload, null);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Player is already connected\""));
        }

        [Test]
        public async Task HandleMessageAsync_AddPlayerFails_ReturnsError()
        {
            // Arrange
            var playerManager = Substitute.For<IPlayerManager>();
            playerManager.IsPlayerConnected(Arg.Any<string>()).Returns(false);
            playerManager.AddPlayer(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<WebSocket>()).Returns(false);

            var handler = new LoginHandler(playerManager);
            var loginMessage = new LoginMessage { UDID = "device-123" };
            var payload = JsonSerializer.SerializeToElement(loginMessage);

            // Act
            var result = await handler.HandleMessageAsync(payload, null);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Failed to add player\""));
        }

        [Test]
        public async Task HandleMessageAsync_NullDeviceId_ReturnsError()
        {
            // Arrange
            var playerManager = Substitute.For<IPlayerManager>();
            var handler = new LoginHandler(playerManager);
            var loginMessage = new LoginMessage { UDID = null };  // DeviceId is null
            var payload = JsonSerializer.SerializeToElement(loginMessage);

            // Act
            var result = await handler.HandleMessageAsync(payload, null);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Invalid Login message\""));
        }

        [Test]
        public async Task HandleMessageAsync_EmptyDeviceId_ReturnsError()
        {
            // Arrange
            var playerManager = Substitute.For<IPlayerManager>();
            var handler = new LoginHandler(playerManager);
            var loginMessage = new LoginMessage { UDID = string.Empty };  // DeviceId is empty
            var payload = JsonSerializer.SerializeToElement(loginMessage);

            // Act
            var result = await handler.HandleMessageAsync(payload, null);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Invalid Login message\""));
        }
    }
}