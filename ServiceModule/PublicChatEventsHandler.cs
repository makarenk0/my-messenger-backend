using DTOs.FromClient;
using MongoDB.Bson;
using MyMessengerBackend.DatabaseModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServiceModule
{
    public class PublicChatEventsHandler
    {
        private UserController _userController;


        private delegate (bool, List<string>) HandleEvent(PublicChatEventPayload request);
        private Dictionary<int, HandleEvent> publicChatHandlers;

        public PublicChatEventsHandler(UserController controller)
        {
            _userController = controller;
            RegisterPublicChatHandlers();
        }

        private void RegisterPublicChatHandlers()
        {
            publicChatHandlers = new Dictionary<int, HandleEvent>();
            publicChatHandlers.TryAdd(1, LeavePublicChat);
            publicChatHandlers.TryAdd(2, ExcludeMemberFromPublicChat);
            publicChatHandlers.TryAdd(3, AddMemberToPublicChat);
            publicChatHandlers.TryAdd(4, TranferAdminRightsToUser);
        }

        public (bool, List<string>) ProcessPublicChatEvents(PublicChatEventPayload eventPayload)
        {
            return publicChatHandlers[eventPayload.EventType](eventPayload);
        }


        private (bool, List<string>) LeavePublicChat(PublicChatEventPayload e)
        {
            LeaveEvent leaveEvent = JsonSerializer.Deserialize<LeaveEvent>(e.EventData.ToString());
            var res = _userController.LeavePublicChat(e.ChatId);
            if (res)
            {
                var sendedToUsers = _userController.SendMessageToChat(e.ChatId,
                    new Message() { Id = ObjectId.GenerateNewId(), Sender = "System", 
                        Body = $"Member {_userController.User.FirstName + " " + _userController.User.LastName} left the chat" });
                return (true, sendedToUsers);
            }

            return (false, null);
        }


        private (bool, List<string>) ExcludeMemberFromPublicChat(PublicChatEventPayload e)
        {

            ExcludeEvent excludeEvent = JsonSerializer.Deserialize<ExcludeEvent>(e.EventData.ToString());
            var res = _userController.ExcludeMemberFromPublicChat(e.ChatId, excludeEvent.UserId);
            if (res.Item1)
            {
                var sendedToUsers = _userController.SendMessageToChat(
                    e.ChatId,
                    new Message()
                    {
                        Id = ObjectId.GenerateNewId(),
                        Sender = "System",
                        Body = $"Member {res.Item2} was excluded\n" +
                        $"by {_userController.User.FirstName} {_userController.User.LastName}"
                    });
                return (true, sendedToUsers);
            }

            return (false, null);
        }

        private (bool, List<string>) AddMemberToPublicChat(PublicChatEventPayload e)
        {
            ExcludeEvent excludeEvent = JsonSerializer.Deserialize<ExcludeEvent>(e.EventData.ToString());
            var res = _userController.AddMemberToPublicChat(e.ChatId, excludeEvent.UserId);
            if (res.Item1)
            {
                var sendedToUsers = _userController.SendMessageToChat(
                    e.ChatId,
                    new Message()
                    {
                        Id = ObjectId.GenerateNewId(),
                        Sender = "System",
                        Body = $"Member {res.Item2} was added\n" +
                        $"by {_userController.User.FirstName} {_userController.User.LastName}"
                    });
                return (true, sendedToUsers);
            }

            return (false, null);
        }

        private (bool, List<string>) TranferAdminRightsToUser(PublicChatEventPayload e)
        {
            AdminRoleTransferEvent excludeEvent = JsonSerializer.Deserialize<AdminRoleTransferEvent>(e.EventData.ToString());
            var res = _userController.TranferAdminRightsToUser(e.ChatId, excludeEvent.UserId);
            if (res.Item1)
            {
                var sendedToUsers = _userController.SendMessageToChat(
                    e.ChatId,
                    new Message()
                    {
                        Id = ObjectId.GenerateNewId(),
                        Sender = "System",
                        Body = $"Member {res.Item2} is admin now"
                    });
                return (true, sendedToUsers);
            }

            return (false, null);
        }
    }
}
