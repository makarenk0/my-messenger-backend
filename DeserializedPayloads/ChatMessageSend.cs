using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MyMessengerBackend.DeserializedPayloads
{
    public class ChatMessageSend
    {
        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }

        [JsonPropertyName("Body")]
        public string Body { get; set; }
    }
}
