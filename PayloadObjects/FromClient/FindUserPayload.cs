using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PayloadObjects.FromClient
{
    public class FindUserPayload : SessionTokenPayload
    {
        [JsonPropertyName("FindUsersRequest")]
        public string FindUsersRequest { get; set; }
    }
}
