using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace MyMessengerBackend.DeserializedPayloads
{
    public class StatusResponse
    {
        public StatusResponse(string status)
        {
            Status = status;
            Details = "";
        }

        public StatusResponse(string status, string details)
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
