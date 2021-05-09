using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DTOs.FromClient
{
    public class DeleteMessagesPayload
    {
        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }


        [JsonPropertyName("MessagesIds")]
        public List<string> MessagesIds { get; set; }
    }
}
