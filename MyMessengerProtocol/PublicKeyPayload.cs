using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MyMessengerBackend.MyMessengerProtocol
{
    class PublicKeyPayload
    {
        
        private String public_key;

        [JsonPropertyName("Public_key")]
        public string Public_key { get => public_key; set => public_key = value; }
    }
}
