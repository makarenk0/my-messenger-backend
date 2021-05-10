using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule.Entities
{
    public class ToDo : Document
    {
        public string ToDoContent { get; set; }
    }
}
