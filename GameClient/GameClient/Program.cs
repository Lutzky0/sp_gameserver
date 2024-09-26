using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Serilog;

namespace GameClient
{
    public class Program
    {
        private static ClientWebSocket _client;

        public static async Task Main(string[] args)
        {
            //Logger
            Log.Logger = new LoggerConfiguration()
          .WriteTo.Console()
          .WriteTo.File("logs/client_logs.txt")
          .CreateLogger();

            // Start the WebSocket client and connect to the server
            _client = new ClientWebSocket();
            Uri serverUri = new Uri("ws://localhost:5000/");

            Log.Information("Connecting to the server...");
            await _client.ConnectAsync(serverUri, CancellationToken.None);
            Log.Information("Connected!");

            // Example actions:

            //Login
            await SendLoginMessage("device-111");
            var loginResponse = await ListenForMessage();
            var playerId = ExtractPlayerId(loginResponse);

            //Login2
            await SendLoginMessage("device-222");
            var loginResponse2 = await ListenForMessage();
            var playerId2 = ExtractPlayerId(loginResponse2);


            // Start a background task to listen for messages from the server
            var listenerTask = Task.Run(ListenForMessages);


            //SendUpdateResources
            await SendUpdateResourcesMessage(playerId, "coins", 100);

            //SendGift
            await SendGiftMessage(playerId, playerId2, "coins", 50);

            // Close WebSocket connection gracefully
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
        }

        private static string ExtractPlayerId(string response)
        {
            // Simplified parsing to extract PlayerId from response
            var startIndex = response.IndexOf("\"PlayerId\":\"") + 12;
            var endIndex = response.IndexOf("\"", startIndex);
            return response.Substring(startIndex, endIndex - startIndex);
        }

        private static async Task SendLoginMessage(string deviceId)
        {
            var message = new
            {
                Type = "Login",
                Payload = new
                {
                    UDID = deviceId
                }
            };

            await SendMessageAsync(message);
        }

        private static async Task SendUpdateResourcesMessage(string playerId, string resourceType, int resourceValue)
        {
            var message = new
            {
                Type = "UpdateResources",
                Payload = new
                {
                    PlayerId = playerId,
                    ResourceType = resourceType,
                    ResourceValue = resourceValue
                }
            };

            await SendMessageAsync(message);
        }

        private static async Task SendGiftMessage(string senderPlayerId, string friendPlayerId, string resourceType, int resourceValue)
        {
            var message = new
            {
                Type = "SendGift",
                Payload = new
                {
                    SenderPlayerId = senderPlayerId,
                    FriendPlayerId = friendPlayerId,
                    ResourceType = resourceType,
                    ResourceValue = resourceValue
                }
            };

            await SendMessageAsync(message);
        }

        private static async Task SendMessageAsync(object message)
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonMessage);
            await _client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

            Log.Information("Sent: {jsonMessage}", jsonMessage);
        }

        private static async Task ListenForMessages()
        {
            byte[] buffer = new byte[1024];
            while (_client.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Log.Information("Received: {response}", response);
            }
        }

        private static async Task<string> ListenForMessage()
        {
            byte[] buffer = new byte[1024];
            if (_client.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Log.Information("Received: {response}", response);
                return response;
            }
            return null;

        }
    }
}