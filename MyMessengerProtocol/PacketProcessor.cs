using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace MyMessengerBackend.MyMessengerProtocol
{
    class PacketProcessor
    {
        private String _serverDerivedKey;

        public PacketProcessor()
        {

        }

        public Packet Process(Packet packet)
        {
            switch (packet.PacketType)
            {
                case '0':
                    return InitDiffieHellman(packet);
                case '1':

                default:
                    break;
            }
            return null;
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


            String json = Encoding.ASCII.GetString(packet.Payload);
            PublicKeyPayload clientPublicKey = JsonSerializer.Deserialize<PublicKeyPayload>(json);

            byte[] otherKeyFromBase64 = Convert.FromBase64String(clientPublicKey.Public_key);
            
 
            ECDiffieHellmanOpenSsl eCDiffie2 = new ECDiffieHellmanOpenSsl(256);
      

            int some = 0;
            eCDiffie2.ImportSubjectPublicKeyInfo(otherKeyFromBase64, out some);

         
            byte[] derivedKey = eCDiffie.DeriveKeyMaterial(eCDiffie2.PublicKey);

            String derivedKeyBase64 = Convert.ToBase64String(derivedKey);
            _serverDerivedKey = derivedKeyBase64;

#if DEBUG
            Console.WriteLine(String.Format("Derived key: {0}", derivedKeyBase64));
#endif


            PublicKeyPayload publicKey = new PublicKeyPayload();
            publicKey.Public_key = myPublicKeyBase64.Replace("\\u002B", "+");
            String publicKeyString = JsonSerializer.Serialize(publicKey);

            return new Packet('0', Encoding.ASCII.GetBytes(publicKeyString));
        }


        private Packet InitDiffieHellman__WINDOWS__(Packet packet)
        {
            ECDiffieHellmanCng eCDiffie = new ECDiffieHellmanCng(256);
            eCDiffie.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            eCDiffie.HashAlgorithm = CngAlgorithm.Sha256;


            byte[] myPublicKey = eCDiffie.ExportSubjectPublicKeyInfo();   //export in x509 format
            String myPublicKeyBase64 = Convert.ToBase64String(myPublicKey);


            String json = Encoding.ASCII.GetString(packet.Payload);
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
          

            String derivedKeyBase64 = Convert.ToBase64String(derivedKey);
            _serverDerivedKey = derivedKeyBase64;


#if DEBUG
            Console.WriteLine(String.Format("Derived key: {0}", derivedKeyBase64));
#endif

            PublicKeyPayload publicKey = new PublicKeyPayload();
            publicKey.Public_key = myPublicKeyBase64.Replace("\\u002B", "+");
            String publicKeyString = JsonSerializer.Serialize(publicKey);

            return new Packet('0', Encoding.ASCII.GetBytes(publicKeyString));
        }

    }
}
