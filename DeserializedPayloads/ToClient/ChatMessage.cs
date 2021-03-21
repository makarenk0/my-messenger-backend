using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DeserializedPayloads.ToClient
{
    public class ChatMessage
    {
        public ChatMessage(string id, string sender, string body)
        {
            MessageId = id;
            Sender = sender;
            Body = body;
        }


        [JsonPropertyName("_id")]
        public string MessageId { get; set; }

        [JsonPropertyName("Sender")]
        public string Sender { get; set; }

        [JsonPropertyName("Body")]
        public string Body { get; set; }
    }
}
