
using PayloadObjects.ToClient;
using PayloadObjects.FromClient;
using MongoDB.Bson;
using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Text.Json;
using System.Linq;
using VirtualAssistant;

namespace MyMessengerBackend.ApplicationModule
{
    public class ApplicationProcessor
    {

        private const int MAXIMUM_USERS_SEARCH_NUMBER = 10;
        private UserController _userController;
        private MongoDbSettings _dbSettings;

        private string _sessionToken;

        //private bool _userSubscribedForUpdates;
        private char _subscriptionUpdatePacketNumber;


        public delegate void UserLoggedIn(string userId);
        private UserLoggedIn _userLoggedAction;


        public delegate void UpdateAction(string chatId);
        public static ConcurrentDictionary<string, UpdateAction> _activeUsersTable;

        private VirtualAssistantEntryPoint _virtualAssistant;

        private Dictionary<string, string> _lastChatsMessages;

        

        public ApplicationProcessor(UserLoggedIn action)
        {
            //_userSubscribedForUpdates = false;
            _dbSettings = new MongoDbSettings();
            _dbSettings.ConnectionString = ConfigurationManager.AppSettings["db_connection"];
            _dbSettings.DatabaseName = ConfigurationManager.AppSettings["db_name"];
            _userController = new UserController(new MongoRepository<User>(_dbSettings), new MongoRepository<Chat>(_dbSettings));
            _userLoggedAction = action;
            _lastChatsMessages = new Dictionary<string, string>();

            _virtualAssistant = new VirtualAssistantEntryPoint(ConfigurationManager.AppSettings["assistant_file"]);
        }

        public (char, string) Process(char packetType, string payload)
        {
            switch (packetType)
            {
                //Registartion
                case '1':
                    RegistrationPayload registration = JsonSerializer.Deserialize<RegistrationPayload>(payload);
                    if(String.IsNullOrWhiteSpace(registration.Login) || String.IsNullOrWhiteSpace(registration.FirstName) ||
                        String.IsNullOrWhiteSpace(registration.LastName) || String.IsNullOrWhiteSpace(registration.Password) ||
                        String.IsNullOrWhiteSpace(registration.BirthDate))
                    {
                        return ('1', JsonSerializer.Serialize(new StatusResponsePayload("error", "some of the fields are empty")));
                    }
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
                //Find user (by id or by firstname, lastname, login)
                case '3':
                    FindUserPayload find = JsonSerializer.Deserialize<FindUserPayload>(payload);

                    var verifyResult3 = VerifySessionToken(find.SessionToken);
                    if (!verifyResult3.Item1)
                    {
                        return ('4', JsonSerializer.Serialize(verifyResult3.Item2));
                    }
                    if(find.UserIds != null)
                    {
                        UsersInfoPayload usersInfoByIds = new UsersInfoPayload("success", "found users", _userController.GetUsersByIds(find.UserIds) );
                        return ('3', JsonSerializer.Serialize(usersInfoByIds));
                    }
                    UsersInfoPayload usersInfo = new UsersInfoPayload("success", "found users", _userController.GetUsers(find.FindUsersRequest, MAXIMUM_USERS_SEARCH_NUMBER));
                    return ('3', JsonSerializer.Serialize(usersInfo));
                //Send message
                case '4': 
                    SendChatMessagePayload send = JsonSerializer.Deserialize<SendChatMessagePayload>(payload);

                    var verifyResult4 = VerifySessionToken(send.SessionToken);
                    if (!verifyResult4.Item1)
                    {
                        return ('4', JsonSerializer.Serialize(verifyResult4.Item2));
                    }

                    Message newMessage = new Message() { Id = ObjectId.GenerateNewId(), Sender = _userController.User.Id.ToString(), Body = send.Body };
                    
                    var sended = _userController.SendMessageToChat(send.ChatId, newMessage);
                    TriggerUsers(send.ChatId, sended);
                    return ('4', JsonSerializer.Serialize(new StatusResponsePayload("success", "Message was sent")));
                //private chats
                case '6':
                    InitChatPayload init = JsonSerializer.Deserialize<InitChatPayload>(payload);

                    var verifyResult5 = VerifySessionToken(init.SessionToken);
                    if (!verifyResult5.Item1)
                    {
                        return ('6', JsonSerializer.Serialize(verifyResult5.Item2));
                    }

                    Message newInitMessage = new Message() { Id = ObjectId.GenerateNewId(), Sender = _userController.User.Id.ToString(), Body = init.Body };
                    Chat toAdd = new Chat() { Members = init.UserIds, Messages = new List<Message>() { newInitMessage } , IsGroup = false};
                    string newChatId = _userController.AddChat(toAdd);


                 
                    TriggerUsers(newChatId, toAdd.Members);
                    return ('6', JsonSerializer.Serialize(new UpdateChatPayload() { ChatId = newChatId, IsNew = true, Members = toAdd.Members, 
                        NewMessages = new List<ChatMessage>() { new ChatMessage(toAdd.Messages[0].Id.ToString(), toAdd.Messages[0].Sender, toAdd.Messages[0].Body) } }));
                //Subscribing to updates, notifying server about last received messages in chats
                case '7':
                    SubscriptionToUpdatePayload updatePayload = JsonSerializer.Deserialize<SubscriptionToUpdatePayload>(payload);

                    var verifyResult7 = VerifySessionToken(updatePayload.SessionToken);
                    if (!verifyResult7.Item1)
                    {
                        return ('7', JsonSerializer.Serialize(verifyResult7.Item2));
                    }

                    //_userSubscribedForUpdates = true;
                    _subscriptionUpdatePacketNumber = updatePayload.SubscriptionPacketNumber;
                    FormLastMessagesTable(updatePayload.LastChatsMessages);
                    return ('7', JsonSerializer.Serialize(GetZeroUpdate()));
                //Group chats
                case '8':
                    InitChatPayload initGroupChat = JsonSerializer.Deserialize<InitChatPayload>(payload);

                    var verifyResult8 = VerifySessionToken(initGroupChat.SessionToken);
                    if (!verifyResult8.Item1)
                    {
                        return ('8', JsonSerializer.Serialize(verifyResult8.Item2));
                    }

                    Message newGroupInitMessage = new Message() { Id = ObjectId.GenerateNewId(), Sender = "System", 
                        Body = String.Concat(_userController.User.FirstName, " ", _userController.User.LastName, " created a group") };
                    Chat toAddGroup = new Chat() { Members = initGroupChat.UserIds, ChatName = initGroupChat.ChatName, Messages = new List<Message>() { newGroupInitMessage } , IsGroup = true};
                    string newGroupChatId = _userController.AddChat(toAddGroup);



                    TriggerUsers(newGroupChatId, toAddGroup.Members);
                    return ('8', JsonSerializer.Serialize(new StatusResponsePayload("success", "Group created")));
                // Virtual assistant
                case 'a':
                    SendChatMessagePayload messageToAssistant = JsonSerializer.Deserialize<SendChatMessagePayload>(payload);

                    var verifyResultA = VerifySessionToken(messageToAssistant.SessionToken);
                    if (!verifyResultA.Item1)
                    {
                        return ('a', JsonSerializer.Serialize(verifyResultA.Item2));
                    }
                    string assistantResponse = _virtualAssistant.Process(messageToAssistant.Body);
                    _userController.SendMessageToChat(messageToAssistant.ChatId, new Message() { Id = ObjectId.GenerateNewId(), Sender = _userController.User.Id.ToString(), Body = messageToAssistant.Body });
                    var sendedToUsers = _userController.SendMessageToChat(messageToAssistant.ChatId, new Message() { Id = ObjectId.GenerateNewId(), Sender = "assistant", Body = assistantResponse });
                    TriggerUsers(messageToAssistant.ChatId, sendedToUsers);
                    return ('a', JsonSerializer.Serialize(JsonSerializer.Serialize(new StatusResponsePayload("success", "Message to assistant was sent"))));

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
                var update = GetOneChatUpdated(m);
                if(update.NewMessages.Count > 0)
                {
                    res.AllChats.Add(update);
                }
                
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
                var wholeChat = _userController.GetWholeChat(chatId);
                newMessages = wholeChat.Messages;
                _lastChatsMessages[chatId] = newMessages.Count > 0 ? newMessages[newMessages.Count - 1].Id.ToString() : null;  //update last messages table, if chat is empty leave null

                string chatName = "";
                if (wholeChat.ChatName == null)
                {
                    var oppositeUser = _userController.GetUserById(wholeChat.Members.Where(x => x != _userController.User.Id.ToString()).First());
                    chatName = String.Concat(oppositeUser.FirstName, " ", oppositeUser.LastName);
                }
                else
                {
                    chatName = wholeChat.ChatName;
                }
                
                return new UpdateChatPayload() { ChatId = chatId, IsNew=true, IsGroup= wholeChat.IsGroup, ChatName = chatName, Members = wholeChat.Members, NewMessages = newMessages.ConvertAll(x => new ChatMessage(x.Id.ToString(), x.Sender, x.Body)) };
            }
            else //get only new chat messages
            {
                var newMessagesChat = _userController.GetMessagesAfter(chatId, _lastChatsMessages[chatId]);
                newMessages = newMessagesChat;
                _lastChatsMessages[chatId] = newMessages.Count > 0 ? newMessages[newMessages.Count - 1].Id.ToString() : _lastChatsMessages[chatId];  //update last messages table, if no new messages - leave as it was
                return new UpdateChatPayload() { ChatId = chatId, IsNew = false, ChatName = null, Members = null, NewMessages = newMessages.ConvertAll(x => new ChatMessage(x.Id.ToString(), x.Sender, x.Body)) };  //TO DO: debug
            }
            
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
