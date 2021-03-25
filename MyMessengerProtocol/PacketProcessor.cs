using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Configuration;
using MyMessengerBackend.DatabaseModule;
using MyMessengerBackend.ApplicationModule;

namespace MyMessengerBackend.MyMessengerProtocol
{
    public class PacketProcessor
    {
        private AesBase64Wrapper _encryptionModule;
        private ApplicationProcessor _applicationProcessor;

        private ApplicationProcessor.UserLoggedIn _action;

        public AesBase64Wrapper EncryptionModule { get => _encryptionModule; set => _encryptionModule = value; }

        public PacketProcessor(ApplicationProcessor.UserLoggedIn action)
        {
            _encryptionModule = new AesBase64Wrapper(Int32.Parse(ConfigurationManager.AppSettings["KEY_DERIVATION_ITERATIONS_NUMBER"]),
                Int32.Parse(ConfigurationManager.AppSettings["AES_KEY_LENGTH"]));
            _action = action;
        }

        public Packet Process(Packet packet)
        {
            if (packet.PacketType == '0')
            {
                Packet establishPacket = InitDiffieHellman(packet);
                _applicationProcessor = new ApplicationProcessor(_action);
                return establishPacket;
            }
            return ProcessEncryptedPacket(packet);
        }

        public Packet UpdateChat(string chatId)
        {
            var resUpd = _applicationProcessor.UpdatePacketForChat(chatId);
            return EncryptPacket(new Packet(resUpd.Item1, resUpd.Item2));
        }

        private Packet InitDiffieHellman(Packet packet)
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))  // My server running in google cloud on debian
            {
                return InitDiffieHellman__LINUX__(packet);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))  // For debugging locally on my pc
            {
                return InitDiffieHellman__WINDOWS__(packet);
            }
            throw new PlatformNotSupportedException("Supported only linux and windows");
        }


        private Packet InitDiffieHellman__LINUX__(Packet packet)
        {
       
            ECDiffieHellmanOpenSsl eCDiffie = new ECDiffieHellmanOpenSsl(256);
     
            byte[] myPublicKey = eCDiffie.ExportSubjectPublicKeyInfo();   //export in x509 format
            String myPublicKeyBase64 = Convert.ToBase64String(myPublicKey);


            String json = Encoding.ASCII.GetString(Convert.FromBase64String(packet.Payload));
            PublicKeyPayload clientPublicKey = JsonSerializer.Deserialize<PublicKeyPayload>(json);

            byte[] otherKeyFromBase64 = Convert.FromBase64String(clientPublicKey.Public_key);
            
 
            ECDiffieHellmanOpenSsl eCDiffie2 = new ECDiffieHellmanOpenSsl(256);
      

            int some = 0;
            eCDiffie2.ImportSubjectPublicKeyInfo(otherKeyFromBase64, out some);

         
            byte[] derivedKey = eCDiffie.DeriveKeyMaterial(eCDiffie2.PublicKey);
            _encryptionModule.DerivedKey = derivedKey;

#if DEBUG
            String derivedKeyBase64 = Convert.ToBase64String(derivedKey);
            Console.WriteLine(String.Format("Derived key: {0}", derivedKeyBase64));
#endif


            PublicKeyPayload publicKey = new PublicKeyPayload();
            publicKey.Public_key = myPublicKeyBase64.Replace("\\u002B", "+");
            String publicKeyString = JsonSerializer.Serialize(publicKey);


            return new Packet('0', Convert.ToBase64String(Encoding.ASCII.GetBytes(publicKeyString)));
        }


        private Packet InitDiffieHellman__WINDOWS__(Packet packet)
        {
            ECDiffieHellmanCng eCDiffie = new ECDiffieHellmanCng(256);
            eCDiffie.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            eCDiffie.HashAlgorithm = CngAlgorithm.Sha256;


            byte[] myPublicKey = eCDiffie.ExportSubjectPublicKeyInfo();   //export in x509 format
            String myPublicKeyBase64 = Convert.ToBase64String(myPublicKey);


            String json = Encoding.ASCII.GetString(Convert.FromBase64String(packet.Payload));
            PublicKeyPayload clientPublicKey = JsonSerializer.Deserialize<PublicKeyPayload>(json);

            byte[] otherKeyFromBase64 = Convert.FromBase64String(clientPublicKey.Public_key);
            ECDiffieHellmanCng eCDiffie2 = new ECDiffieHellmanCng(256);
            eCDiffie2.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            eCDiffie2.HashAlgorithm = CngAlgorithm.Sha256;



            int some = 0;
            eCDiffie2.ImportSubjectPublicKeyInfo(otherKeyFromBase64, out some);

            byte[] otherKeyDecoded = eCDiffie2.PublicKey.ToByteArray();
            CngKey k = CngKey.Import(otherKeyDecoded, CngKeyBlobFormat.EccPublicBlob);
            byte[] derivedKey = eCDiffie.DeriveKeyMaterial(k);
            _encryptionModule.DerivedKey = derivedKey;


#if DEBUG
            String derivedKeyBase64 = Convert.ToBase64String(derivedKey);
            Console.WriteLine(String.Format("Derived key: {0}", derivedKeyBase64));
#endif

            PublicKeyPayload publicKey = new PublicKeyPayload();
            publicKey.Public_key = myPublicKeyBase64.Replace("\\u002B", "+");
            String publicKeyString = JsonSerializer.Serialize(publicKey);


            return new Packet('0', Convert.ToBase64String(Encoding.ASCII.GetBytes(publicKeyString)));
        }


        private Packet ProcessEncryptedPacket(Packet packet)
        {
            string decryptedPayload = _encryptionModule.DecodeAndDecrypt(packet.Payload);
            var response = _applicationProcessor.Process(packet.PacketType, decryptedPayload);
            return EncryptPacket(new Packet(response.Item1, response.Item2));
        }

        public Packet EncryptPacket(Packet packet)
        {
            packet.Payload = _encryptionModule.EncryptAndEncode(packet.Payload);
            return packet;
        }

    }
}
