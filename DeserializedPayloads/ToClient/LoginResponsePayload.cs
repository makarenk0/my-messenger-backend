using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DeserializedPayloads.ToClient
{
    public class LoginResponsePayload : StatusResponsePayload
    {

        public LoginResponsePayload(string status, string details, string sessionToken, string id) : base(status, details)
        {   
            SessionToken = sessionToken;
            Id = id;
        }

        public LoginResponsePayload(string status, string details) : base(status, details)
        {
            SessionToken = "";
        }

        [JsonPropertyName("SessionToken")]
        public string SessionToken { get; set; }

        [JsonPropertyName("Id")]
        public string Id { get; set; }
    }
}
