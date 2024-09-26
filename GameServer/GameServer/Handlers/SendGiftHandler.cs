using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GameServer.Common.Messages;
using GameServer.Handlers;
using Serilog;

namespace GameServer
{
    public class SendGiftHandler : IHandler
    {
        private readonly IPlayerManager _playerManager;
        private readonly ResourceTypeManager _resourceTypeManager;

        public SendGiftHandler(IPlayerManager playerManager)
        {
            _playerManager = playerManager;
            _resourceTypeManager = new ResourceTypeManager();
        }

        public async Task<string> HandleMessageAsync(JsonElement payload, WebSocket socket)
        {
            try
            {
                // Deserialize and validate the payload
                var sendGiftMessage = JsonSerializer.Deserialize<SendGiftMessage>(payload.GetRawText());

                if (sendGiftMessage == null || string.IsNullOrEmpty(sendGiftMessage.SenderPlayerId) || string.IsNullOrEmpty(sendGiftMessage.FriendPlayerId))
                {
                    Log.Error("Invalid SendGift message: Missing SenderPlayerId or FriendPlayerId");
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Invalid SendGift message" });
                }

                // Validate the resource type using ResourceTypeManager
                if (!_resourceTypeManager.IsValidResourceType(sendGiftMessage.ResourceType))
                {
                    Log.Error("Invalid resource type {ResourceType} in SendGift message", sendGiftMessage.ResourceType);
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Invalid resource type" });
                }

                var sender = _playerManager.GetPlayer(sendGiftMessage.SenderPlayerId);
                var recipient = _playerManager.GetPlayer(sendGiftMessage.FriendPlayerId);

                if (sender == null || recipient == null)
                {
                    Log.Warning("Sender ({SenderPlayerId}) or Friend ({FriendPlayerId}) not found", sendGiftMessage.SenderPlayerId, sendGiftMessage.FriendPlayerId);
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Sender or friend not found" });
                }

                // Check if the sender has enough resources to send the gift
                if (!await sender.CanUpdateResource(sendGiftMessage.ResourceType, -sendGiftMessage.ResourceValue))
                {
                    Log.Error("Insufficient {ResourceType} for Player {SenderPlayerId}", sendGiftMessage.ResourceType, sender.PlayerId);
                    return JsonSerializer.Serialize(new { Status = "Error", Message = $"Insufficient {sendGiftMessage.ResourceType}" });
                }

                // Deduct resource from the sender
                await sender.UpdateResource(sendGiftMessage.ResourceType, -sendGiftMessage.ResourceValue);

                // Add resource to the recipient
                await recipient.UpdateResource(sendGiftMessage.ResourceType, sendGiftMessage.ResourceValue);

                Log.Information("{ResourceValue} {ResourceType} sent from Player {SenderPlayerId} to Player {RecipientPlayerId}",
                         sendGiftMessage.ResourceValue, sendGiftMessage.ResourceType, sender.PlayerId, recipient.PlayerId);


                Log.Information("Player {SenderPlayerId} successfully sent gift to {FriendPlayerId}", sendGiftMessage.SenderPlayerId, sendGiftMessage.FriendPlayerId);
                // Notify the recipient if they are online
                if (recipient.Socket != null && recipient.Socket.State == WebSocketState.Open)
                {
                    var giftNotification = JsonSerializer.Serialize(new
                    {
                        EventType = "GiftEvent",
                        FromPlayerId = sender.PlayerId,
                        ResourceType = sendGiftMessage.ResourceType,
                        ResourceValue = sendGiftMessage.ResourceValue
                    });

                    // Send notification to recipient's WebSocket
                    var responseBytes = Encoding.UTF8.GetBytes(giftNotification);
                    await recipient.Socket.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, CancellationToken.None);

                    Log.Information("Sent gift notification to Player {0}", recipient.PlayerId);
                }

                return JsonSerializer.Serialize(new { Status = "Success", Message = "Gift sent successfully" });
            }
            catch (JsonException jsonEx)
            {
                Log.Error(jsonEx, "JSON deserialization failed for SendGiftMessage");
                return JsonSerializer.Serialize(new { Status = "Error", Message = "Invalid JSON format", Details = jsonEx.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in SendGiftHandler");
                return JsonSerializer.Serialize(new { Status = "Error", Message = "An unexpected error occurred", Details = ex.Message });
            }
        }
    }
}