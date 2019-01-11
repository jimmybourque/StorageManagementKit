using StorageManagementKit.Core.Diagnostics;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StorageManagementKit.Core.Crypto
{
    /// <summary>
    /// Encrypt/decrypt files with a 3-DES algorithm
    /// </summary>
    public class TripleDES
    {
        private const string KEY_SPLIT_MARK = "A0B1C1";
        private const string KEY_LINER = "---------SMK-KEY---------";

        private readonly TripleDESCryptoServiceProvider _des = new TripleDESCryptoServiceProvider();
        private readonly UTF8Encoding _utf8 = new UTF8Encoding();
        private readonly byte[] _key;
        private readonly byte[] _iv;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The 3-DES key</param>
        /// <param name="iv">The 3-DES vector</param>
        public TripleDES(byte[] key, byte[] iv)
        {
            _key = key;
            _iv = iv;
        }

        /// <summary>
        /// Encrypt a file
        /// </summary>
        public byte[] SecureFile(string file, out string md5)
        {
            try
            {
                if (!File.Exists(file))
                    throw new FileNotFoundException(file);

                var bytes = File.ReadAllBytes(file);

                md5 = MD5.CreateHash(bytes);
                return Encrypt(bytes);
            }
            catch (Exception ex)
            {
                throw new SmkCryptoException($"securing {file} failed", ex);
            }
        }

        /// <summary>
        /// Decrypt a file
        /// </summary>
        public byte[] UnsecureFile(string encryptedFile)
        {
            try
            {
                string file = encryptedFile.Substring(0, encryptedFile.Length - Constants.EncryptedExt.Length);

                if (!File.Exists(encryptedFile))
                    throw new FileNotFoundException(encryptedFile);

                if (File.Exists(file))
                    File.Delete(file);

                var bytes = File.ReadAllBytes(encryptedFile);

                return Decrypt(bytes);
            }
            catch (Exception ex)
            {
                throw new SmkCryptoException($"Unsecuring {encryptedFile} failed", ex);
            }
        }

        /// <summary>
        /// Encrypt an array of byte
        /// </summary>
        /// <returns>Returns an encrypted array of bytes</returns>
        public byte[] Encrypt(byte[] input)
        {
            return Transform(input, _des.CreateEncryptor(_key, _iv));
        }

        /// <summary>
        /// Decrypt an array of byte
        /// </summary>
        /// <returns>Returns a decrypted array of bytes</returns>
        public byte[] Decrypt(byte[] input)
        {
            return Transform(input, _des.CreateDecryptor(_key, _iv));
        }

        /// <summary>
        /// Encrypts a plain text and returns a cipher text
        /// </summary>
        public string Encrypt(string text)
        {
            byte[] input = _utf8.GetBytes(text);
            byte[] output = Transform(input, _des.CreateEncryptor(_key, _iv));
            return Convert.ToBase64String(output);
        }

        /// <summary>
        /// Decrypts a cipher text and returns a plain text
        /// </summary>
        public string Decrypt(string text)
        {
            byte[] input = Convert.FromBase64String(text);
            byte[] output = Transform(input, _des.CreateDecryptor(_key, _iv));
            return _utf8.GetString(output);
        }

        /// <summary>
        /// Use this method en encrypt data with a 3-DES algorithm
        /// </summary>
        private byte[] Transform(byte[] input, ICryptoTransform CryptoTransform)
        {
            using (MemoryStream memStream = new MemoryStream())
            using (CryptoStream cryptStream = new CryptoStream(memStream, CryptoTransform, CryptoStreamMode.Write))
            {
                cryptStream.Write(input, 0, input.Length);
                cryptStream.FlushFinalBlock();

                memStream.Position = 0;
                return memStream.ToArray();
            }
        }

        /// <summary>
        /// Reads the 3-DES key from a file
        /// </summary>
        public static bool LoadKeyFile(string keyfile, out byte[] key, out byte[] iv, ILogging logger)
        {
            if (!File.Exists(keyfile))
            {
                logger.WriteLog(ErrorCodes.TripleDES_KeyfileNotFound,
                    string.Format(ErrorResources.TripleDES_KeyfileNotFound, keyfile),
                    Severity.Error, VerboseLevel.User);
                key = null;
                iv = null;
                return false;
            }

            try
            {
                string[] lines = File.ReadAllText(keyfile).Replace(KEY_LINER, "").Split(KEY_SPLIT_MARK);

                // The key must include the 'key' and the vector IV
                if (lines.Length != 2)
                {
                    logger.WriteLog(ErrorCodes.TripleDES_InvalidKey,
                        string.Format(ErrorResources.TripleDES_InvalidKey, keyfile),
                        Severity.Error, VerboseLevel.User);
                    key = null;
                    iv = null;
                    return false;
                }

                key = Convert.FromBase64String(lines[0]);
                iv = Convert.FromBase64String(lines[1]);
            }
            catch (Exception ex)
            {
                throw new SmkCryptoException(
                    $"Invalid key file '{keyfile}'{Environment.NewLine}{Environment.NewLine}" +
                    $"Your file must be formatted as below (header and footer lines must be included):{Environment.NewLine}{Environment.NewLine}" +
                    $"---------SMK-KEY---------{Environment.NewLine}" +
                    $@"kMPgm9bKkSyBxN8l9aZaVrty4l333RGAA0FF34as4545CE1/g={Environment.NewLine}" +
                    $"---------SMK-KEY---------", ex);
            }

            return true;
        }

        /// <summary>
        /// Generate a new 3-DES key
        /// </summary>
        public static void GenerateKey(string keyfile, ILogging logger)
        {
            try
            {
                using (System.Security.Cryptography.TripleDES des = System.Security.Cryptography.TripleDES.Create())
                {
                    string key = Convert.ToBase64String(des.Key) + KEY_SPLIT_MARK + Convert.ToBase64String(des.IV);
                    key = $"{KEY_LINER}{Environment.NewLine}{key}{Environment.NewLine}{KEY_LINER}";

                    File.WriteAllText(keyfile, key);
                }
            }
            catch (Exception ex)
            {
                logger.WriteLog(ErrorCodes.TripleDES_GenerateKey,
                    string.Format(ErrorResources.TripleDES_GenerateKey, ex.Message),
                    Severity.Error, VerboseLevel.User);
            }
        }
    }
}
