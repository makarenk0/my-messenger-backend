using DTOs.FromClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApplicationModule
{
    public class ObjectTypeMapper
    {
        public readonly static Dictionary<char, Type> table = new Dictionary<char, Type>() { 
            { '1', typeof(RegistrationPayload) } ,
            { '2', typeof(LoginPayload) },
            { '3', typeof(FindUserPayload) },
            { '4', typeof(SendChatMessagePayload) },
            { '6', typeof(InitChatPayload) },
            { '7', typeof(SubscriptionToUpdatePayload) },
            { '8', typeof(InitChatPayload) },
            { 'a', typeof(SendChatMessagePayload) },
        };
    }
}
