
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
using ServiceModule;
using DatabaseModule;

namespace MyMessengerBackend.ServiceModule
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

        private Dictionary<string, LastChatData> _lastChatsData;

        private delegate string InputProcess(object request);
        private Dictionary<char, InputProcess> handlers;
       

        private PublicChatEventsHandler _publicChatEventsHandler;


        public ServiceProcessor(UserLoggedIn action)
        {
            _userController = new UserController(RepositoryService.UsersRepository, RepositoryService.ChatsRepository, RepositoryService.AssistantDBRepository);
            _userLoggedAction = action;
            _lastChatsData = new Dictionary<string, LastChatData>();

            _publicChatEventsHandler = new PublicChatEventsHandler(_userController);

            RegisterAppHandlers();
            
        }

        private void RegisterAppHandlers()
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
            handlers.TryAdd('d', DeleteMessageForUser);
            handlers.TryAdd('p', PublicChatEventHandle);
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
                VirtualAssistantEntryPoint.UpdateActionFromAssistant updateActionFromAssistant = TriggerFromAssistant;
                _virtualAssistant = new VirtualAssistantEntryPoint(ConfigurationManager.AppSettings["assistant_file"], _userController, updateActionFromAssistant);
            }
            return JsonSerializer.Serialize(result);
        }

        private void TriggerFromAssistant()
        {
            TriggerUsers(_userController.User.AssistantChatId, new List<string>() { _userController.User.Id.ToString() });
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

            Message newMessage = new Message() { 
                Id = ObjectId.GenerateNewId(), 
                Sender = _userController.User.Id.ToString(), 
                Body = send.Body,
                DeletedForUsers = new List<ObjectId>()};

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

            Message newInitMessage = new Message() { 
                Id = ObjectId.GenerateNewId(), 
                Sender = _userController.User.Id.ToString(), 
                Body = init.Body,
                DeletedForUsers = new List<ObjectId>()
            };
            Chat toAdd = new Chat() { 
                Members = init.UserIds, 
                Messages = new List<Message>() { newInitMessage }, 
                IsGroup = false 
            };
            string newChatId = _userController.AddChat(toAdd);



            TriggerUsers(newChatId, toAdd.Members);
            var message = new ChatMessage(toAdd.Messages[0].Id.ToString(), toAdd.Messages[0].Sender, toAdd.Messages[0].Body, false);
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
                Body = String.Concat(_userController.User.FirstName, " ", _userController.User.LastName, " created a group"),
                DeletedForUsers = new List<ObjectId>()
            };
            Chat toAddGroup = new Chat() { 
                Members = initGroupChat.UserIds, 
                ChatName = initGroupChat.ChatName, 
                Messages = new List<Message>() { newGroupInitMessage }, 
                Admin = _userController.User.Id.ToString(),
                IsGroup = true };
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
            _userController.SendMessageToChat(messageToAssistant.ChatId, new Message() { 
                Id = ObjectId.GenerateNewId(), 
                Sender = _userController.User.Id.ToString(), 
                Body = messageToAssistant.Body,
                DeletedForUsers = new List<ObjectId>()
            });
            var sendedToUsers = _userController.SendMessageToChat(_userController.User.AssistantChatId, 
                new Message() { 
                    Id = ObjectId.GenerateNewId(), 
                    Sender = "assistant", 
                    Body = assistantResponse,
                    DeletedForUsers = new List<ObjectId>()
                });
            TriggerUsers(messageToAssistant.ChatId, sendedToUsers);

            return JsonSerializer.Serialize(JsonSerializer.Serialize(new StatusResponsePayload("success", "Message to assistant was sent")));
        }


        private string PublicChatEventHandle(object request)
        {
            PublicChatEventPayload pChatEvent = (PublicChatEventPayload)request;

            var result = _publicChatEventsHandler.ProcessPublicChatEvents(pChatEvent);
            if (result.Item1)
            {
                TriggerUsers(pChatEvent.ChatId, result.Item2);
                return JsonSerializer.Serialize(new StatusResponsePayload("success", "Public event was executed"));
            }
            return JsonSerializer.Serialize(new StatusResponsePayload("error", "Can't proccess public event"));
        }

        private string DeleteMessageForUser(object request)
        {
            DeleteMessagesPayload deleteMessagesPayload = (DeleteMessagesPayload)request;
            _userController.MarkMessagesAsDeleted(deleteMessagesPayload.ChatId, deleteMessagesPayload.MessagesIds.ConvertAll(x => new ObjectId(x)));
            return JsonSerializer.Serialize(new StatusResponsePayload("success", "Messages were deleted"));
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
            foreach (var m in _lastChatsData.Keys)
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
            if (!_lastChatsData.ContainsKey(chatId))
            {
                _lastChatsData.Add(chatId, null);
            }

            return (_subscriptionUpdatePacketNumber, JsonSerializer.Serialize(GetOneChatUpdated(chatId)));
        }


        private UpdateChatPayload GetOneChatUpdated(string chatId)
        {
            List<Message> newMessages;
            if (_lastChatsData[chatId] == null) //get whole chat messages
            {
                var wholeChat = _userController.GetWholeChat(chatId);
                newMessages = wholeChat.Messages;
                _lastChatsData[chatId] = new LastChatData(wholeChat.Id.ToString(), wholeChat.Members, wholeChat.Admin, newMessages[newMessages.Count - 1].Id.ToString());//newMessages.Count > 0 ? newMessages[newMessages.Count - 1].Id.ToString() : null;  //update last messages table, if chat is empty leave null

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
                var newMessagesPayload = newMessages.ConvertAll(x => new ChatMessage(x.Id.ToString(), x.Sender, x.Body, x.DeletedForUsers.Contains(_userController.User.Id)));
                return new UpdateChatPayload(chatId, true, wholeChat.IsGroup, chatName, wholeChat.Members, wholeChat.Admin, newMessagesPayload);
            }
            else //get only new data (user already have local data)
            {
                var (updateChatData, newMessagesData) = _userController.GetUpdatedData(chatId, _lastChatsData[chatId].LastMessageId, _lastChatsData[chatId].Admin, _lastChatsData[chatId].Members);
                newMessages = newMessagesData;



                _lastChatsData[chatId].LastMessageId = updateChatData.LastMessageId;
                _lastChatsData[chatId].Members = updateChatData.Members == null ? _lastChatsData[chatId].Members : updateChatData.Members;
                _lastChatsData[chatId].Admin = updateChatData.Admin == null ? _lastChatsData[chatId].Admin : updateChatData.Admin;

                if (!_lastChatsData[chatId].Members.Contains(_userController.User.Id.ToString()))  // this means we are kicked and this is the last packet
                {
                    _lastChatsData.Remove(chatId);
                }

                var newMessagesPayload = newMessages.ConvertAll(x => new ChatMessage(x.Id.ToString(), x.Sender, x.Body, x.DeletedForUsers.Contains(_userController.User.Id)));
                return new UpdateChatPayload(chatId, false, updateChatData.Members, updateChatData.Admin, newMessagesPayload);  //retrieve members from database in case of new members added
            }
            
        }


        private void FormLastMessagesTable(List<LastChatData> lastChatsMessages)  // may ne null
        {
            // meassages and chats stored on device user
            foreach(var m in lastChatsMessages)
            {
                _lastChatsData.TryAdd(m.ChatId, m);
            }
            
            //if someone inits chat with user while user is offline
            var allChats = _userController.GetAllConnectedChats();
            foreach(var ch in allChats)
            {
                _lastChatsData.TryAdd(ch, null);
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
