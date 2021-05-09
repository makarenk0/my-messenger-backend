using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessengerBackend.DatabaseModule
{
    public class Message : Document
    {
        public string Sender { get; set; }
        public List<ObjectId> DeletedForUsers { get; set; }
        public string Body { get; set; }
    }
}
