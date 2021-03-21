using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DeserializedPayloads.ToClient
{
    public class ZeroUpdatePayload : StatusResponsePayload
    {
        public ZeroUpdatePayload(string status, string details) : base(status, details)
        {
        }

        [JsonPropertyName("AllChats")]
        public List<UpdateChatPayload> AllChats { get; set; }
    }
}
