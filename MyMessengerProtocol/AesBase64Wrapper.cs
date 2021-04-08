using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;

namespace MyMessengerBackend.MyMessengerProtocol
{
    public class AesBase64Wrapper
    {
        // SHOULD GENERATE EACH MESSAGE AND TRANSFER WITH PACKET!!!
        private static string IV = "IV_VALUE_16_BYTE";
        private static string SALT = "SALT_VALUE";

        private int _aesIterationsNum;
        private int _aesKeyLengthBytes;

        public AesBase64Wrapper(int aesIterationsNum, int aesKeyLengthInBits)
        {
            _aesIterationsNum = aesIterationsNum;
            _aesKeyLengthBytes = aesKeyLengthInBits / 8;
        }


        private byte[] _derivedKey;
        public byte[] DerivedKey { get => _derivedKey; set => _derivedKey = value; }


        public string EncryptAndEncode(string raw)
        {
            using (var csp = new AesCryptoServiceProvider())
            {
                ICryptoTransform e = GetCryptoTransform(csp, true);
                byte[] inputBuffer = Encoding.UTF8.GetBytes(raw);
                byte[] output = e.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                string encrypted = Convert.ToBase64String(output);
                return encrypted;
            }
        }

        public (bool,string) DecodeAndDecrypt(string encrypted)
        {
            using (var csp = new AesCryptoServiceProvider())
            {
                var d = GetCryptoTransform(csp, false);
                byte[] output = Convert.FromBase64String(encrypted);
                byte[] decryptedOutput = null;
                try
                {
                    decryptedOutput = d.TransformFinalBlock(output, 0, output.Length);
                }
                catch(CryptographicException e)
                {
                    return (false, null);
                }
           
                string decypted = Encoding.UTF8.GetString(decryptedOutput);
                return (true, decypted);
            }
        }

        private ICryptoTransform GetCryptoTransform(AesCryptoServiceProvider csp, bool encrypting)
        {
            csp.Mode = CipherMode.CBC;
            csp.Padding = PaddingMode.PKCS7;

            //_derivedKey
            var spec = new Rfc2898DeriveBytes(_derivedKey, Encoding.UTF8.GetBytes(SALT), _aesIterationsNum); //TO DO: handle derived key is null

            byte[] key = spec.GetBytes(_aesKeyLengthBytes);


            csp.IV = Encoding.UTF8.GetBytes(IV);
            csp.Key = key;
            if (encrypting)
            {
                return csp.CreateEncryptor();
            }
            return csp.CreateDecryptor();
        }
    }
}
