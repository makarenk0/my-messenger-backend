
using MyMessengerBackend.ApplicationModule;
using System.Collections.Concurrent;

namespace MyMessengerBackend.NetworkModule
{
    public class Program
    {
        private const int PORT = 20; // receving port
        private const string IP_ADDRESS = "192.168.1.19";
        // private const string IP_ADDRESS = "10.156.0.2";  // private ip (google cloud machine)

        public static void Main(string[] args)
        {
            ApplicationProcessor._activeUsersTable = new ConcurrentDictionary<string, ApplicationProcessor.UpdateAction>();
            new MainListener(PORT, IP_ADDRESS);
        }
    }
}
