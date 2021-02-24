using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessengerBackend.MyMessengerProtocol
{
    class Packet
    {
        
        //private long _packetId;
        private string _payload;

        private static int _magicSequence = 0x4D454F57;

        
        //public long PacketId { get => _packetId; set => _packetId = value; }
        public string Payload { get => _payload; set => _payload = value; }
        public static int MagicSequence { get => _magicSequence; }

        public Packet(string payload)
        {
            Payload = payload;
        }
        
    }
}
