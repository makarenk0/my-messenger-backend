using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessengerBackend.DatabaseModule
{
    [BsonCollection("chats")]
    public class Chat : Document
    {
        public ICollection<String> Members { get; set; }

        public ICollection<Message> Messages { get; set; }
    }
}
