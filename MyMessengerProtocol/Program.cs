using System;
using System.Configuration;
using System.Collections.Specialized;

namespace MyMessengerBackend.MyMessengerProtocol
{
    class Program
    {
        static void Main(string[] args)
        {

            string appSettings = ConfigurationManager.AppSettings["AES_KEY_LENGTH"];
        
            Console.WriteLine(appSettings);
        }
    }
}
