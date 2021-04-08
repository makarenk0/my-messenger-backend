using MyMessengerBackend.ApplicationModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyMessengerBackend.MyMessengerProtocol
{
    public class Recognizer
    {
        PacketProcessor _pktProcessor;
        private List<byte> _applicationPacket;

        private byte[] _response;

        public Recognizer(ApplicationProcessor.UserLoggedIn action)
        {
            _pktProcessor = new PacketProcessor(action);
            _applicationPacket = new List<byte>();
            _applicationPacket.Capacity = 64;
        }

        public byte[] Response { get => _response; set => _response = value; }

        public bool Process(byte[] block, int size)
        {
            bool responseReady = false;
            for(int i = 0; i< size; i++)
            {
                _applicationPacket.Add(block[i]);
                if (block[i] == Packet.MagicSequence)
                {
                    Response = FormPacketAndProcess();
                    responseReady = true;
                }
            }
            return responseReady;
        }

        public byte[] UpdateChat(string chatId)
        {
            return _pktProcessor.UpdateChat(chatId).GetBytesForm();
        }


        private byte[] FormPacketAndProcess()
        {
            byte[] bytesPayload = _applicationPacket.Skip(1).Take(_applicationPacket.Count - 2).ToArray();
            String payloadBase64 = Encoding.ASCII.GetString(bytesPayload);
            Packet arrived = new Packet(Convert.ToChar(_applicationPacket[0]), payloadBase64);

            Packet response = _pktProcessor.Process(arrived);
            _applicationPacket.Clear();
            
            if(response != null)
            {
                return response.GetBytesForm();
            }
            return new byte[0];
        }
    }
}
