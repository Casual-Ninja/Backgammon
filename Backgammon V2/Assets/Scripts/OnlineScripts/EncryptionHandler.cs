using System;
using System.Text;
using System.Security.Cryptography;

public static class EncryptionHandler
{
    private static RSACryptoServiceProvider rsa;

    private static UnicodeEncoding byteConverter = new UnicodeEncoding();

    public static void Initialize(RSACryptoServiceProvider RSA)
    {
        EncryptionHandler.rsa = RSA;

        UnityEngine.Debug.Log("public key: " + RSA.ToXmlString(false));
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
        UnityEngine.Debug.Log("Encrypting using:\n" + rsa.ToXmlString(false));
        byte[] encryptedData = RSAEncrypt(byteConverter.GetBytes(dataToEncrypt), false);
        UnityEngine.Debug.Log($"Encrypting:'{dataToEncrypt}' and got:\n{HelperSpace.HelperMethods.ArrayToString(encryptedData)}");
        return Convert.ToBase64String(encryptedData);
    }

    public static string RSADecrypt(string dataToDecrypt)
    {
        return byteConverter.GetString(RSADecrypt(byteConverter.GetBytes(dataToDecrypt), false));
    }
}
