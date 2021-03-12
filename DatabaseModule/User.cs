using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessengerBackend.DatabaseModule
{
    [BsonCollection("users")]
    public class User : Document
    {
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string BirthDate { get; set; }
    }
}
