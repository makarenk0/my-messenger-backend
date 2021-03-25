using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PayloadObjects.FromClient
{
    public class SendChatMessagePayload : SessionTokenPayload
    {
        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }

        [JsonPropertyName("Body")]
        public string Body { get; set; }
    }
}
