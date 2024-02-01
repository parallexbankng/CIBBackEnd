using System.Security.Cryptography;
using System.Text;

namespace CIB.TransactionReversalService.Utils;

  public static class Encryption
  {
      public static string DecryptStrings(string encryptedString)
      {
          if(string.IsNullOrEmpty(encryptedString))
          {
            return encryptedString;
          }

          return OpenSSLDecrypt(encryptedString, EncrptionKey());
      }

      private static string EncrptionKey()
      {
          return "#!$96secure";
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
          MD5 md5 = MD5.Create();
          bool enoughBytesForKey = false;
          // See http://www.openssl.org/docs/crypto/EVP_BytesToKey.html#KEY_DERIVATION_ALGORITHM
          while (!enoughBytesForKey)
          {
              int preHashLength = currentHash.Length + password.Length + salt.Length;
              byte[] preHash = new byte[preHashLength];

              Buffer.BlockCopy(currentHash, 0, preHash, 0, currentHash.Length);
              Buffer.BlockCopy(password, 0, preHash, currentHash.Length, password.Length);
              Buffer.BlockCopy(salt, 0, preHash, currentHash.Length + password.Length, salt.Length);

              currentHash = md5.ComputeHash(preHash);
              concatenatedHashes.AddRange(currentHash);

              if (concatenatedHashes.Count >= 48)
                  enoughBytesForKey = true;
          }

          key = new byte[32];
          iv = new byte[16];
          concatenatedHashes.CopyTo(0, key, 0, 32);
          concatenatedHashes.CopyTo(32, iv, 0, 16);

          md5.Clear();
          md5 = null;
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
  }
