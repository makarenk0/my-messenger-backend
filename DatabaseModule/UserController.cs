﻿using MongoDB.Bson;
using MongoDB.Driver;
using DTOs.ToClient;
using DTOs.FromClient;
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

        public StatusResponsePayload Register(RegistrationPayload payload)
        {

            User checkLogin = _usersRepository.FindOneAsync(x => x.Login == payload.Login).Result;
            if(checkLogin != null)
            {
                return new StatusResponsePayload("error", "Login is already taken");
            }
            byte[] passwordSalt = CreateSalt(32); //best practice - salt length same as the length of hash function
            byte[] hashSaltedPassword = GenerateSaltedHash(Encoding.ASCII.GetBytes(payload.Password), passwordSalt);

            string saltBase64Representation = Convert.ToBase64String(passwordSalt);
            string base64PasswordRepresentation = Convert.ToBase64String(hashSaltedPassword);

            payload.BirthDate = payload.BirthDate.Substring(0, 15);
            string dateFormat = "ddd MMM dd yyyy";
            DateTime date = DateTime.ParseExact(payload.BirthDate, dateFormat, CultureInfo.InvariantCulture);


            ObjectId userId = ObjectId.GenerateNewId();
            String assistantChatId = InitAssistantChat(userId.ToString());

            User newUser = new User() {Id= userId, Login = payload.Login, FirstName = payload.FirstName, 
                LastName = payload.LastName, BirthDate = date, AssistantChatId = assistantChatId,
                PasswordHash = base64PasswordRepresentation, PasswordSalt = saltBase64Representation
            };

            _usersRepository.InsertOneAsync(newUser);


            return new StatusResponsePayload("success", "Account successfully created!");
        }

        private string InitAssistantChat(string userId)
        {

            Message firstAssistantMessage = new Message() { Id = ObjectId.GenerateNewId(), Body = "Hello, I am your virtual assistant!", Sender = "assistant" };
            Chat assistantChat = new Chat() { ChatName = "Virtual assistant", IsGroup = false, Members = new List<string>() { userId }, Messages = new List<Message>() { firstAssistantMessage } };
            return AddChat(assistantChat);
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

            UserInfo currentUserInfo = new UserInfo(_currentUser.Id.ToString(), _currentUser.Login, _currentUser.FirstName, _currentUser.LastName,  _currentUser.AssistantChatId, _currentUser.BirthDate );

            return new LoginResponsePayload("success", "logged in successfully", Guid.NewGuid().ToString(), currentUserInfo);
        }

        public String AddChat(Chat chat)
        {
            chat.Id = ObjectId.GenerateNewId();
            _chatsRepository.InsertOne(chat);
            return chat.Id.ToString();
        }

        public List<string> SendMessageToChat(string chatId, Message mes)
        {
            List<string> usersOfChat = _chatsRepository.FilterBy(x => x.Id == new ObjectId(chatId), x => x.Members).SingleOrDefault().ToList();
            _chatsRepository.UpdateOneArray(chatId, "Messages", mes);
            return usersOfChat;
        }

        public List<Message> GetMessagesAfter(string chatId, string lastMessageId)
        {
            var pipeline = _chatsRepository.Collection.Aggregate().Match(x => x.Id == new ObjectId(chatId)).Project(i => new {x = i.Messages.Where(x => x.Id > new ObjectId(lastMessageId))});
            //var pipeline = _chatsRepository.Collection.Aggregate().Match(x => x.Id == new ObjectId(chatId)).Project(i => new Chat{ChatName=i.ChatName, Members=i.Members, Messages = i.Messages.Where(x => x.Id > new ObjectId(lastMessageId)).ToList()}); //TO DO: debug
            
        
            var res = pipeline.SingleOrDefault();
            if(res != null)
            {
                return res.x.ToList();
            }
            return new List<Message>();
        }

        public Chat GetWholeChat(string chatId)
        {
            //List<Message> allMessages = _chatsRepository.FilterBy(x => x.Id == new ObjectId(chatId), x => x.Messages).SingleOrDefault().ToList();

            Chat chat = _chatsRepository.FindById(chatId);
            return chat;
        }

        public List<string> GetAllConnectedChats()
        {
            var res = _chatsRepository.FilterBy(x => x.Members.Contains(_currentUser.Id.ToString()), x => x.Id);
            return res.Select(x => x.ToString()).ToList();
        }

        public List<UserInfo> GetUsers(string request, int limit)
        {
            List<User> searchResult = _usersRepository.FilterByLimited(x => x.Login.Contains(request) || x.FirstName.Contains(request) || x.LastName.Contains(request), limit).ToList();
            List<UserInfo> casted = searchResult.ConvertAll(x => new UserInfo(x.Id.ToString(), x.Login, x.FirstName, x.LastName, x.BirthDate));
            
            return casted.Where(x => x.UserId != _currentUser.Id.ToString()).ToList();
        }

        public UserInfo GetUserById(string id)
        {
            var res = _usersRepository.FindById(id);
            return new UserInfo(res.Id.ToString(), res.Login, res.FirstName, res.LastName, res.BirthDate);
        }

        public List<UserInfo> GetUsersByIds(List<string> ids)
        {
            var idsobj = ids.ConvertAll(x => new ObjectId(x));
            var res = _usersRepository.FilterBy(x => idsobj.Contains(x.Id));
            return res.Select(x => new UserInfo(x.Id.ToString(), x.Login, x.FirstName, x.LastName, x.BirthDate)).ToList();
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
