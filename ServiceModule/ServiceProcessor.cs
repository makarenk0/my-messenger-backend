
using DTOs.ToClient;
using DTOs.FromClient;
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
using ApplicationModule;

namespace MyMessengerBackend.ApplicationModule
{
    public class ServiceProcessor
    {

        private const int MAXIMUM_USERS_SEARCH_NUMBER = 10;
        private UserController _userController;
        private MongoDbSettings _dbSettings;

        private string _sessionToken;

        private char _subscriptionUpdatePacketNumber;


        public delegate void UserLoggedIn(string userId);
        private UserLoggedIn _userLoggedAction;


        public delegate void UpdateAction(string chatId);
        public static ConcurrentDictionary<string, UpdateAction> _activeUsersTable;

        private VirtualAssistantEntryPoint _virtualAssistant;

        private Dictionary<string, string> _lastChatsMessages;

        private delegate string InputProcess(object request);
        private Dictionary<char, InputProcess> handlers;



        public ServiceProcessor(UserLoggedIn action)
        {
            _dbSettings = new MongoDbSettings();
            _dbSettings.ConnectionString = ConfigurationManager.AppSettings["db_connection"];
            _dbSettings.DatabaseName = ConfigurationManager.AppSettings["db_name"];
            _userController = new UserController(new MongoRepository<User>(_dbSettings), new MongoRepository<Chat>(_dbSettings));
            _userLoggedAction = action;
            _lastChatsMessages = new Dictionary<string, string>();

            _virtualAssistant = new VirtualAssistantEntryPoint(ConfigurationManager.AppSettings["assistant_file"]);

            RegisterHandlers();
            
        }

        private void RegisterHandlers()
        {
            handlers = new Dictionary<char, InputProcess>();
            handlers.TryAdd('1', Registration);
            handlers.TryAdd('2', LogIn);
            handlers.TryAdd('3', FindUsers);
            handlers.TryAdd('4', SendMessage);
            handlers.TryAdd('6', InitPrivateChat);
            handlers.TryAdd('7', SubscribeOnUpdates);
            handlers.TryAdd('8', InitPublicChat);
            handlers.TryAdd('a', AssistantRequest);
        }

        public (char, string) Process(char packetType, string payload)
        {
            object deserializedRequest;
            try
            {
                deserializedRequest = JsonSerializer.Deserialize(payload, ObjectTypeMapper.table[packetType]);
            }
            catch(JsonException e)
            {
                return (packetType, JsonSerializer.Serialize(new StatusResponsePayload("error", "Invalid json string")));
            }
            catch (ArgumentNullException e)
            {
                return (packetType, JsonSerializer.Serialize(new StatusResponsePayload("error", "Nothing in packet")));
            }

            return (packetType, handlers[packetType](deserializedRequest));
            
        }



        private string Registration(object request)
        {
            RegistrationPayload registration = (RegistrationPayload)request;
            if (String.IsNullOrWhiteSpace(registration.Login) || String.IsNullOrWhiteSpace(registration.FirstName) ||
                String.IsNullOrWhiteSpace(registration.LastName) || String.IsNullOrWhiteSpace(registration.Password) ||
                String.IsNullOrWhiteSpace(registration.BirthDate))
            {
                return JsonSerializer.Serialize(new StatusResponsePayload("error", "Some of the fields are empty"));
            }
            return JsonSerializer.Serialize(_userController.Register(registration));
        }


        private string LogIn(object request)
        {
            LoginPayload login = (LoginPayload)request;
            var result = _userController.Login(login);
            if (result.Status == "success")
            {
                _sessionToken = result.SessionToken;
                _userLoggedAction(_userController.User.Id.ToString());
            }
            return JsonSerializer.Serialize(result);
        }

        private string FindUsers(object request)
        {
            FindUserPayload find = (FindUserPayload)request;

            var verifyResult = VerifySessionToken(find.SessionToken);
            if (!verifyResult.Item1)
            {
                return JsonSerializer.Serialize(verifyResult.Item2);
            }
            if (find.UserIds != null)
            {
                UsersInfoPayload usersInfoByIds = new UsersInfoPayload("success", "found users", _userController.GetUsersByIds(find.UserIds));
                return JsonSerializer.Serialize(usersInfoByIds);
            }
            UsersInfoPayload usersInfo = new UsersInfoPayload("success", "found users", _userController.GetUsers(find.FindUsersRequest, MAXIMUM_USERS_SEARCH_NUMBER));
            return JsonSerializer.Serialize(usersInfo);
        }

        private string SendMessage(object request)
        {
            SendChatMessagePayload send = (SendChatMessagePayload)request;

            var verifyResult = VerifySessionToken(send.SessionToken);
            if (!verifyResult.Item1)
            {
                return JsonSerializer.Serialize(verifyResult.Item2);
            }

            Message newMessage = new Message() { Id = ObjectId.GenerateNewId(), Sender = _userController.User.Id.ToString(), Body = send.Body };

            var sended = _userController.SendMessageToChat(send.ChatId, newMessage);
            TriggerUsers(send.ChatId, sended);
            return JsonSerializer.Serialize(new StatusResponsePayload("success", "Message was sent"));
        }

        private string InitPrivateChat(object request)
        {
            InitChatPayload init = (InitChatPayload)request;

            var verifyResult = VerifySessionToken(init.SessionToken);
            if (!verifyResult.Item1)
            {
                return JsonSerializer.Serialize(verifyResult.Item2);
            }

            Message newInitMessage = new Message() { Id = ObjectId.GenerateNewId(), Sender = _userController.User.Id.ToString(), Body = init.Body };
            Chat toAdd = new Chat() { Members = init.UserIds, Messages = new List<Message>() { newInitMessage }, IsGroup = false };
            string newChatId = _userController.AddChat(toAdd);



            TriggerUsers(newChatId, toAdd.Members);
            var message = new ChatMessage(toAdd.Messages[0].Id.ToString(), toAdd.Messages[0].Sender, toAdd.Messages[0].Body);
            return JsonSerializer.Serialize(new UpdateChatPayload(newChatId, true, toAdd.Members, message));
        }

        private string SubscribeOnUpdates(object request)
        {
            SubscriptionToUpdatePayload updatePayload = (SubscriptionToUpdatePayload)request;

            var verifyResult = VerifySessionToken(updatePayload.SessionToken);
            if (!verifyResult.Item1)
            {
                return JsonSerializer.Serialize(verifyResult.Item2);
            }

            _subscriptionUpdatePacketNumber = updatePayload.SubscriptionPacketNumber;
            FormLastMessagesTable(updatePayload.LastChatsMessages);
            return JsonSerializer.Serialize(GetZeroUpdate());
        }




        private string InitPublicChat(object request)
        {

            InitChatPayload initGroupChat = (InitChatPayload)request;

            var verifyResult = VerifySessionToken(initGroupChat.SessionToken);
            if (!verifyResult.Item1)
            {
                return JsonSerializer.Serialize(verifyResult.Item2);
            }

            Message newGroupInitMessage = new Message()
            {
                Id = ObjectId.GenerateNewId(),
                Sender = "System",
                Body = String.Concat(_userController.User.FirstName, " ", _userController.User.LastName, " created a group")
            };
            Chat toAddGroup = new Chat() { Members = initGroupChat.UserIds, ChatName = initGroupChat.ChatName, Messages = new List<Message>() { newGroupInitMessage }, IsGroup = true };
            string newGroupChatId = _userController.AddChat(toAddGroup);

            TriggerUsers(newGroupChatId, toAddGroup.Members);
            return JsonSerializer.Serialize(new StatusResponsePayload("success", "Group created"));
        }


        private string AssistantRequest(object request)
        {
            SendChatMessagePayload messageToAssistant = (SendChatMessagePayload)request;

            var verifyResult = VerifySessionToken(messageToAssistant.SessionToken);
            if (!verifyResult.Item1)
            {
                return JsonSerializer.Serialize(verifyResult.Item2);
            }
            string assistantResponse = _virtualAssistant.Process(messageToAssistant.Body);
            _userController.SendMessageToChat(messageToAssistant.ChatId, new Message() { Id = ObjectId.GenerateNewId(), Sender = _userController.User.Id.ToString(), Body = messageToAssistant.Body });
            var sendedToUsers = _userController.SendMessageToChat(_userController.User.AssistantChatId, new Message() { Id = ObjectId.GenerateNewId(), Sender = "assistant", Body = assistantResponse });
            TriggerUsers(messageToAssistant.ChatId, sendedToUsers);

            return JsonSerializer.Serialize(JsonSerializer.Serialize(new StatusResponsePayload("success", "Message to assistant was sent")));
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
                var newMessagesPayload = newMessages.ConvertAll(x => new ChatMessage(x.Id.ToString(), x.Sender, x.Body));
                return new UpdateChatPayload(chatId, true, wholeChat.IsGroup, chatName, wholeChat.Members, newMessagesPayload);
            }
            else //get only new chat messages
            {
                var newMessagesChat = _userController.GetMessagesAfter(chatId, _lastChatsMessages[chatId]);
                newMessages = newMessagesChat;
                _lastChatsMessages[chatId] = newMessages.Count > 0 ? newMessages[newMessages.Count - 1].Id.ToString() : _lastChatsMessages[chatId];  //update last messages table, if no new messages - leave as it was
                var newMessagesPayload = newMessages.ConvertAll(x => new ChatMessage(x.Id.ToString(), x.Sender, x.Body));
                return new UpdateChatPayload(chatId, false, null, newMessagesPayload);  //retrieve members from database in case of new members added
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
