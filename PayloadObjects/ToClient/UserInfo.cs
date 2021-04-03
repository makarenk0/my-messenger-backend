using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PayloadObjects.ToClient
{
    public class UserInfo
    {

        [JsonPropertyName("UserID")]
        public string UserID { get; set; }

        [JsonPropertyName("Login")]
        public string Login { get; set; }

        [JsonPropertyName("FirstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string LastName { get; set; }

        [JsonPropertyName("BirthDate")]
        public DateTime BirthDate { get; set; }
    }
}
