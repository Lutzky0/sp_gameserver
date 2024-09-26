using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    public class PlayerState
    {
        public string DeviceId { get; }
        public string PlayerId { get; }
        public WebSocket Socket { get; set; } // Track the player's WebSocket

        private Dictionary<string, int> Resources = new Dictionary<string, int>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Semaphore for thread safety

        public PlayerState(string deviceId, string playerId, WebSocket socket)
        {
            DeviceId = deviceId;
            PlayerId = playerId;
            Socket = socket;
            Resources = new Dictionary<string, int>();
        }

        // Method to get resource value
        public async Task<int> GetResource(string resourceType)
        {
            await _semaphore.WaitAsync();
            try
            {
                // Return the resource value if it exists, otherwise default to 0
                if (Resources.TryGetValue(resourceType.ToLower(), out var value))
                {
                    return value;
                }
                else
                {
                    return 0;  // Default to 0 if the resource type is not found
                }
            }
            finally
            {
                _semaphore.Release();
            }

        }

        // Method to update resource value
        public async Task<int> UpdateResource(string resourceType, int value)
        {
            await _semaphore.WaitAsync();
            try
            {
                // If the resource doesn't exist, add it with the default value
                if (!Resources.ContainsKey(resourceType.ToLower()))
                {
                    Resources[resourceType.ToLower()] = 0;
                }

                // Update the resource
                Resources[resourceType.ToLower()] += value;
                return Resources[resourceType.ToLower()];
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // Check if the update would result in negative resources
        public async Task<bool> CanUpdateResource(string resourceType, int value)
        {
            int currentResourceValue = await GetResource(resourceType);
            return currentResourceValue + value >= 0;
        }
    }
}
