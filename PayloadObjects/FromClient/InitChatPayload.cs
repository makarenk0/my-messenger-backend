using System.Text.Json.Serialization;

namespace PayloadObjects.FromClient
{
    public class InitChatPayload : SessionTokenPayload
    {
        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        [JsonPropertyName("Body")]
        public string Body { get; set; }
    }
}
