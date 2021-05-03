using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule.Entities
{
    [BsonCollection("assistant_DB")]
    public class AssistantDB : Document
    {
        public string UserId { get; set; }

        public List<Reminder> Reminders { get; set; }
    }
}
