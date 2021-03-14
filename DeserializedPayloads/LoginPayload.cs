
using System.Text.Json.Serialization;

namespace MyMessengerBackend.DeserializedPayloads
{
    public class LoginPayload
    {
        [JsonPropertyName("Login")] 
        public string Login { get; set; }

        [JsonPropertyName("Password")]
        public string Password { get; set; }

    }
}
