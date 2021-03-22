using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessengerBackend.DatabaseModule
{
    [BsonCollection("chats")]
    public class Chat : Document
    {
        public string ChatName { get; set; }

        public List<String> Members { get; set; }

        public List<Message> Messages { get; set; }
    }
}
