﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DTOs.ToClient
{
    public class UpdateChatPayload
    {
        public UpdateChatPayload(string chatId, bool isNew, List<string> members, ChatMessage oneMessage)
        {
            ChatId = chatId;
            IsNew = isNew;
            IsGroup = false;
            Members = members;
            Admin = null;
            NewMessages = new List<ChatMessage>() { oneMessage };
        }

        public UpdateChatPayload(string chatId, bool isNew, bool isGroup, string chatName, List<string> members, string admin, List<ChatMessage> newMessages)
        {
            ChatId = chatId;
            IsNew = isNew;
            IsGroup = isGroup;
            ChatName = chatName;
            Members = members;
            Admin = admin;
            NewMessages = newMessages;
        }

        public UpdateChatPayload(string chatId, bool isNew, List<string> members, string admin, List<ChatMessage> newMessages)
        {
            ChatId = chatId;
            IsNew = isNew;
            //IsGroup = false;
            Members = members;
            Admin = admin;
            NewMessages = newMessages;
        }


        [JsonPropertyName("ChatId")]
        public string ChatId { get; set; }

        [JsonPropertyName("IsNew")]
        public bool IsNew { get; set; }

        [JsonPropertyName("NewMessages")]
        public List<ChatMessage> NewMessages { get; set; }

        [JsonPropertyName("ChatName")]
        public string ChatName { get; set; }

        [JsonPropertyName("IsGroup")]
        public bool IsGroup { get; set; }

        [JsonPropertyName("Admin")]
        public string Admin { get; set; }

        [JsonPropertyName("Members")]
        public List<String> Members { get; set; }
    }
}
