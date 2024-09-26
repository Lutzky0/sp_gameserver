namespace GameServer.Common.Messages
{
    public class SendGiftMessage
    {
        public required string SenderPlayerId { get; set; }
        public required string FriendPlayerId { get; set; }
        public required string ResourceType { get; set; }
        public int ResourceValue { get; set; }
    }
}