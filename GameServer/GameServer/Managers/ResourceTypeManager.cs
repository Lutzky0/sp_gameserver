using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace GameServer
{
    public class ResourceTypeManager
    {
        private readonly List<string> _validResourceTypes;

        // Constructor to load valid resource types from appsettings.json
        public ResourceTypeManager()
        {

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("valid_resources_types.json", optional: false, reloadOnChange: true)
                .Build();

            // Get the valid resource types from the configuration
            _validResourceTypes = configuration.GetSection("ValidResourceTypes").Get<List<string>>() ?? new List<string>();
        }

        // Method to check if the resource type is valid
        public bool IsValidResourceType(string resourceType)
        {
            return _validResourceTypes.Contains(resourceType.ToLower());
        }
    }
}