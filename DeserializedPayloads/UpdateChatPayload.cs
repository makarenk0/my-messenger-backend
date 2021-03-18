﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MyMessengerBackend.DeserializedPayloads
{
    public class UpdateChatPayload
    {
        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }

        [JsonPropertyName("NewMessages")]
        public List<ChatMessage> NewMessages { get; set; }
    }
}
