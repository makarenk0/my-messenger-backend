using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PayloadObjects.FromClient
{
    public class SessionTokenPayload
    {
        [JsonPropertyName("SessionToken")]
        public string SessionToken { get; set; }
    }
}
