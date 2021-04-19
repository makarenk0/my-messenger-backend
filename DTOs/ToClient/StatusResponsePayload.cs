using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DTOs.ToClient
{
    public class StatusResponsePayload
    {
        public StatusResponsePayload(string status)
        {
            Status = status;
            Details = "";
        }

        public StatusResponsePayload(string status, string details)
        {
            Status = status;
            Details = details;
        }

        [JsonPropertyName("Status")]
        public string Status { get; set; }

        [JsonPropertyName("Details")]
        public string Details { get; set; }
    }
}
