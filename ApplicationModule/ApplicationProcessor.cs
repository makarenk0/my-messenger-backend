
using MongoDB.Bson;
using MyMessengerBackend.DatabaseModule;
using MyMessengerBackend.DeserializedPayloads;
using System;
using System.Collections.Concurrent;
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

        private bool _userSubscribedForUpdates;
        private char _subscriptionUpdatePacketNumber;


        public delegate void UserLoggedIn(string userId);
        private UserLoggedIn _userLoggedAction;


        public delegate void UpdateAction(string chatId);
        public static ConcurrentDictionary<string, UpdateAction> _activeUsersTable;


        private Dictionary<string, string> _lastChatsMessages;

        public ApplicationProcessor(UserLoggedIn action)
        {
            _userSubscribedForUpdates = false;
            _dbSettings = new MongoDbSettings();
            _dbSettings.ConnectionString = ConfigurationManager.AppSettings["db_connection"];
            _dbSettings.DatabaseName = ConfigurationManager.AppSettings["db_name"];
            _userController = new UserController(new MongoRepository<User>(_dbSettings), new MongoRepository<Chat>(_dbSettings));
            _userLoggedAction = action;
            _lastChatsMessages = new Dictionary<string, string>();
        }

        public (char, string) Process(char packetType, string payload)
        {
            switch (packetType)
            {
                //Registartion
                case '1':
                    RegistrationPayload registration = JsonSerializer.Deserialize<RegistrationPayload>(payload);
                    return ('1', JsonSerializer.Serialize(_userController.Register(registration)));
                //Sign in
                case '2':
                    LoginPayload login = JsonSerializer.Deserialize<LoginPayload>(payload);
                    var result = _userController.Login(login);
                    if(result.Status == "success")
                    {
                        _sessionToken = result.SessionToken;
                        _userLoggedAction(_userController.User.Id.ToString());
                        //_activeUsersTable[_userController.User.Id.ToString()]("superchatid");  //Test
                    }
                    return ('2', JsonSerializer.Serialize(result));
                case '3':
                    Message mes = new Message() { Sender = "member2", Body = "Forth message add to db", Id = MongoDB.Bson.ObjectId.GenerateNewId() };
                    //_userController.AddTestChat(new Chat() { Members = new List<string>() { "member1", "member2" }, Messages = new List<Message>() { mes } });
                    _userController.UpdateTestChat(mes);
                    return ('3', JsonSerializer.Serialize(new StatusResponse("success", "Chat added")));
                case '4':  //Dont forget token
                    ChatMessageSend send = JsonSerializer.Deserialize<ChatMessageSend>(payload);
                    Message newMessage = new Message() { Id = ObjectId.GenerateNewId(), Sender = _userController.User.Id.ToString(), Body = send.Body };
                    
                    var sended = _userController.SendMessageToChat(send.ChatId, newMessage);
                    TriggerUsers(send.ChatId, sended);
                    return ('4', JsonSerializer.Serialize(new ChatMessage(newMessage.Id.ToString(), newMessage.Sender, newMessage.Body)));
                //Subscribing to updates, notifying server about last received messages in chats
                case '7':
                    SubscriptionToUpdatePayload updatePayload = JsonSerializer.Deserialize<SubscriptionToUpdatePayload>(payload);

                    var verifyResult = VerifySessionToken(updatePayload.SessionToken);
                    if (!verifyResult.Item1)
                    {
                        return ('7', JsonSerializer.Serialize(verifyResult.Item2));
                    }

                    _userSubscribedForUpdates = true;
                    _subscriptionUpdatePacketNumber = updatePayload.SubscriptionPacketNumber;
                    FormLastMessagesTable(updatePayload.LastChatsMessages);

                    return ('7', JsonSerializer.Serialize(new StatusResponse("success", $"Subscribed for update on packet number {updatePayload.SubscriptionPacketNumber}")));
                default:
                    return ('9', JsonSerializer.Serialize(new StatusResponse("error", "Unrecognized packet type")));
            }
        }

        private void TriggerUsers(string chatId, List<string> users)
        {
            foreach(var user in users)
            {
                if (_activeUsersTable.ContainsKey(user))
                {
                    _activeUsersTable[user]?.Invoke(chatId);
                }
            }
            
        }

        public (char, string) UpdatePacketForChat(string chatId)
        {

            // init new chat
            if (!_lastChatsMessages.ContainsKey(chatId))
            {
                _lastChatsMessages.Add(chatId, null);
            }

            
            List<Message> newMessages;
            if (String.IsNullOrEmpty(_lastChatsMessages[chatId])) //get whole chat messages
            {
                throw new NotImplementedException("whole chat load not implemented");
                //newMessages = 
            }
            else //get only new chat messages
            {
                newMessages = _userController.GetMessagesAfter(chatId, _lastChatsMessages[chatId]);
                _lastChatsMessages[chatId] = newMessages[newMessages.Count-1].Id.ToString();  //udpate last messages table
            }
            UpdateChatPayload updatePayload = new UpdateChatPayload() { ChatId = chatId, NewMessages = newMessages.ConvertAll(x => new ChatMessage(x.Id.ToString(), x.Sender, x.Body)) };
            return (_subscriptionUpdatePacketNumber, JsonSerializer.Serialize(updatePayload));
        }


        private void FormLastMessagesTable(List<LastChatMessage> lastChatsMessages)
        {
            // meassages and chats stored on device user
            foreach(var m in lastChatsMessages)
            {
                _lastChatsMessages.TryAdd(m.ChatId, m.LastMessageId);
            }
            
            //if someone inits chat with user while user is offline
            var allChats = _userController.GetAllConnectedChats();
            foreach(var ch in allChats)
            {
                _lastChatsMessages.TryAdd(ch, null);
            }

        }


        private (bool, StatusResponse) VerifySessionToken(string passedSessionToken)
        {
            if(passedSessionToken != _sessionToken)
            {
                return (false, new StatusResponse("error", "Invalid session token"));
            }
            return (true, null);
        }
    }
}
