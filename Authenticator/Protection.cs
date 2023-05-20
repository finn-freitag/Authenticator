using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.IO;

namespace Authenticator
{
    public static class Protection
    {
        public static string MasterPassword = "";

        public static byte[] Protect(byte[] bytes, Encryption encryption)
        {
            switch (encryption)
            {
                case Encryption.None:
                    return bytes;
                case Encryption.User:
                    return ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
                case Encryption.Machine:
                    return ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);
                case Encryption.Masterpassword:
                    return AESEncrypt(bytes, MasterPassword);
            }
            return bytes;
        }

        public static byte[] UnProtect(byte[] bytes, Encryption encryption)
        {
            switch (encryption)
            {
                case Encryption.None:
                    return bytes;
                case Encryption.User:
                    return ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                case Encryption.Machine:
                    return ProtectedData.Unprotect(bytes, null, DataProtectionScope.LocalMachine);
                case Encryption.Masterpassword:
                    return AESDecrypt(bytes, MasterPassword);
            }
            return bytes;
        }

        public static byte[] AESEncrypt(byte[] data, string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            Aes aes = Aes.Create();
            aes.Key = SHA256.Create().ComputeHash(passwordBytes);
            aes.IV = MD5.Create().ComputeHash(passwordBytes);
            //aes.Mode = CipherMode.CBC;
            //aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            MemoryStream ms_enc = new MemoryStream();
            CryptoStream cs_enc = new CryptoStream(ms_enc, encryptor, CryptoStreamMode.Write);
            cs_enc.Write(data, 0, data.Length);
            cs_enc.FlushFinalBlock();
            //ms_enc.Flush();

            return ms_enc.ToArray();
        }

        public static byte[] AESDecrypt(byte[] data, string password)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            Aes aes = Aes.Create();
            aes.Key = SHA256.Create().ComputeHash(passwordBytes);
            aes.IV = MD5.Create().ComputeHash(passwordBytes);
            //aes.Mode = CipherMode.CBC;
            //aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            MemoryStream ms_dec = new MemoryStream(data);
            CryptoStream cs_dec = new CryptoStream(ms_dec, decryptor, CryptoStreamMode.Read);
            return ReadFully(cs_dec);
        }

        public static byte[] ReadFully(Stream input) // https://stackoverflow.com/questions/221925/creating-a-byte-array-from-a-stream
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
