using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessengerBackend.DatabaseModule
{
    public class UserController
    {
        private readonly IMongoRepository<User> _usersRepository;

        public UserController(IMongoRepository<User> usersRepository)
        {
            _usersRepository = usersRepository;
        }

        public string GetTest()
        {
            User u = _usersRepository.FindById("604bef01d01cc97da1e0b78b");
            return u.Login;
        }
    }
}
