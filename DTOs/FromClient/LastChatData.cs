using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DTOs.FromClient
{
    public class LastChatData
    {
        public LastChatData(string chatId, List<string> members, string admin, string lastMessageId)
        {
            ChatId = chatId;
            Members = members;
            Admin = admin;
            LastMessageId = lastMessageId;
        }


        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }

        [JsonPropertyName("Members")]
        public List<string> Members { get; set; }

        [JsonPropertyName("Admin")]
        public string Admin { get; set; }

        [JsonPropertyName("LastMessageId")]
        public string LastMessageId { get; set; }
    }
}
