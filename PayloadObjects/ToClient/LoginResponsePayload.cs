using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PayloadObjects.ToClient
{
    public class LoginResponsePayload : StatusResponsePayload
    {

        public LoginResponsePayload(string status, string details, string sessionToken, UserInfo info) : base(status, details)
        {   
            SessionToken = sessionToken;
            userInfo = info;
        }

        public LoginResponsePayload(string status, string details) : base(status, details)
        {
            SessionToken = "";
        }

        [JsonPropertyName("SessionToken")]
        public string SessionToken { get; set; }

        [JsonPropertyName("UserInfo")]
        public UserInfo userInfo { get; set; }
    }
}
