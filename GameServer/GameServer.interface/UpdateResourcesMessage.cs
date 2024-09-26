namespace GameServer.Common.Messages
{
    public class UpdateResourcesMessage
    {
        public required string PlayerId { get; set; }
        public required string ResourceType { get; set; }
        public int ResourceValue { get; set; }
    }
}