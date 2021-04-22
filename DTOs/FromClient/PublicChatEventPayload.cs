using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DTOs.FromClient
{
    public class PublicChatEventPayload
    {
        [JsonPropertyName("EventType")]
        public int EventType { get; set; }

        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }

        [JsonPropertyName("EventData")]
        public Object EventData { get; set; }
    }


    public class LeaveEvent {
        
    }

    public class AddEvent
    {
        public string UserId { get; set; }
    }

    public class ExcludeEvent
    {
        public string UserId { get; set; }
    }

    public class AdminRoleTransferEvent
    {
        public string UserId { get; set; }
    }

}
