using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GameServer.FunctionalTests
{
    [TestFixture]
    public class WebSocketFunctionalTests
    {
        private const string ServerUri = "ws://localhost:5000/";

        private async Task SendMessageAsync(ClientWebSocket socket, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task<string> ReceiveMessageAsync(ClientWebSocket socket)
        {
            var buffer = new byte[1024];
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        private string ExtractPlayerId(string response)
        {
            // Simplified parsing to extract PlayerId from response
            var startIndex = response.IndexOf("\"PlayerId\":\"") + 12;
            var endIndex = response.IndexOf("\"", startIndex);
            return response.Substring(startIndex, endIndex - startIndex);
        }

        [Test]
        public async Task WebSocket_ConnectToServer_ShouldSucceed()
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri(ServerUri), CancellationToken.None);

                // Assert
                Assert.That(client.State, Is.EqualTo(WebSocketState.Open));

                // Properly close the connection
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
        }

        [Test]
        public async Task WebSocket_Login_ShouldReturnPlayerId()
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri(ServerUri), CancellationToken.None);

                // Send login message
                var loginMessage = $"{{\"Type\":\"Login\", \"Payload\": {{\"UDID\":\"device-{Guid.NewGuid()}\"}}}}";
                await SendMessageAsync(client, loginMessage);

                // Receive response from server
                var response = await ReceiveMessageAsync(client);

                // Assert response contains PlayerId
                Assert.That(response, Does.Contain("\"Status\":\"Success\""));
                Assert.That(response, Does.Contain("\"PlayerId\""));

                // Properly close the connection
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
        }

        [Test]
        public async Task WebSocket_UpdateResources_ShouldReturnUpdatedBalance()
        {
            using (var client = new ClientWebSocket())
            {
                await client.ConnectAsync(new Uri(ServerUri), CancellationToken.None);

                // Send login message to get PlayerId
                var loginMessage = $"{{\"Type\":\"Login\", \"Payload\": {{\"UDID\":\"device-{Guid.NewGuid()}\"}}}}";
                await SendMessageAsync(client, loginMessage);
                var loginResponse = await ReceiveMessageAsync(client);

                // Extract PlayerId from login response
                var playerId = ExtractPlayerId(loginResponse);

                // Send UpdateResources message
                var updateResourcesMessage = $"{{\"Type\":\"UpdateResources\", \"Payload\": {{\"PlayerId\":\"{playerId}\", \"ResourceType\":\"coins\", \"ResourceValue\":100}}}}";
                await SendMessageAsync(client, updateResourcesMessage);

                // Receive response from server
                var response = await ReceiveMessageAsync(client);

                // Assert response contains updated balance
                Assert.That(response, Does.Contain("\"Status\":\"Success\""));
                Assert.That(response, Does.Contain("\"ResourceType\":\"coins\""));
                Assert.That(response, Does.Contain("\"Amount\":100"));

                // Properly close the connection
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
        }

        [Test]
        public async Task WebSocket_SendGift_ShouldNotifyFriend()
        {
            using (var client = new ClientWebSocket())
            using (var friendClient = new ClientWebSocket())
            {
                // Connect the first client
                await client.ConnectAsync(new Uri(ServerUri), CancellationToken.None);
                Assert.That(client.State, Is.EqualTo(WebSocketState.Open));

                // Connect the second (friend) client
                await friendClient.ConnectAsync(new Uri(ServerUri), CancellationToken.None);
                Assert.That(friendClient.State, Is.EqualTo(WebSocketState.Open));

                // Log in the first client (sender)
                var loginMessageClient = $"{{\"Type\":\"Login\", \"Payload\": {{\"UDID\":\"device-{Guid.NewGuid()}\"}}}}";
                await SendMessageAsync(client, loginMessageClient);
                var clientResponse = await ReceiveMessageAsync(client);
                var clientId = ExtractPlayerId(clientResponse);

                // Log in the second client (friend)
                var loginMessageFriend = $"{{\"Type\":\"Login\", \"Payload\": {{\"UDID\":\"device-{Guid.NewGuid()}\"}}}}";
                await SendMessageAsync(friendClient, loginMessageFriend);
                var friendResponse = await ReceiveMessageAsync(friendClient);
                var friendId = ExtractPlayerId(friendResponse);

                // Send UpdateResources message
                var updateResourcesMessage = $"{{\"Type\":\"UpdateResources\", \"Payload\": {{\"PlayerId\":\"{clientId}\", \"ResourceType\":\"coins\", \"ResourceValue\":100}}}}";
                await SendMessageAsync(client, updateResourcesMessage);

                // Send a gift from client to friend
                var sendGiftMessage = $"{{\"Type\":\"SendGift\", \"Payload\": {{\"SenderPlayerId\":\"{clientId}\", \"FriendPlayerId\":\"{friendId}\", \"ResourceType\":\"coins\", \"ResourceValue\":50}}}}";
                await SendMessageAsync(client, sendGiftMessage);

                // Check friend client receives the gift notification
                var friendNotification = await ReceiveMessageAsync(friendClient);
                Assert.That(friendNotification, Does.Contain("\"GiftEvent\""));
                Assert.That(friendNotification, Does.Contain("\"ResourceValue\":50"));

                // Properly close both connections
                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                await friendClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
            }
        }
    }
}