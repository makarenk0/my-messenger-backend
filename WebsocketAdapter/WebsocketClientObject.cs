using Fleck;
using MyMessengerBackend.ServiceModule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebsocketAdapter
{
    public class WebsocketClientObject
    {
        //public TcpClient client;

        //private static byte _magicSequence = 0x7E;


        private string _userId;


        //private NetworkStream _stream;
        private ServiceProcessor _applicationProcessor;
        //private List<byte> _accumulator;
        //private bool _toClose = false;

        private IWebSocketConnection _connection;

        public WebsocketClientObject(IWebSocketConnection connection)//TcpClient tcpClient)//Socket tcpClient)
        {
            //client = tcpClient;
            _applicationProcessor = new ServiceProcessor(UserLoggedIn);
            // _accumulator = new List<byte>();
            _connection = connection;

            _connection.OnMessage = message => { DeconstructPacketAndProcess(message); };
            _connection.OnClose = () => { Close(); };

            ServiceProcessor.UserLoggedIn userLoggedInAction = UserLoggedIn;
        }

        private void ConstructPacketAndWrite((char, string) response)
        {
            string combinedStr = String.Concat(response.Item1, Convert.ToBase64String(Encoding.ASCII.GetBytes(response.Item2)), '~');
            byte[] res = Encoding.ASCII.GetBytes(combinedStr);

            
            _connection.Send(res);
        }

        private void DeconstructPacketAndProcess(string data)
        {
            string res = data.Substring(0, data.Length - 1);
            char packetType = res[0];
            string payloadBase64 = res.Substring(1);
            string payloadFromBase64 = Encoding.ASCII.GetString(Convert.FromBase64String(payloadBase64));
            Console.WriteLine(payloadFromBase64);
            var response = _applicationProcessor.Process(packetType, payloadFromBase64);
            ConstructPacketAndWrite(response);
        }

        private void Close()
        {
            if (!String.IsNullOrEmpty(_userId))
            {
                ServiceProcessor._activeUsersTable.TryRemove(_userId, out _);
            }
#if DEBUG
            Console.WriteLine($"Connection closed, user id: {_userId}");
#endif
        }

        private void UserLoggedIn(String userId)
        {
#if DEBUG
            Console.WriteLine($"User with id {userId} logged in");
#endif
            _userId = userId;
            ServiceProcessor._activeUsersTable.TryAdd(_userId, SendUpdate);
        }

        private void SendUpdate(string chatId)
        {
#if DEBUG
            Console.WriteLine($"Update chat event triggered, chat id: {chatId}");
#endif
            var updated = _applicationProcessor.UpdatePacketForChat(chatId);
            ConstructPacketAndWrite(updated); //send update packet
        }
    }
}
