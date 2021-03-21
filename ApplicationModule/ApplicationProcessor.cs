
using DeserializedPayloads.ToClient;
using DeserializedPayloads.FromClient;
using MongoDB.Bson;
using MyMessengerBackend.DatabaseModule;
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
                    }
                    return ('2', JsonSerializer.Serialize(result));
                case '3':
                    Message mes = new Message() { Sender = "member2", Body = "Forth message add to db", Id = MongoDB.Bson.ObjectId.GenerateNewId() };
                    //_userController.AddTestChat(new Chat() { Members = new List<string>() { "member1", "member2" }, Messages = new List<Message>() { mes } });
                    _userController.UpdateTestChat(mes);
                    return ('3', JsonSerializer.Serialize(new StatusResponsePayload("success", "Chat added")));
                case '4':  //Dont forget token

                    

                    SendChatMessagePayload send = JsonSerializer.Deserialize<SendChatMessagePayload>(payload);

                    var verifyResult4 = VerifySessionToken(send.SessionToken);
                    if (!verifyResult4.Item1)
                    {
                        return ('4', JsonSerializer.Serialize(verifyResult4.Item2));
                    }

                    Message newMessage = new Message() { Id = ObjectId.GenerateNewId(), Sender = _userController.User.Id.ToString(), Body = send.Body };
                    
                    var sended = _userController.SendMessageToChat(send.ChatId, newMessage);
                    TriggerUsers(send.ChatId, sended);
                    return ('4', JsonSerializer.Serialize(new ChatMessage(newMessage.Id.ToString(), newMessage.Sender, newMessage.Body)));
                //Subscribing to updates, notifying server about last received messages in chats
                case '7':
                    SubscriptionToUpdatePayload updatePayload = JsonSerializer.Deserialize<SubscriptionToUpdatePayload>(payload);

                    var verifyResult7 = VerifySessionToken(updatePayload.SessionToken);
                    if (!verifyResult7.Item1)
                    {
                        return ('7', JsonSerializer.Serialize(verifyResult7.Item2));
                    }

                    _userSubscribedForUpdates = true;
                    _subscriptionUpdatePacketNumber = updatePayload.SubscriptionPacketNumber;
                    FormLastMessagesTable(updatePayload.LastChatsMessages);
                    return ('7', JsonSerializer.Serialize(GetZeroUpdate()));
                //Debug
                case 'd':
                    return ('3', JsonSerializer.Serialize(new StatusResponsePayload("success", "Debug message")));
                default:
                    return ('9', JsonSerializer.Serialize(new StatusResponsePayload("error", "Unrecognized packet type")));
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

        private ZeroUpdatePayload GetZeroUpdate()
        {
            ZeroUpdatePayload res = new ZeroUpdatePayload("success", "Subscribed for update") { AllChats = new List<UpdateChatPayload>()};
            foreach (var m in _lastChatsMessages.Keys)
            {
                res.AllChats.Add(GetOneChatUpdated(m));
            }
            return res;
        }

        public (char, string) UpdatePacketForChat(string chatId)
        {

            // if someone init new chat with you
            if (!_lastChatsMessages.ContainsKey(chatId))
            {
                _lastChatsMessages.Add(chatId, null);
            }

            return (_subscriptionUpdatePacketNumber, JsonSerializer.Serialize(GetOneChatUpdated(chatId)));
        }


        private UpdateChatPayload GetOneChatUpdated(string chatId)
        {
            List<Message> newMessages;
            if (String.IsNullOrEmpty(_lastChatsMessages[chatId])) //get whole chat messages
            {
                newMessages = _userController.GetWholeChatMessages(chatId);
                _lastChatsMessages[chatId] = newMessages.Count > 0 ? newMessages[newMessages.Count - 1].Id.ToString() : null;  //update last messages table, if chat is empty leave null
            }
            else //get only new chat messages
            {
                newMessages = _userController.GetMessagesAfter(chatId, _lastChatsMessages[chatId]);
                _lastChatsMessages[chatId] = newMessages.Count > 0 ? newMessages[newMessages.Count - 1].Id.ToString() : _lastChatsMessages[chatId];  //update last messages table, if no new messages - leave as it was
            }
            return new UpdateChatPayload() { ChatId = chatId, NewMessages = newMessages.ConvertAll(x => new ChatMessage(x.Id.ToString(), x.Sender, x.Body)) };
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


        private (bool, StatusResponsePayload) VerifySessionToken(string passedSessionToken)
        {
            if(passedSessionToken != _sessionToken)
            {
                return (false, new StatusResponsePayload("error", "Invalid session token"));
            }
            return (true, null);
        }
    }
}
