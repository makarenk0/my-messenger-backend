
using MyMessengerBackend.DatabaseModule;
using MyMessengerBackend.DeserializedPayloads;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.Json;

namespace MyMessengerBackend.ApplicationModule
{
    public class ApplicationProcessor
    {
        private UserController _userController;
      
        private MongoDbSettings _dbSettings;

        private string _sessionToken;

        public ApplicationProcessor()
        {
            _dbSettings = new MongoDbSettings();
            _dbSettings.ConnectionString = ConfigurationManager.AppSettings["db_connection"];
            _dbSettings.DatabaseName = ConfigurationManager.AppSettings["db_name"];
            _userController = new UserController(new MongoRepository<User>(_dbSettings));
        }

        public (char, string) Process(char packetType, string payload)
        {
            //if(packetType != '1' && packetType != '2' && )

            switch (packetType)
            {
                case '1':
                    RegistrationPayload registration = JsonSerializer.Deserialize<RegistrationPayload>(payload);
                    return ('1', JsonSerializer.Serialize(_userController.Register(registration)));
                case '2':
                    LoginPayload login = JsonSerializer.Deserialize<LoginPayload>(payload);
                    var result = _userController.Login(login);
                    if(result.Status == "success")
                    {
                        _sessionToken = result.SessionToken;
                    }
                    return ('2', JsonSerializer.Serialize(result));
                default:
                    break;
            }
            return (' ', null);
        }
    }
}
