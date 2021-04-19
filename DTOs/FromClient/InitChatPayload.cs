using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PayloadObjects.FromClient
{
    public class InitChatPayload : SessionTokenPayload
    {
        [JsonPropertyName("ChatName")]
        public string ChatName { get; set; }

        [JsonPropertyName("UserIds")]
        public List<string> UserIds { get; set; }

        [JsonPropertyName("Body")]
        public string Body { get; set; }
    }
}
