using NUnit.Framework;
using GameServer;
using System.Text.Json;
using System.Net.WebSockets;
using NSubstitute;
using System.Threading.Tasks;
using GameServer.Common.Messages;

namespace GameServer.Tests
{
    public class SendGiftHandlerTests
    {
        private PlayerManager _playerManager;
        private SendGiftHandler _handler;
        private WebSocket _mockWebSocket;

        [SetUp]
        public void Setup()
        {
            _playerManager = new PlayerManager();
            _handler = new SendGiftHandler(_playerManager);
            _mockWebSocket = Substitute.For<WebSocket>();
        }

        [Test]
        public async Task HandleMessageAsync_Should_ReturnSuccess_WhenGiftSentSuccessfully()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "sender-1", _mockWebSocket);
            _playerManager.AddPlayer("device-456", "recipient-1", _mockWebSocket);
            var sender = _playerManager.GetPlayer("sender-1");
            await sender.UpdateResource("coins", 100);

            var message = new SendGiftMessage { SenderPlayerId = "sender-1", FriendPlayerId = "recipient-1", ResourceType = "coins", ResourceValue = 50 };
            var payload = JsonSerializer.SerializeToElement(message);

            // Act
            var result = await _handler.HandleMessageAsync(payload, _mockWebSocket);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Success\""));
            var updatedSender = _playerManager.GetPlayer("sender-1");
            var updatedRecipient = _playerManager.GetPlayer("recipient-1");

            var senderCoints = await updatedSender.GetResource("coins");
            var recipientCoints = await updatedRecipient.GetResource("coins");

            Assert.That(senderCoints, Is.EqualTo(50));
            Assert.That(recipientCoints, Is.EqualTo(50));
        }

        [Test]
        public async Task HandleMessageAsync_Should_ReturnError_WhenSenderHasInsufficientResources()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "sender-1", _mockWebSocket);
            _playerManager.AddPlayer("device-456", "recipient-1", _mockWebSocket);
            var sender = _playerManager.GetPlayer("sender-1");
            await sender.UpdateResource("coins", 30);  // Sender has only 30 coins

            var message = new SendGiftMessage { SenderPlayerId = "sender-1", FriendPlayerId = "recipient-1", ResourceType = "coins", ResourceValue = 50 };
            var payload = JsonSerializer.SerializeToElement(message);

            // Act
            var result = await _handler.HandleMessageAsync(payload, _mockWebSocket);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Insufficient coins\""));
        }

        [Test]
        public async Task HandleMessageAsync_Should_ReturnError_WhenRecipientNotFound()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "sender-1", _mockWebSocket);
            var sender = _playerManager.GetPlayer("sender-1");
            await sender.UpdateResource("coins", 100);

            var message = new SendGiftMessage { SenderPlayerId = "sender-1", FriendPlayerId = "non-existent", ResourceType = "coins", ResourceValue = 50 };
            var payload = JsonSerializer.SerializeToElement(message);

            // Act
            var result = await _handler.HandleMessageAsync(payload, _mockWebSocket);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Sender or friend not found\""));
        }

        [Test]
        public async Task HandleMessageAsync_Should_ReturnError_WhenInvalidResourceType()
        {
            // Arrange
            _playerManager.AddPlayer("device-123", "sender-1", _mockWebSocket);
            _playerManager.AddPlayer("device-456", "recipient-1", _mockWebSocket);

            var message = new SendGiftMessage { SenderPlayerId = "sender-1", FriendPlayerId = "recipient-1", ResourceType = "invalid_resource", ResourceValue = 50 };
            var payload = JsonSerializer.SerializeToElement(message);

            // Act
            var result = await _handler.HandleMessageAsync(payload, _mockWebSocket);

            // Assert
            Assert.That(result, Does.Contain("\"Status\":\"Error\""));
            Assert.That(result, Does.Contain("\"Message\":\"Invalid resource type\""));
        }
    }
}
