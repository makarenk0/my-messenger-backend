using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DTOs.FromClient
{
    public class RegistrationPayload
    {
        [JsonPropertyName("Login")]
        public string Login { get; set; }

        [JsonPropertyName("FirstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string LastName { get; set; }

        [JsonPropertyName("BirthDate")]
        public string BirthDate { get; set; }

        [JsonPropertyName("Password")]
        public string Password { get; set; }
    }
}
