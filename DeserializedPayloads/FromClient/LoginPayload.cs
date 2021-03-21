
using System.Text.Json.Serialization;

namespace DeserializedPayloads.FromClient
{
    public class LoginPayload
    {
        [JsonPropertyName("Login")] 
        public string Login { get; set; }

        [JsonPropertyName("Password")]
        public string Password { get; set; }

    }
}
