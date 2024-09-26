using System.Net.WebSockets;
using System.Text.Json;

namespace GameServer.Handlers
{
    public interface IHandler
    {
        Task<string> HandleMessageAsync(JsonElement payload, WebSocket socket);
    }
}