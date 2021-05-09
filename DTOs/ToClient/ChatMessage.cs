using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DTOs.ToClient
{
    public class ChatMessage
    {
        public ChatMessage(string id, string sender, string body, bool isDeleted)
        {
            MessageId = id;
            Sender = sender;
            Body = body;
            IsDeleted = isDeleted;
        }


        [JsonPropertyName("_id")]
        public string MessageId { get; set; }

        [JsonPropertyName("Sender")]
        public string Sender { get; set; }

        [JsonPropertyName("IsDeleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("Body")]
        public string Body { get; set; }
    }
}
