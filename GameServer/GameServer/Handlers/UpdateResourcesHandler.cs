using System.Net.WebSockets;
using System.Security.AccessControl;
using System.Text.Json;
using System.Threading.Tasks;
using GameServer.Common.Messages;
using GameServer.Handlers;
using Serilog;

namespace GameServer
{
    public class UpdateResourcesHandler : IHandler
    {
        private readonly IPlayerManager _playerManager;
        private readonly ResourceTypeManager _resourceTypeManager;

        public UpdateResourcesHandler(IPlayerManager playerManager)
        {
            _playerManager = playerManager;
            _resourceTypeManager = new ResourceTypeManager();
        }

        public async Task<string> HandleMessageAsync(JsonElement payload, WebSocket socket)
        {
            try
            {
                // Deserialize and validate the payload
                var updateMessage = JsonSerializer.Deserialize<UpdateResourcesMessage>(payload.GetRawText());

                if (updateMessage == null || string.IsNullOrEmpty(updateMessage.PlayerId) || updateMessage.ResourceValue == 0 )
                {
                    Log.Error("Invalid UpdateResources message: Missing PlayerId");
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Invalid UpdateResources message" });
                }

                // Validate the resource type using ResourceTypeManager
                if (!_resourceTypeManager.IsValidResourceType(updateMessage.ResourceType))
                {
                    Log.Error("Invalid resource type {ResourceType} in SendGift message", updateMessage.ResourceType);
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Invalid resource type" });
                }

                var player = _playerManager.GetPlayer(updateMessage.PlayerId);
                if (player == null)
                {
                    Log.Warning("Player with PlayerId {PlayerId} not found", updateMessage.PlayerId);
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Player not found" });
                }

                if (!await player.CanUpdateResource(updateMessage.ResourceType, updateMessage.ResourceValue))
                {
                    Log.Error("Insufficient {ResourceType} in UpdateResources message", updateMessage.ResourceType);
                    return JsonSerializer.Serialize(new { Status = "Error", Message = $"Insufficient {updateMessage.ResourceType}" });
                }

                var updatedAmount = await player.UpdateResource(updateMessage.ResourceType, updateMessage.ResourceValue);
                Log.Information("{ResourceType} updated successfully for Player {PlayerId}", updateMessage.ResourceType, updateMessage.PlayerId);

                return JsonSerializer.Serialize(new { Status = "Success", ResourceType = updateMessage.ResourceType, Amount = updatedAmount });
            }
            catch (JsonException jsonEx)
            {
                Log.Error(jsonEx, "JSON deserialization failed for UpdateResourcesMessage");
                return JsonSerializer.Serialize(new { Status = "Error", Message = "Invalid JSON format", Details = jsonEx.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in UpdateResourcesHandler");
                return JsonSerializer.Serialize(new { Status = "Error", Message = "An unexpected error occurred", Details = ex.Message });
            }
        }
    }
}