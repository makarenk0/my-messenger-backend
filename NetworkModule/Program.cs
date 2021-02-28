using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MyMessengerBackend.NetworkModule
{
    class Program
    {
        private const int PORT = 20; // receving port

        //private const string IP_ADDRESS = "192.168.1.19";
         private const string IP_ADDRESS = "10.156.0.2";  // private ip (google cloud machine)

        static void Main(string[] args)
        {
            new MainListener(PORT, IP_ADDRESS);
        }
    }
}
