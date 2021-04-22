using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DTOs.FromClient
{
    public class FindUserPayload : SessionTokenPayload
    {
        [JsonPropertyName("UserIds")]
        public List<string> UserIds { get; set; }


        [JsonPropertyName("FindUsersRequest")]
        public string FindUsersRequest { get; set; }
    }
}
