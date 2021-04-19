using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DTOs.ToClient
{
    public class UserInfo
    {
        public UserInfo(string userId, string login, string firstName, string lastName, DateTime birthDate)
        {
            UserId = userId;
            Login = login;
            FirstName = firstName;
            LastName = lastName;
            BirthDate = birthDate;
        }

        public UserInfo(string userId, string login, string firstName, string lastName, string assistantChatId, DateTime birthDate)
        {
            UserId = userId;
            Login = login;
            FirstName = firstName;
            LastName = lastName;
            AssistantChatId = assistantChatId;
            BirthDate = birthDate;
        }

        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        [JsonPropertyName("Login")]
        public string Login { get; set; }

        [JsonPropertyName("FirstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string LastName { get; set; }

        [JsonPropertyName("BirthDate")]
        public DateTime BirthDate { get; set; }

        [JsonPropertyName("AssistantChatId")]
        public string AssistantChatId { get; set; }
    }
}
