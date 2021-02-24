using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMessengerBackend.MyMessengerProtocol
{
    public class Recognizer
    {
        private List<byte> _applicationPacket;
        public Recognizer()
        {
            _applicationPacket = new List<byte>();
            _applicationPacket.Capacity = 64;
        }

        public void Process(byte[] block, int size)
        {
            for(int i = 0; i< size; i++)
            {
                _applicationPacket.Add(block[i]);
                if (_applicationPacket.Count >= 4 && BitConverter.ToInt32(_applicationPacket.GetRange(_applicationPacket.Count-4, 4).ToArray(), 0) == Packet.MagicSequence)
                {
                    Packet fullPacket = new Packet(new ASCIIEncoding().GetString(_applicationPacket.GetRange(0, _applicationPacket.Count - 4).ToArray()));
                    Console.WriteLine(fullPacket.Payload);
                    _applicationPacket.Clear();
                }
            }
        }
    }
}
