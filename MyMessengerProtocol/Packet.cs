using System;
using System.Collections.Generic;
using System.Text;

namespace MyMessengerBackend.MyMessengerProtocol
{
    public class Packet
    {

        //private long _packetId;
        private char _packetType;
        private string _payload; //base 64 format
        private static byte _magicSequence = 0x7E;  // not included in base 64 encoding "~"

        
        //public long PacketId { get => _packetId; set => _packetId = value; }
        public string Payload { get => _payload; set => _payload = value; }
        public static byte MagicSequence { get => _magicSequence; }
        public char PacketType { get => _packetType; set => _packetType = value; }

        public Packet(char packetType, string payload)
        {
            PacketType = packetType;
            Payload = payload;      
        }

        public byte[] GetBytesForm()
        {
            int index = 0;

            byte[] payloadBytes = Encoding.ASCII.GetBytes(_payload);

            byte[] result = new byte[sizeof(byte) + payloadBytes.Length + sizeof(byte)];
            result[index] = Convert.ToByte(_packetType);
            ++index;

            
            for (; index < payloadBytes.Length + 1; index++)
            {
                result[index] = payloadBytes[index-1];
            }      
            result[index] = _magicSequence;
            return result;
        }
        
    }
}
