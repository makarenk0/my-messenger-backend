using MongoDB.Bson.Serialization.Attributes;
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

        [BsonDateTimeOptions(DateOnly = true)]
        public DateTime BirthDate { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
    }
}
