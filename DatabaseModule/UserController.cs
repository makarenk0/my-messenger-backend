using MongoDB.Bson;
using MongoDB.Driver;
using MyMessengerBackend.DeserializedPayloads;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MyMessengerBackend.DatabaseModule
{
    public class UserController
    {
        private readonly IMongoRepository<User> _usersRepository;
        private readonly IMongoRepository<Chat> _chatsRepository;


        private User _currentUser;

        public User User
        {
            get
            {
                return _currentUser;
            }
        }

        public UserController(IMongoRepository<User> usersRepository, IMongoRepository<Chat> chatsRepository)
        {
            _usersRepository = usersRepository;
            _chatsRepository = chatsRepository;
        }

        public StatusResponse Register(RegistrationPayload payload)
        {
            User checkLogin = _usersRepository.FindOneAsync(x => x.Login == payload.Login).Result;
            if(checkLogin != null)
            {
                return new StatusResponse("error", "Login is already taken");
            }
            byte[] passwordSalt = CreateSalt(32); //best practice - salt length same as the length of hash function
            byte[] hashSaltedPassword = GenerateSaltedHash(Encoding.ASCII.GetBytes(payload.Password), passwordSalt);

            string saltBase64Representation = Convert.ToBase64String(passwordSalt);
            string base64PasswordRepresentation = Convert.ToBase64String(hashSaltedPassword);

            payload.BirthDate = payload.BirthDate.Substring(0, 15);
            string dateFormat = "ddd MMM dd yyyy";
            DateTime date = DateTime.ParseExact(payload.BirthDate, dateFormat, CultureInfo.InvariantCulture);

            User newUser = new User() { Login = payload.Login, FirstName = payload.FirstName, 
                LastName = payload.LastName, BirthDate = date, 
                PasswordHash = base64PasswordRepresentation, PasswordSalt = saltBase64Representation
            };
            _usersRepository.InsertOneAsync(newUser);
    
            return new StatusResponse("success", "Account successfully created!");
        }


        public LoginResponsePayload Login(LoginPayload payload)
        {
            User user = _usersRepository.FindOneAsync(x => x.Login == payload.Login).Result;
            if(user == null)
            {
                return new LoginResponsePayload("error", "Login does not exist!");
            }
            if(!VerifyUserPassword(user, payload.Password))
            {
                return new LoginResponsePayload("error", "Password is invalid!");
            }
            _currentUser = user;
            return new LoginResponsePayload("success", "logged in successfully", Guid.NewGuid().ToString());
        }

        public void AddTestChat(Chat chat)
        {
            _chatsRepository.InsertOneAsync(chat);
        }

        public List<string> SendMessageToChat(string chatId, Message mes)
        {
            List<string> usersOfChat = _chatsRepository.FilterBy(x => x.Id == new ObjectId(chatId), x => x.Members).SingleOrDefault().ToList()
                .Where(x => x != _currentUser.Id.ToString()).ToList();
            _chatsRepository.UpdateOneArray(chatId, "Messages", mes);
            return usersOfChat;
        }

        public void UpdateTestChat(Message mes)
        {
            //_chatsRepository.UpdateOneArrayAsync("6052553af34ec222c2c36a57", "Messages", mes);
            //Chat ch = _chatsRepository.FindOneAsync(x => x.Id == new MongoDB.Bson.ObjectId("6052553af34ec222c2c36a57")).Result;
            //  _chatsRepository.GetElementsFromArrayAfter("6052553af34ec222c2c36a57", "60526d82fd5b47331d0f0401");
            //var filter = Builders<Chat>.Filter.And(Builders<Chat>.Filter.Eq("_id", new ObjectId("6052553af34ec222c2c36a57"))); //Builders<TDocument>.Filter.Eq("Messages._id", afterArrayId)
            //var projection = Builders<Chat>.Projection.Expression(x => x.Messages.Where(y => y.Id.Timestamp > new ObjectId("60526d82fd5b47331d0f0401").Timestamp));
            //_chatsRepository.AsQueryable().Where(x => x.Id == new ObjectId("6052553af34ec222c2c36a57")).Where(y => y.Messages.W)
            //ch.

            List<Message> messages = GetMessagesAfter("6052553af34ec222c2c36a57", "60526d82fd5b47331d0f0401");


            //var items3 = _chatsRepository.AsQueryable().SingleOrDefault(x => x.Id == new ObjectId("6052553af34ec222c2c36a57")).Messages.Where(x => x.Id.Timestamp > new ObjectId("60526d82fd5b47331d0f0401").Timestamp);  // BAD - gets all document from db
        }


        public List<Message> GetMessagesAfter(string chatId, string lastMessageId)
        {
            var pipeline = _chatsRepository.Collection.Aggregate().Match(x => x.Id == new ObjectId(chatId)).Project(i => new {x = i.Messages.Where(x => x.Id > new ObjectId(lastMessageId))});

            var res = pipeline.SingleOrDefault();
            return res.x.ToList();
        }


        public List<string> GetAllConnectedChats()
        {
            var res = _chatsRepository.FilterBy(x => x.Members.Contains(_currentUser.Id.ToString()), x => x.Id);
            return res.Select(x => x.ToString()).ToList();
        }

        private bool VerifyUserPassword(User user, string inputPassword)
        {
            String base64Hashed = Convert.ToBase64String(GenerateSaltedHash(Encoding.ASCII.GetBytes(inputPassword), Convert.FromBase64String(user.PasswordSalt)));
            return base64Hashed == user.PasswordHash;
        }


        private byte[] GenerateSaltedHash(byte[] plainText, byte[] salt)
        {
            HashAlgorithm algorithm = new SHA256Managed();

            byte[] plainTextWithSaltBytes =
              new byte[plainText.Length + salt.Length];

            for (int i = 0; i < plainText.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainText[i];
            }
            for (int i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[plainText.Length + i] = salt[i];
            }

            return algorithm.ComputeHash(plainTextWithSaltBytes);
        }

        private byte[] CreateSalt(int size)
        {     
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);
            return buff;
        }
    }
}
