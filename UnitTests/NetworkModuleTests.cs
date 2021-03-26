using MyMessengerBackend.NetworkModule;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class NetworkModuleTests
    {
        private const int PORT = 20;
        private const string IP_ADDRESS = "192.168.1.19";

        private readonly ITestOutputHelper _output;

        public NetworkModuleTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Main_ClientsShouldAccept()
        {
            Task t = Task.Run(() => { Program.Main(new string[0]); });


            TcpClient client = null;
            try
            {
                client = new TcpClient(IP_ADDRESS, PORT);
                NetworkStream stream = client.GetStream();

                //while (true)
                //{
                //    Console.Write(userName + ": ");
                //    // ввод сообщения
                //    string message = Console.ReadLine();
                //    message = String.Format("{0}: {1}", userName, message);
                //    // преобразуем сообщение в массив байтов
                //    byte[] data = Encoding.Unicode.GetBytes(message);
                //    // отправка сообщения
                //    stream.Write(data, 0, data.Length);

                //    // получаем ответ
                //    data = new byte[64]; // буфер для получаемых данных
                //    StringBuilder builder = new StringBuilder();
                //    int bytes = 0;
                //    do
                //    {
                //        bytes = stream.Read(data, 0, data.Length);
                //        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                //    }
                //    while (stream.DataAvailable);

                //    message = builder.ToString();
                //    Console.WriteLine("Сервер: {0}", message);
                //}
            }
            catch (Exception ex)
            {
               _output.WriteLine(ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

       
    }
}
