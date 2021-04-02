
using MyMessengerBackend.ApplicationModule;
using System.Collections.Concurrent;
using System.Threading;
using WebsocketAdapter;

namespace MyMessengerBackend.NetworkModule
{
    public class Program
    {
        private const int MOBILE_CLIENT_PORT = 20; // receving port for mobile clients (raw tcp)
        private const int WEB_CLIENT_PORT = 80; // receving port for web clients through websockets
        private const string IP_ADDRESS = "192.168.1.19";
        // private const string IP_ADDRESS = "10.156.0.2";  // private ip (google cloud machine)

        public static void Main(string[] args)
        {
            ApplicationProcessor._activeUsersTable = new ConcurrentDictionary<string, ApplicationProcessor.UpdateAction>();
            new Thread(() =>{ new MainListener(MOBILE_CLIENT_PORT, IP_ADDRESS); }).Start();   //mobile clients
            new Thread(() => { new MainWebsocketListener(WEB_CLIENT_PORT, IP_ADDRESS); }).Start();  //web clients
        }
    }
}
