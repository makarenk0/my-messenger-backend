using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PayloadObjects.ToClient
{
    public class UpdateChatPayload
    {
        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }

        [JsonPropertyName("IsNew")]
        public bool IsNew { get; set; }

        [JsonPropertyName("NewMessages")]
        public List<ChatMessage> NewMessages { get; set; }

        [JsonPropertyName("ChatName")]
        public string ChatName { get; set; }

        [JsonPropertyName("IsGroup")]
        public bool IsGroup { get; set; }

        [JsonPropertyName("Members")]
        public List<String> Members { get; set; }
    }
}
