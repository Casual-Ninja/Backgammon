using System;
using System.Text;
using System.Security.Cryptography;

namespace TicTacToe_MCTS_NEAT
{
    public static class EncryptionHandler
    {
        private static RSACryptoServiceProvider rsa;
        public static string publicKey { get; private set; }
        public static string privateKey { get; private set; }

        private static string oldKeyInfo;

        private static UnicodeEncoding byteConverter = new UnicodeEncoding();

        public static void Initialize(RSACryptoServiceProvider rsa)
        {
            EncryptionHandler.rsa = rsa;

            publicKey = rsa.ToXmlString(false);
            privateKey = rsa.ToXmlString(true);
            oldKeyInfo = privateKey;

            Console.WriteLine(publicKey);

            Console.WriteLine("The key info at start:\n" + oldKeyInfo);
        }

        private static byte[] RSAEncrypt(byte[] DataToEncrypt, bool DoOAEPPadding)
        {
            try
            {
                //The RSA only needs to include the public key information.

                //Encrypt the passed byte array and specify OAEP padding.  
                //OAEP padding is only available on Microsoft Windows XP or
                //later.
                return rsa.Encrypt(DataToEncrypt, DoOAEPPadding);
            }
            //Catch and display a CryptographicException  
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);

                return null;
            }
        }

        private static byte[] RSADecrypt(byte[] DataToDecrypt, bool DoOAEPPadding)
        {
            try
            {
                //The RSA needs to include the private key information.

                //Decrypt the passed byte array and specify OAEP padding.  
                //OAEP padding is only available on Microsoft Windows XP or
                //later.  
                return rsa.Decrypt(DataToDecrypt, DoOAEPPadding);
            }
            //Catch and display a CryptographicException  
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());

                return null;
            }
        }

        public static string RSAEncrypt(string dataToEncrypt)
        {
            return byteConverter.GetString(RSAEncrypt(byteConverter.GetBytes(dataToEncrypt), false));
        }

        public static string RSADecrypt(string dataToDecrypt)
        {
            byte[] data = Convert.FromBase64String(dataToDecrypt);

            return byteConverter.GetString(RSADecrypt(data, false));
        }
    }
}
