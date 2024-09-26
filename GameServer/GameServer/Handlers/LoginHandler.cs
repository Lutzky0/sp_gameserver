using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using GameServer.Common.Messages;
using GameServer.Handlers;
using Serilog;

namespace GameServer
{
    public class LoginHandler : IHandler
    {
        private readonly IPlayerManager _playerManager;

        public LoginHandler(IPlayerManager playerManager)
        {
            _playerManager = playerManager;
        }

        public async Task<string> HandleMessageAsync(JsonElement payload, WebSocket socket)
        {
            try
            {
                // Deserialize and validate the payload
                var loginMessage = JsonSerializer.Deserialize<LoginMessage>(payload.GetRawText());

                if (loginMessage == null || string.IsNullOrEmpty(loginMessage.UDID))
                {
                    Log.Error("Invalid Login message: Missing DeviceId");
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Invalid Login message" });
                }

                string playerId = Guid.NewGuid().ToString();

                if (_playerManager.IsPlayerConnected(loginMessage.UDID))
                {
                    Log.Warning("Player with DeviceId {DeviceId} is already connected", loginMessage.UDID);
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Player is already connected" });
                }

                bool added = _playerManager.AddPlayer(loginMessage.UDID, playerId, socket);
                if (added)
                {
                    Log.Information("Player with DeviceId {DeviceId} successfully logged in with PlayerId {PlayerId}", loginMessage.UDID, playerId);
                    return JsonSerializer.Serialize(new { Status = "Success", PlayerId = playerId });
                }
                else
                {
                    Log.Error("Failed to add player with DeviceId {DeviceId}", loginMessage.UDID);
                    return JsonSerializer.Serialize(new { Status = "Error", Message = "Failed to add player" });
                }
            }
            catch (JsonException jsonEx)
            {
                Log.Error(jsonEx, "JSON deserialization failed for LoginMessage");
                return JsonSerializer.Serialize(new { Status = "Error", Message = "Invalid JSON format", Details = jsonEx.Message });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in LoginHandler");
                return JsonSerializer.Serialize(new { Status = "Error", Message = "An unexpected error occurred", Details = ex.Message });
            }
        }
    }
}