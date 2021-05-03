using MongoDB.Bson.Serialization.Attributes;
using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule.Entities
{
    public class Reminder : Document
    {
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime ReminderDateTime { get; set; }
        public string ReminderContent { get; set; }
    }
}
