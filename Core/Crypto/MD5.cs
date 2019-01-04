using StorageManagementKit.Core.Diagnostics;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StorageManagementKit.Core.Crypto
{
    public static class MD5
    {
        /// <summary>
        /// Creates a digital signature from an array of bytes
        /// </summary>
        public static string CreateHash(byte[] data)
        {
            StringBuilder hash = new StringBuilder();

            using (MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider())
            {
                byte[] bytes = md5provider.ComputeHash(data);

                for (int i = 0; i < bytes.Length; i++)
                    hash.Append(bytes[i].ToString("x2"));
            }

            return hash.ToString();
        }

        public static string GetMD5FromString(string data, MD5Kind kind, ILogging logger)
        {
            string[] lines = data.Split(Environment.NewLine);
            return _GetMD5Value(kind, logger, lines);
        }

        public static string GetMD5FromFile(string md5File, MD5Kind kind, ILogging logger)
        {
            string[] lines = File.ReadAllLines(md5File);
            return _GetMD5Value(kind, logger, lines);
        }

        private static string _GetMD5Value(MD5Kind kind, ILogging logger, string[] lines)
        {
            string key = lines.FirstOrDefault(l => l.StartsWith(kind.ToString()));

            if (key == null)
            {
                logger.WriteLog(ErrorCodes.LocalDirectorySource_InvalidMD5File,
                    ErrorResources.LocalDirectorySource_InvalidMD5File,
                    Severity.Error, VerboseLevel.User);
                return null;
            }

            return key.Replace(kind.ToString() + ":", "");
        }
    }
}
