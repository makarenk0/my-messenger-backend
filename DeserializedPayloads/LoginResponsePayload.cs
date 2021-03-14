using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MyMessengerBackend.DeserializedPayloads
{
    public class LoginResponsePayload : StatusResponse
    {

        public LoginResponsePayload(string status, string details, string sessionToken) : base(status, details)
        {   
            SessionToken = sessionToken;
        }

        public LoginResponsePayload(string status, string details) : base(status, details)
        {
            SessionToken = "";
        }

        [JsonPropertyName("SessionToken")]
        public string SessionToken { get; set; }
    }
}
