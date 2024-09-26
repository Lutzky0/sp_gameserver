using System.Collections.Concurrent;
using System.Net.WebSockets;
using GameServer.Managers;

namespace GameServer
{
    public interface IPlayerManager
    {
        PlayerState GetPlayer(string playerId);
        bool IsPlayerConnected(string deviceId);
        bool AddPlayer(string deviceId, string playerId, WebSocket socket);
    }

    public class PlayerManager : IPlayerManager
    {
        private readonly ConcurrentDictionary<string, PlayerState> _players = new();

        // Checks if a player is connected by their DeviceId
        public bool IsPlayerConnected(string deviceId)
        {
            return _players.Values.Any(p => p.DeviceId == deviceId);
        }

        public bool AddPlayer(string UDID, string playerId, WebSocket socket)
        {
            var playerState = new PlayerState(UDID, playerId, socket);
            return _players.TryAdd(playerId, playerState);
        }

        public PlayerState GetPlayer(string playerId)
        {
            _players.TryGetValue(playerId, out var player);
            return player;
        }
    }

    
}