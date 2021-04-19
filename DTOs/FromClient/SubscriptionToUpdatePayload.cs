﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTOs.FromClient
{
    public class SubscriptionToUpdatePayload : SessionTokenPayload
    {
        [JsonPropertyName("SubscriptionPacketNumber")]
        public char SubscriptionPacketNumber { get; set; }


        [JsonPropertyName("LastChatsMessages")]
        public List<LastChatMessage> LastChatsMessages { get; set; }
    }
}
