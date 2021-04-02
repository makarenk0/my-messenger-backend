using MyMessengerBackend.ApplicationModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebsocketAdapter
{
    public class WebsocketClientObject
    {
        //public Socket client;
        public TcpClient client;


        private string _userId;
        //public List<>

        private NetworkStream _stream;
        
        public WebsocketClientObject(TcpClient tcpClient)//Socket tcpClient)
        {
            client = tcpClient;
        }

        public void Process(Object stateInfo)
        {

            try
            {
                _stream = client.GetStream();
                byte[] data = new byte[64]; // buffer

                //Need this to know when to pass delegate for updating
                ApplicationProcessor.UserLoggedIn userLoggedInAction = UserLoggedIn;
                List<byte> result = new List<byte>();

                while (!(client.Client.Poll(1000, SelectMode.SelectRead) && client.Available == 0))
                {
                    //while (!_stream.DataAvailable) ;
                    //while (client.Available < 3) ; // match against "get"


                    int bytes = 0;
                    do
                    {
                        bytes = _stream.Read(data, 0, data.Length);   //TO DO: handle System.IO.IOException
                        result.AddRange(data.Take(bytes));
                    }
                    while (_stream.DataAvailable);
                    if(result.Count > 0)
                    {
                        HandleData(result);
                        result.Clear();
                    }
                   
                    


                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.Message);
            }

            // Removing updating delegate from active users table 
            if (!String.IsNullOrEmpty(_userId))
            {
                ApplicationProcessor._activeUsersTable.TryRemove(_userId, out _);
            }

            client.Close();
#if DEBUG
            Console.WriteLine($"Connection closed, user id: {_userId}");
#endif
        }


        private void HandleData(List<byte> bytes)
        {
            string s = Encoding.UTF8.GetString(bytes.ToArray());

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                _stream.Write(response, 0, response.Length);
            }
            else
            {
                bool fin = (bytes[0] & 0b10000000) != 0,
                    mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                    msglen = bytes[1] - 128, // & 0111 1111
                    offset = 2;

                if (msglen == 126)
                {
                    // was ToUInt16(bytes, offset) but the result is incorrect
                    msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                    offset = 4;
                }
                else if (msglen == 127)
                {
                    Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                    // i don't really know the byte order, please edit this
                    // msglen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                    // offset = 10;
                }

                if (msglen == 0)
                    Console.WriteLine("msglen == 0");
                else if (mask)
                {
                    byte[] decoded = new byte[msglen];
                    byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                    offset += 4;

                    for (int i = 0; i < msglen; ++i)
                        decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                    string text = Encoding.UTF8.GetString(decoded);
                    Console.WriteLine("{0}", text);
                }
                else
                    Console.WriteLine("mask bit not set");

                Console.WriteLine();
            }
        }
    

        private void UserLoggedIn(String userId)
        {
#if DEBUG
            Console.WriteLine($"User with id {userId} logged in");
#endif
            _userId = userId;
            ApplicationProcessor._activeUsersTable.TryAdd(_userId, SendUpdate);
        }

        private void SendUpdate(string chatId)
        {
#if DEBUG
            Console.WriteLine($"Update chat event triggered, chat id: {chatId}");
#endif
            //_stream.Write(clientRecognizer.UpdateChat(chatId));
        }
    }
}
