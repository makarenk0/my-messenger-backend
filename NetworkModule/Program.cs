
using MyMessengerBackend.ServiceModule;
using System.Collections.Concurrent;
using System.Configuration;
using System.Threading;
using WebsocketAdapter;

namespace MyMessengerBackend.NetworkModule
{
    public class Program
    {
        private const int MOBILE_CLIENT_PORT = 20; // receving port for mobile clients (raw tcp)
        private const int WEB_CLIENT_PORT = 80; // receving port for web clients through websockets
        //private const string IP_ADDRESS = "192.168.1.19";
        private static string IP_ADDRESS = ConfigurationManager.AppSettings["server_ip"];  // private ip (google cloud machine)

        public static void Main(string[] args)
        {
            ServiceProcessor._activeUsersTable = new ConcurrentDictionary<string, ServiceProcessor.UpdateAction>();
            new Thread(() =>{ new MainListener(MOBILE_CLIENT_PORT, IP_ADDRESS); }).Start();   //mobile clients
            new Thread(() => { new MainWebsocketListener(WEB_CLIENT_PORT, IP_ADDRESS); }).Start();  //web clients
        }
    }
}
