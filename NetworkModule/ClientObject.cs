using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using MyMessengerBackend.MyMessengerProtocol;
using System.Threading.Tasks;
using MyMessengerBackend.ApplicationModule;

namespace MyMessengerBackend.NetworkModule
{
    class ClientObject
    {
        //public Socket client;
        public TcpClient client;


        private string _userId;
        //public List<>

        private NetworkStream _stream;
        Recognizer clientRecognizer;

        public ClientObject(TcpClient tcpClient)//Socket tcpClient)
        {
            client = tcpClient;
            
        }

        public void Process(Object stateInfo)
        {
            
            try
            {
                _stream = client.GetStream();
                byte[] data = new byte[64]; // buffer

                //Need this to know when to pass delegate for updating
                ApplicationProcessor.UserLoggedIn userLoggedInAction = UserLoggedIn;
                clientRecognizer = new Recognizer(userLoggedInAction);

                while (!(client.Client.Poll(1000, SelectMode.SelectRead) && client.Available == 0))
                {

                    // Request - Response
                    int bytes = 0;
                    do
                    {
                        bytes = _stream.Read(data, 0, data.Length);
                        if (clientRecognizer.Process(data, bytes))
                        {
                            _stream.Write(clientRecognizer.Response, 0, clientRecognizer.Response.Length);
                        }


                    }
                    while (_stream.DataAvailable);
                    // ---------------------

                    // Update from server

                    //clientRecognizer.Update();
                    //while(clientRecognizer.UpdateQueue.Count > 0)
                    //{
                    //    byte[] updateBytes = clientRecognizer.UpdateQueue.Dequeue();
                    //    stream.Write(updateBytes, 0, updateBytes.Length);
                    //}

                    // ---------------------
                    
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }

            // Removing updating delegate from active users table 
            if (!String.IsNullOrEmpty(_userId))
            {
                ApplicationProcessor._activeUsersTable.TryRemove(_userId, out _);
            }

            client.Close();
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
            ApplicationProcessor._activeUsersTable.TryAdd(_userId, SendUpdate);
        }

        private void SendUpdate(string chatId)
        {
#if DEBUG
            Console.WriteLine($"Update chat event triggered, chat id: {chatId}");
#endif
            _stream.Write(clientRecognizer.UpdateChat(chatId));
        }


    }
}
