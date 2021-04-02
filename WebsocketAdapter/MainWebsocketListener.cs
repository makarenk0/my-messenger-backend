using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
            _port = port;
            _ipAddress = ipAddress;

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
            _listener = new TcpListener(IPAddress.Parse(_ipAddress), _port);//new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Start();

            //int minWorker, minIOC;
            //ThreadPool.GetMinThreads(out minWorker, out minIOC);
            //ThreadPool.SetMinThreads(DEFAULT_PARALLEL_THREADS_NUM, minIOC);
            //Console.WriteLine(String.Concat("Resetting default thread pool volume from ", minWorker, " to ", DEFAULT_PARALLEL_THREADS_NUM));

            Listen();
        }

        private void Listen()
        {
            try
            {
                int clientsAmount = 0;
                while (true)
                {
                    Console.WriteLine("Waiting for web clients...");
                    //Socket client = _listener.Accept();
                    TcpClient client = _listener.AcceptTcpClient();
                    WebsocketClientObject clientObject = new WebsocketClientObject(client);

                    ThreadPool.QueueUserWorkItem(clientObject.Process);

                    ++clientsAmount;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
