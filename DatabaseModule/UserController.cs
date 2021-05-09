using MongoDB.Bson;
using MongoDB.Driver;
using DTOs.ToClient;
using DTOs.FromClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DatabaseModule.Entities;

namespace MyMessengerBackend.DatabaseModule
{
    public class UserController
    {
        
        private User _currentUser;

        private IMongoRepository<User> _usersRep;
        private IMongoRepository<Chat> _chatsRep;
        private IMongoRepository<AssistantDB> _assistantDB;


        public User User
        {
            get
            {
                return _currentUser;
            }
        }

        public UserController(IMongoRepository<User> usersRep, IMongoRepository<Chat> chatsRep, IMongoRepository<AssistantDB> assistantDB)
        {
            _usersRep = usersRep;
            _chatsRep = chatsRep;
            _assistantDB = assistantDB;
        }

        public StatusResponsePayload Register(RegistrationPayload payload)
        {

            User checkLogin = _usersRep.FindOneAsync(x => x.Login == payload.Login).Result;
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

            _usersRep.InsertOneAsync(newUser);


            return new StatusResponsePayload("success", "Account successfully created!");
        }

        private string InitAssistantChat(string userId)
        {

            Message firstAssistantMessage = new Message() { Id = ObjectId.GenerateNewId(), Body = "Hello, I am your virtual assistant!", Sender = "assistant" };
            Chat assistantChat = new Chat() { ChatName = "Virtual assistant", IsGroup = false, Members = new List<string>() { userId }, Messages = new List<Message>() { firstAssistantMessage } };

            _assistantDB.InsertOne(new AssistantDB() { UserId = userId, Reminders = new List<Reminder>() });
            return AddChat(assistantChat);
        }


        public LoginResponsePayload Login(LoginPayload payload)
        {
            User user = _usersRep.FindOneAsync(x => x.Login == payload.Login).Result;
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
            _chatsRep.InsertOne(chat);
            return chat.Id.ToString();
        }

        public List<string> SendMessageToChat(string chatId, Message mes)
        {
            List<string> usersOfChat = _chatsRep.FilterBy(x => x.Id == new ObjectId(chatId), x => x.Members).SingleOrDefault().ToList();
            _chatsRep.UpdateOneArray(chatId, "Messages", mes);
            return usersOfChat;
        }

        public (LastChatData, List<Message>) GetUpdatedData(string chatId, string lastMessageId, string lastAdminId, List<string> lastMembersIds)
        {
            var lastMessages = GetLastMessages(chatId, lastMessageId);
            LastChatData chatUpdatedData = _chatsRep.FilterBy(x => x.Id == new ObjectId(chatId), 
                x => new LastChatData(chatId, x.Members, x.Admin, lastMessageId)).FirstOrDefault();

            chatUpdatedData.Members = chatUpdatedData.Members.SequenceEqual(lastMembersIds) ? null : chatUpdatedData.Members;
            chatUpdatedData.Admin = chatUpdatedData.Admin == lastAdminId ? null : chatUpdatedData.Admin;
            chatUpdatedData.LastMessageId = lastMessages.Count == 0 ? lastMessageId : lastMessages.Last().Id.ToString();

            return (chatUpdatedData, lastMessages);
        }

        private List<Message> GetLastMessages(string chatId, string lastMessageId)
        {
            var res = _chatsRep.FilterBy(x => x.Id == new ObjectId(chatId), 
                i => new { x = i.Messages.Where(x => x.Id > new ObjectId(lastMessageId)) }).SingleOrDefault();
            if (res != null)
            {
                return res.x.ToList();
            }
            return new List<Message>();
        }




        public Chat GetWholeChat(string chatId)
        {
            //List<Message> allMessages = _chatsRepository.FilterBy(x => x.Id == new ObjectId(chatId), x => x.Messages).SingleOrDefault().ToList();

            Chat chat = _chatsRep.FindById(chatId);
            return chat;
        }

        public List<string> GetAllConnectedChats()
        {
            var res = _chatsRep.FilterBy(x => x.Members.Contains(_currentUser.Id.ToString()), x => x.Id);
            return res.Select(x => x.ToString()).ToList();
        }

        public List<UserInfo> GetUsers(string request, int limit)
        {
            List<User> searchResult = _usersRep.FilterByLimited(x => x.Login.Contains(request) || x.FirstName.Contains(request) || x.LastName.Contains(request), limit).ToList();
            List<UserInfo> casted = searchResult.ConvertAll(x => new UserInfo(x.Id.ToString(), x.Login, x.FirstName, x.LastName, x.BirthDate));
            
            return casted.Where(x => x.UserId != _currentUser.Id.ToString()).ToList();
        }

        public UserInfo GetUserById(string id)
        {
            var res = _usersRep.FindById(id);
            return new UserInfo(res.Id.ToString(), res.Login, res.FirstName, res.LastName, res.BirthDate);
        }

        public List<UserInfo> GetUsersByIds(List<string> ids)
        {
            List<ObjectId> idsObj = new List<ObjectId>();
            foreach(var id in ids)
            {
                if (ObjectId.TryParse(id, out _))
                {
                    idsObj.Add(new ObjectId(id));
                } 
            }
            var res = _usersRep.FilterBy(x => idsObj.Contains(x.Id));
            return res.Select(x => new UserInfo(x.Id.ToString(), x.Login, x.FirstName, x.LastName, x.BirthDate)).ToList();
        }


        public bool LeavePublicChat(string chatId)
        {
            var members = _chatsRep.FilterBy(x => x.Id == new ObjectId(chatId), x => x.Members).FirstOrDefault().Where(x => x != _currentUser.Id.ToString()).ToArray();
            _chatsRep.UpdateOne<IEnumerable<string>>(chatId, "Members", members);
            return true;
        }

        public (bool, string) ExcludeMemberFromPublicChat(string chatId, string memberId)
        {
            Chat adminIdAndMembers = _chatsRep.FilterBy(x => x.Id == new ObjectId(chatId), x => new Chat() {Admin = x.Admin, Members = x.Members }).FirstOrDefault();
            if(_currentUser.Id.ToString() == adminIdAndMembers.Admin)
            {
                var updatedMembers = adminIdAndMembers.Members.Where(x => x != memberId);
                _chatsRep.UpdateOne(chatId, "Members", updatedMembers);
                var member = GetUserById(memberId);
                return (true, $"{member.FirstName} { member.LastName}");
            }
            return (false, null);
        }

        public (bool, string) AddMemberToPublicChat(string chatId, string newMemberId)
        {
            Chat adminIdAndMembers = _chatsRep.FilterBy(x => x.Id == new ObjectId(chatId), x => new Chat() { Admin = x.Admin, Members = x.Members }).FirstOrDefault();
            if (_currentUser.Id.ToString() == adminIdAndMembers.Admin)
            {
                var updatedMembers = adminIdAndMembers.Members;
                updatedMembers.Add(newMemberId);
                _chatsRep.UpdateOne(chatId, "Members", updatedMembers);
                var newMember = GetUserById(newMemberId);
                return (true, $"{newMember.FirstName} { newMember.LastName}");
            }
            return (false, null);
        }

        public (bool, string) TranferAdminRightsToUser(string chatId, string memberId)
        {
            Chat adminIdAndMembers = _chatsRep.FilterBy(x => x.Id == new ObjectId(chatId), x => new Chat() { Admin = x.Admin, Members = x.Members }).FirstOrDefault();
            if (_currentUser.Id.ToString() == adminIdAndMembers.Admin && adminIdAndMembers.Members.Contains(memberId))
            {
                _chatsRep.UpdateOne(chatId, "Admin", memberId);
                var newAdmin = GetUserById(memberId);
                return (true, $"{newAdmin.FirstName} { newAdmin.LastName}");
            }
            return (false, null);
        }

        public bool MarkMessagesAsDeleted(string chatId, List<ObjectId> messagesIds)
        {
            var messagesToDelete = _chatsRep.FilterBy(x => x.Id == new ObjectId(chatId), x => new Chat() { Messages = x.Messages.Where(y => messagesIds.Contains(y.Id)).ToList() });
            
            foreach(var message in messagesToDelete.SingleOrDefault().Messages)
            {
                var filter = Builders<Chat>.Filter;
                var studentIdAndCourseIdFilter = filter.And(
                  filter.Eq(x => x.Id, new ObjectId(chatId)),
                  filter.ElemMatch(x => x.Messages, c => c.Id == message.Id));
                // find student with id and course id
                var student = _chatsRep.GetWholeCollection().Find(studentIdAndCourseIdFilter).SingleOrDefault();

                // update with positional operator
                var update = Builders<Chat>.Update;
                var courseLevelSetter = update.Push("Messages.$.DeletedForUsers", _currentUser.Id);
                _chatsRep.GetWholeCollection().UpdateOne(studentIdAndCourseIdFilter, courseLevelSetter);
            }
            return true;
            
        }


        public List<Reminder> GetActiveReminders()
        {
            var assistantData = _assistantDB.FindOne(x => x.UserId == _currentUser.Id.ToString());
            return assistantData.Reminders;
        }

        public void CreateReminder(Reminder rem)
        {
            var id = _assistantDB.FilterBy(x => x.UserId == _currentUser.Id.ToString(), y => y.Id);
            _assistantDB.UpdateOneArray(id.First().ToString(), "Reminders", rem);
        }

        public void RemoveReminder(Reminder rem)
        {
            var assistantData = _assistantDB.FindOne(x => x.UserId == _currentUser.Id.ToString());
            var currentReminders = assistantData.Reminders.Where(x => x.Id != rem.Id).ToList();
            _assistantDB.UpdateOne<IEnumerable<Reminder>>(assistantData.Id.ToString(), "Reminders", currentReminders);
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
