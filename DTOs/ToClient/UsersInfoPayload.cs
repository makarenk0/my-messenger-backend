using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PayloadObjects.ToClient
{
    public class UsersInfoPayload : StatusResponsePayload
    {
        public UsersInfoPayload(string status, string details, List<UserInfo> users) : base(status, details)
        {
            Users = users;
        }

        [JsonPropertyName("Users")]
        public List<UserInfo> Users { get; set; }
    }
}
