using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MyMessengerBackend.DeserializedPayloads
{
    public class LastChatMessage
    {
        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }

        [JsonPropertyName("LastMessageId")]
        public string LastMessageId { get; set; }
    }
}
