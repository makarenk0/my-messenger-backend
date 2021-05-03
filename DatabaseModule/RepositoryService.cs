using DatabaseModule.Entities;
using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule
{
    public static class RepositoryService
    {
        public static readonly IMongoRepository<User> UsersRepository;
        public static readonly IMongoRepository<Chat> ChatsRepository;
        public static readonly IMongoRepository<AssistantDB> AssistantDBRepository;

        static RepositoryService()
        {
            MongoDbSettings _dbSettings = new MongoDbSettings();
            _dbSettings.ConnectionString = ConfigurationManager.AppSettings["db_connection"];
            _dbSettings.DatabaseName = ConfigurationManager.AppSettings["db_name"];

            UsersRepository = new MongoRepository<User>(_dbSettings);
            ChatsRepository = new MongoRepository<Chat>(_dbSettings);
            AssistantDBRepository = new MongoRepository<AssistantDB>(_dbSettings);
        }
    }
}
