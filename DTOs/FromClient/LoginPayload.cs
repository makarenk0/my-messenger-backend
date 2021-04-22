
using System.Text.Json.Serialization;

namespace DTOs.FromClient
{
    public class LoginPayload
    {
        [JsonPropertyName("Login")] 
        public string Login { get; set; }

        [JsonPropertyName("Password")]
        public string Password { get; set; }

    }
}
