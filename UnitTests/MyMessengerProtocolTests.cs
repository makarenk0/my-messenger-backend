using MyMessengerBackend.ServiceModule;
using MyMessengerBackend.MyMessengerProtocol;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class MyMessengerProtocolTests
    {
        private readonly ITestOutputHelper _output;

        public MyMessengerProtocolTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void InitDiffieHellman_ShouldGenerateSameDerivedKeys()   //imitating client to test encrypting
        {

            ECDiffieHellmanCng eCDiffie = new ECDiffieHellmanCng(256);
            eCDiffie.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            eCDiffie.HashAlgorithm = CngAlgorithm.Sha256;

            byte[] myPublicKey = eCDiffie.ExportSubjectPublicKeyInfo();   //export in x509 format
            String myPublicKeyBase64 = Convert.ToBase64String(myPublicKey);


            //Passing to project
            var packetToPreoject = new Packet('0', Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Concat("{", String.Format("\"Public_key\": \"{0}\"", myPublicKeyBase64), "}"))));

            ServiceProcessor.UserLoggedIn stubMethod = StubMethod;
            PacketProcessor packetProcessor = new PacketProcessor(stubMethod);
            Packet response = packetProcessor.Process(packetToPreoject);

            PublicKeyPayload serverPublicKey = JsonSerializer.Deserialize<PublicKeyPayload>(Convert.FromBase64String(response.Payload));

            byte[] otherKeyFromBase64 = Convert.FromBase64String(serverPublicKey.Public_key);
            ECDiffieHellmanCng eCDiffie2 = new ECDiffieHellmanCng(256);
            eCDiffie2.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            eCDiffie2.HashAlgorithm = CngAlgorithm.Sha256;

            int some = 0;
            eCDiffie2.ImportSubjectPublicKeyInfo(otherKeyFromBase64, out some);

            byte[] otherKeyDecoded = eCDiffie2.PublicKey.ToByteArray();
            CngKey k = CngKey.Import(otherKeyDecoded, CngKeyBlobFormat.EccPublicBlob);
            byte[] derivedKey = eCDiffie.DeriveKeyMaterial(k);

            string actual = Convert.ToBase64String(packetProcessor.EncryptionModule.DerivedKey);
            string expected = Convert.ToBase64String(derivedKey);


            Assert.Equal(expected, actual);
        }

        private void StubMethod(string stubParametr){}
    }
}
