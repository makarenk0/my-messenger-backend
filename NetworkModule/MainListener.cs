using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyMessengerBackend.NetworkModule
{
    class MainListener
    {
        private readonly int _port;
        private readonly string _ipAddress;
        //private readonly Socket _listener;
        private readonly TcpListener _listener;
        private const int DEFAULT_PARALLEL_THREADS_NUM = 15;

        public MainListener(int port, string ipAddress)
        {
            Console.WriteLine($"Server for mobile clients working on ip adress: {ipAddress}");
            _port = port;
            _ipAddress = ipAddress;

            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
            
            _listener = new TcpListener(IPAddress.Parse(_ipAddress), _port);//new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listener.Start();

            int minWorker, minIOC;

            ThreadPool.GetMaxThreads(out int maxT, out int work);
            Console.WriteLine($"Max threads in pool: {maxT}");
            ThreadPool.GetMinThreads(out minWorker, out minIOC);
            ThreadPool.SetMinThreads(DEFAULT_PARALLEL_THREADS_NUM, minIOC);
            Console.WriteLine(String.Concat("Resetting default thread pool volume from ", minWorker, " to ", DEFAULT_PARALLEL_THREADS_NUM));

            Listen();
        }

        private void Listen()
        {
            try
            {
                int clientsAmount = 0;
                while (true)
                {
                    Console.WriteLine("Waiting for mobile clients...");
                    //Socket client = _listener.Accept();
                    TcpClient client = _listener.AcceptTcpClient();
                    ClientObject clientObject = new ClientObject(client);

                    ThreadPool.QueueUserWorkItem(clientObject.Process);
                  
                    ++clientsAmount;

                    Console.WriteLine(String.Concat("Current clients number: ", clientsAmount));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
