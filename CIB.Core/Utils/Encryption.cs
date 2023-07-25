
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CIB.Core.Utils
{
    public static class Encryption
    {
        public static string OpenSSLEncrypt(string plainText, string passphrase)
        {
            // generate salt
            byte[] key, iv;
            byte[] salt = new byte[8];
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(salt);
            DeriveKeyAndIV(passphrase, salt, out key, out iv);
            // encrypt bytes
            byte[] encryptedBytes = EncryptStringToBytesAes(plainText, key, iv);
            // add salt as first 8 bytes
            byte[] encryptedBytesWithSalt = new byte[salt.Length + encryptedBytes.Length + 8];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("Salted__"), 0, encryptedBytesWithSalt, 0, 8);
            Buffer.BlockCopy(salt, 0, encryptedBytesWithSalt, 8, salt.Length);
            Buffer.BlockCopy(encryptedBytes, 0, encryptedBytesWithSalt, salt.Length + 8, encryptedBytes.Length);
            // base64 encode
            return Convert.ToBase64String(encryptedBytesWithSalt);
        }

        public static string OpenSSLDecrypt(string encrypted, string passphrase)
        {
            // base 64 decode
            byte[] encryptedBytesWithSalt = Convert.FromBase64String(encrypted);
            // extract salt (first 8 bytes of encrypted)
            byte[] salt = new byte[8];
            byte[] encryptedBytes = new byte[encryptedBytesWithSalt.Length - salt.Length - 8];
            Buffer.BlockCopy(encryptedBytesWithSalt, 8, salt, 0, salt.Length);
            Buffer.BlockCopy(encryptedBytesWithSalt, salt.Length + 8, encryptedBytes, 0, encryptedBytes.Length);
            // get key and iv
            byte[] key, iv;
            DeriveKeyAndIV(passphrase, salt, out key, out iv);
            return DecryptStringFromBytesAes(encryptedBytes, key, iv);
        }

        private static void DeriveKeyAndIV(string passphrase, byte[] salt, out byte[] key, out byte[] iv)
        {
            // generate key and iv
            List<byte> concatenatedHashes = new List<byte>(48);
            byte[] password = Encoding.UTF8.GetBytes(passphrase);
            byte[] currentHash = new byte[0];
            var sha256 = MD5.Create();
            bool enoughBytesForKey = false;
            // See http://www.openssl.org/docs/crypto/EVP_BytesToKey.html#KEY_DERIVATION_ALGORITHM
            while (!enoughBytesForKey)
            {
                int preHashLength = currentHash.Length + password.Length + salt.Length;
                byte[] preHash = new byte[preHashLength];

                Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
                Buffer.BlockCopy(password, 0, preHash, currentHash.Length, password.Length);
                Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + password.Length, salt.Length);

                currentHash = sha256.ComputeHash(preHash);
                concatenatedHashes.AddRange(currentHash);

                if (concatenatedHashes.Count >= 48)
                    enoughBytesForKey = true;
            }

            key = new byte[32];
            iv = new byte[16];
            concatenatedHashes.CopyTo(0, key, 0, 32);
            concatenatedHashes.CopyTo(32, iv, 0, 16);

            sha256.Clear();
            sha256 = null;
        }

        static byte[] EncryptStringToBytesAes(string plainText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException("iv");

            // Declare the stream used to encrypt to an in memory
            // array of bytes.
            MemoryStream msEncrypt;

            // Declare the RijndaelManaged object
            // used to encrypt the data.
            RijndaelManaged aesAlg = null;

            try
            {
                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                msEncrypt = new MemoryStream();
                using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using StreamWriter swEncrypt = new StreamWriter(csEncrypt);

                //Write all data to the stream.
                swEncrypt.Write(plainText);
                swEncrypt.Flush();
                swEncrypt.Close();
            }
            finally
            {
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            // Return the encrypted bytes from the memory stream.
            return msEncrypt.ToArray();
        }

        static string DecryptStringFromBytesAes(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (key == null || key.Length <= 0)
                throw new ArgumentNullException("key");
            if (iv == null || iv.Length <= 0)
                throw new ArgumentNullException("iv");

            // Declare the RijndaelManaged object
            // used to decrypt the data.
            RijndaelManaged aesAlg = null;

            // Declare the string used to hold
            // the decrypted text.
            string plaintext;

            try
            {
                // Create a RijndaelManaged object
                // with the specified key and IV.
                aesAlg = new RijndaelManaged { Mode = CipherMode.CBC, KeySize = 256, BlockSize = 128, Key = key, IV = iv };

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                // Create the streams used for decryption.
                using MemoryStream msDecrypt = new MemoryStream(cipherText);
                using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using StreamReader srDecrypt = new StreamReader(csDecrypt);
                // Read the decrypted bytes from the decrypting stream
                // and place them in a string.
                plaintext = srDecrypt.ReadToEnd();
                srDecrypt.Close();
            }
            finally
            {
                // Clear the RijndaelManaged object.
                aesAlg?.Clear();
            }

            return plaintext;
        }
        public static string DecriptPassword(string password)
        {
            return OpenSSLDecrypt(password, GetEncrptionKey());
        }

        public static  string EncriptPassword(string password)
        {
            return OpenSSLEncrypt(password, GetEncrptionKey());
        }

        public static string EncryptStrings(string data)
        {
            return OpenSSLEncrypt(data, EncrptionKey());
        }

        public static string EncryptAccountStrings(string data)
        {
            return OpenSSLEncrypt(data, AccountEncrptionKey());
        }

        public static string DecryptStrings(string encryptedString)
        {
            if(string.IsNullOrEmpty(encryptedString))
            {
                return encryptedString;
            }

            return OpenSSLDecrypt(encryptedString, EncrptionKey());
        }
        public static string DecryptDebitAccount(string encryptedString)
        {
            if(string.IsNullOrEmpty(encryptedString))
            {
                return encryptedString;
            }

            return OpenSSLDecrypt(encryptedString, AccountEncrptionKey());
        }

        public static int DecryptInt(string encryptedInt)
        {
            if (string.IsNullOrEmpty(encryptedInt))
            {
                return 0;
            }

            return int.Parse(OpenSSLDecrypt(encryptedInt, EncrptionKey()));
        }

        public static Guid DecryptGuid(string encryptedGuid)
        {
            if (string.IsNullOrEmpty(encryptedGuid))
            {
                return Guid.NewGuid();
            }
            return Guid.Parse(OpenSSLDecrypt(encryptedGuid.ToString(), EncrptionKey()));
        }

        public static long DecryptLong(string encryptedLong)
        {
            if (string.IsNullOrEmpty(encryptedLong))
            {
                return 0;
            }

            return long.Parse(OpenSSLDecrypt(encryptedLong.ToString(), EncrptionKey()));
        }

        public static bool DecryptBooleans(string encryptedBoolean)
        {
            if (string.IsNullOrEmpty(encryptedBoolean))
            {
                return false;
            }

            return bool.Parse(OpenSSLDecrypt(encryptedBoolean, EncrptionKey()));
        }

        public static decimal DecryptDecimals(string encryptedDecimal)
        {
            if (string.IsNullOrEmpty(encryptedDecimal))
            {
                return 0;
            }

            return decimal.Parse(OpenSSLDecrypt(encryptedDecimal, EncrptionKey()));
        }

        public static DateTime DecryptDateTime(string encryptedDateTime)
        {
            if (string.IsNullOrEmpty(encryptedDateTime))
            {
                return DateTime.MinValue;
            }

            return DateTime.Parse(OpenSSLDecrypt(encryptedDateTime, EncrptionKey()));
        }

        public static  string GetEncrptionKey()
        {
            return "!#$81secure";
        }
        private static string EncrptionKey()
        {
            return "#!$96secure";
        }
        private static string AccountEncrptionKey()
        {
            return "#!$50secure";
        }
    }
}