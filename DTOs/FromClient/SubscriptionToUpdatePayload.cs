using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTOs.FromClient
{
    public class SubscriptionToUpdatePayload : SessionTokenPayload
    {
        [JsonPropertyName("SubscriptionPacketNumber")]
        public char SubscriptionPacketNumber { get; set; }


        [JsonPropertyName("LastChatsData")]
        public List<LastChatData> LastChatsMessages { get; set; }
    }
}
