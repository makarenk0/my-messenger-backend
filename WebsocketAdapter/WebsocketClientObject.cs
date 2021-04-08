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
        public TcpClient client;

        private static byte _magicSequence = 0x7E;


        private string _userId;


        private NetworkStream _stream;
        private ApplicationProcessor _applicationProcessor;
        private List<byte> _accumulator;

        public WebsocketClientObject(TcpClient tcpClient)//Socket tcpClient)
        {
            client = tcpClient;
            _applicationProcessor = new ApplicationProcessor(UserLoggedIn);
            _accumulator = new List<byte>();
        }

        private void EstablishWebsocketConnection()
        {
            byte[] data = new byte[64];
            do
            {
                int bytes = _stream.Read(data, 0, data.Length);
                _accumulator.AddRange(data.Take(bytes).ToArray());
            }
            while (_stream.DataAvailable);
            HandleData(_accumulator);
            _accumulator.Clear();
        }

        public void Process(Object stateInfo)
        {

            try
            {
                _stream = client.GetStream();
                byte[] data = new byte[64]; // buffer

                //Need this to know when to pass delegate for updating
                ApplicationProcessor.UserLoggedIn userLoggedInAction = UserLoggedIn;

                EstablishWebsocketConnection();

                List<byte> buf = new List<byte>();
                while (!(client.Client.Poll(1000, SelectMode.SelectRead) && client.Available == 0))
                {

                    int bytes = 0;
                    do
                    {
                        bytes = _stream.Read(data, 0, data.Length);   //TO DO: handle System.IO.IOException
                        buf.AddRange(data.Take(bytes));
                        if (!_stream.DataAvailable)
                        {
                            HandleData(buf);
                            buf.Clear();
                        }
                    }
                    while (_stream.DataAvailable);
                    
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

        private void Accumulate(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                _accumulator.Add(bytes[i]);
                if (bytes[i] == _magicSequence)
                {
                    DeconstructPacket(_accumulator);
                    _accumulator.Clear();
                   
                }
            }
        }

        private void DeconstructPacket(List<byte> data)
        {
            string res = Encoding.ASCII.GetString(data.Take(data.Count - 1).ToArray());
            char packetType = res[0];
            string payloadBase64 = res.Substring(1);
            string payloadFromBase64 = Encoding.ASCII.GetString(Convert.FromBase64String(payloadBase64));
            Console.WriteLine(payloadFromBase64);
            var response = _applicationProcessor.Process(packetType, payloadFromBase64);
            ConstructPacketAndWrite(response);
        }

        private void ConstructPacketAndWrite((char, string) response)
        {
            string combinedStr = String.Concat(response.Item1, Convert.ToBase64String(Encoding.ASCII.GetBytes(response.Item2)), '~');
            byte[] res = Encoding.ASCII.GetBytes(combinedStr);

            byte[] toSend = null;
            if (res.Length < 126)
            {
                toSend = new byte[2 + res.Length];
                toSend[0] = 129;
                toSend[1] = (byte)res.Length;
                for (int i = 0; i < res.Length; i++)
                {
                    toSend[i + 2] = res[i];
                }
            }
            else if(res.Length > 125 && res.Length < 65536)
            {
                toSend = new byte[4 + res.Length];
                byte[] intBytes = BitConverter.GetBytes(res.Length);
                toSend[0] = 129;
                toSend[1] = 126;
                toSend[2] = intBytes[1];
                toSend[3] = intBytes[0];
                for(int i = 0; i< res.Length; i++)
                {
                    toSend[i + 4] = res[i];
                }
            }
            else if(res.Length > 65535)
            {
                toSend = new byte[10 + res.Length];
                byte[] intBytes = BitConverter.GetBytes(res.Length);
                toSend[0] = 129;
                toSend[1] = 127;

                for(int i = 0; i < 8; i++)
                {
                    toSend[9-i] = (i > 3 ? (byte)0 : intBytes[i]);
                }
              
                for (int i = 0; i < res.Length; i++)
                {
                    toSend[i + 5] = res[i];
                }
            }
            _stream.Write(toSend);
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
                    Accumulate(decoded);
                    //Console.WriteLine("{0}", text);
                }
                else
                    Console.WriteLine("mask bit not set");
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
            var updated = _applicationProcessor.UpdatePacketForChat(chatId);
            //_stream.Write();  //TO DO: send updte packet
        }
    }
}
