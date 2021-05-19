using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketAdapter
{
    public class MainWebsocketListener
    {
        private readonly int _port;
        private readonly string _ipAddress;
        private readonly TcpListener _listener;
        private const int DEFAULT_PARALLEL_THREADS_NUM = 15;

        public MainWebsocketListener(int port, string ipAddress)
        {
            Console.WriteLine($"Web socket working on ip adress:   {ipAddress}");

            //X509Certificate2 certificate = new X509Certificate2("C:\\test.com.pfx", "14032001");
            
            var server = new WebSocketServer($"ws://{ipAddress}");
            //server.Certificate = certificate;
            server.Start(socket =>
            {
                Console.WriteLine("New user");
                WebsocketClientObject clientObject = new WebsocketClientObject(socket);
            });
        }
    }
}
