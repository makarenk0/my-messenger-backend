using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DeserializedPayloads.ToClient
{
    public class NewChatPayload
    {
        [JsonPropertyName("ChatName")]
        public string ChatName { get; set; }


        [JsonPropertyName("Members")]
        public List<String> Members { get; set; }
    }
}
