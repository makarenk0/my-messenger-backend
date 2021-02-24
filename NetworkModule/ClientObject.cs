using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using MyMessengerBackend.MyMessengerProtocol;

namespace MyMessengerBackend.NetworkModule
{
    class ClientObject
    {
        //public Socket client;
        public TcpClient client;

        //public List<>

        public ClientObject(TcpClient tcpClient)//Socket tcpClient)
        {
            client = tcpClient;
            
        }

        public void Process(Object stateInfo)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                //client.Rece
                byte[] data = new byte[64]; // buffer
                Recognizer clientRecognizer = new Recognizer();

                
                while (!(client.Client.Poll(1000, SelectMode.SelectRead) && client.Available == 0))
                {
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {

                        //Console.WriteLine(String.Concat("Buffer size: ", client.ReceiveBufferSize));
                        //bytes = client.Receive(data);
                        //builder.Append(Encoding.ASCII.GetString(data, 0, bytes));

                        bytes = stream.Read(data, 0, data.Length);

                        clientRecognizer.Process(data, bytes);


                    }
                    while (stream.DataAvailable);

                    //if(bytes != 0)
                    //{
                    //    string message = builder.ToString();

                    //    Console.WriteLine(message);
                    //    message = "Hello from C# server";//message.Substring(message.IndexOf(':') + 1).Trim().ToUpper();
                    //    data = Encoding.ASCII.GetBytes(message);
                    //    stream.Write(data, 0, data.Length);
                    //}
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }
            client.Close();
            Console.WriteLine("Connection closed");
        }

    }
}
