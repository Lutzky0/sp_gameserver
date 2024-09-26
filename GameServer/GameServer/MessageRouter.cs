using System.Net.WebSockets;
using System.Text.Json;
using GameServer.Handlers;

namespace GameServer
{
    public class MessageRouter
    {
        private readonly PlayerManager _playerManager;
        private readonly Dictionary<MessageType, IHandler> _handlers;

        public MessageRouter(PlayerManager playerManager)
        {
            _playerManager = playerManager;

            // Initialize the handlers dictionary with instances of the handlers
            _handlers = new Dictionary<MessageType, IHandler>
            {
                { MessageType.Login, new LoginHandler(_playerManager) },
                { MessageType.UpdateResources, new UpdateResourcesHandler(_playerManager) },
                { MessageType.SendGift, new SendGiftHandler(_playerManager) }
            };
        }

        public async Task<string> RouteMessageAsync(string message, WebSocket socket)
        {
            // Deserialize the incoming message to find out the type.
            var messageObject = JsonSerializer.Deserialize<SocketMessage>(message);

            if (messageObject == null || string.IsNullOrEmpty(messageObject.Type))
                return "Invalid message format";

            // Convert the message type string to the MessageType enum
            MessageType messageType = GetMessageType(messageObject.Type);

            // Try to get the appropriate handler from the dictionary
            if (_handlers.TryGetValue(messageType, out var handler))
            {
                // Call the HandleMessageAsync method in the handler
                return await handler.HandleMessageAsync(messageObject.Payload, socket);
            }
            else
            {
                return "Unknown message type";
            }
        }

        // Helper method to map string to enum
        private MessageType GetMessageType(string messageTypeStr)
        {
            return Enum.TryParse(messageTypeStr, true, out MessageType messageType)
                ? messageType
                : MessageType.Unknown; // Return Unknown if the message type doesn't match any enum values
        }
    }

    // Basic structure to hold deserialized socket message.
    public class SocketMessage
    {
        public string Type { get; set; }
        public JsonElement Payload { get; set; }
    }

    public enum MessageType
    {
        Login,
        UpdateResources,
        SendGift,
        Unknown
    }
}