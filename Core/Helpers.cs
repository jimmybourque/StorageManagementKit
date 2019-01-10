using System;
using System.IO;
using System.Text;

namespace StorageManagementKit.Core
{
    public static class Helpers
    {
        public static void WriteProgress(string text)
        {
            Console.CursorVisible = false;

            Console.CursorLeft = 0;
            Console.Write(" ".PadRight(30));
            Console.CursorLeft = 0;
            Console.Write(text);
        }

        public static void WriteProgress(int percent)
        {
            ConsoleColor bckColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.White;

                percent = (int)Math.Round((double)percent / 10, 0);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < percent; i++)
                    sb.Append("=");

                if (sb.Length > 0)
                    sb[sb.Length - 1] = '>';

                while (sb.Length < 10)
                    sb.Append(" ");

                WriteProgress($"Transferring [{sb.ToString()}]");
            }
            finally
            {
                Console.ForegroundColor = bckColor;
            }
        }

        internal static void MandatoryValue(string v, object cryptoKey)
        {
            throw new NotImplementedException();
        }

        public static string FormatByteSize(long size)
        {
            if (size > 1024 * 1024 * 1024)
                return $"{Math.Round(size / (double)(1024 * 1024 * 1024), 1)}Gb";
            else if (size > 1024 * 1024)
                return $"{Math.Round(size / (double)(1024 * 1024), 1)}Mb";
            else if (size > 1024)
                return $"{Math.Round(size / (double)1024, 1)}Kb";
            else
                return $"{size}b";
        }


        /// <summary>
        /// Format the full path of a file to be displayed to the user
        /// </summary>
        public static string FormatDisplayFileName(bool wideDisplay, string fullName)
        {
            fullName = fullName.Replace("/", "\\");

            if (wideDisplay)
                return fullName.PrefixRoot();

            string directory = Path.GetDirectoryName(fullName);
            string filename = Path.GetFileName(fullName);

            // Makes the full path shorter for a better displaying
            if (!string.IsNullOrEmpty(directory) && (directory != "\\"))
            {
                var dirs = directory.Split("\\");

                for (int i = 0; i < dirs.Length; i++)
                    if (dirs[i].Length > 9)
                        dirs[i] = dirs[i].Substring(0, 7) + "..";

                string path = string.Join("\\", dirs);
                path = $"{path}\\{filename}";

                if (path.EndsWith(Constants.EncryptedExt))
                    path = path.Substring(0, path.Length - Constants.EncryptedExt.Length);

                return path.PrefixRoot();
            }

            // No need to format the displayed path
            if (fullName.EndsWith(Constants.EncryptedExt))
                return fullName.Substring(0, fullName.Length - Constants.EncryptedExt.Length).PrefixRoot();
            else
                return fullName.PrefixRoot();
        }

        internal static void MandatoryValue(string key, string value)
        {
            if (value == null) throw new SmkException($"Missing value for '{key}'");
        }

        /// <summary>
        /// Force a backslash as prefix
        /// </summary>
        public static string PrefixRoot(this string obj)
        {
            while ((obj.Length > 0) && (obj[0].Equals('\\') || obj.Equals('/')))
                obj = obj.Substring(1, obj.Length - 1);

            return $@"\\{obj}";
        }

        /// <summary>
        /// Force a backslash as prefix
        /// </summary>
        public static string PrefixBackslash(this string obj)
        {
            while ((obj.Length > 0) && (obj[0].Equals('\\') || obj.Equals('/')))
                obj = obj.Substring(1, obj.Length - 1);

            return $@"\{obj}";
        }

        /// <summary>
        /// Removes the ".encrypted" file extension to the name
        /// </summary>
        public static string RemoveSecExt(string filename)
        {
            if (filename.EndsWith(Constants.EncryptedExt))
                return filename.Substring(0, filename.Length - Constants.EncryptedExt.Length);
            else
                return filename;
        }

        /// <summary>
        /// Removes the "/" or "\" at beginning of the value
        /// </summary>
        public static string RemoveRootSlash(string file)
        {
            if (file.StartsWith("/") || file.StartsWith("\\"))
                return file.Substring(1, file.Length - 1);
            else
                return file;
        }

        /// <summary>
        /// Removes the folder separator at the end of the path if it is present
        /// </summary>
        public static string RemoveTail(this string value)
        {
            if (value.EndsWith("\\"))
                return value.Substring(0, value.Length - 1);
            else
                return value;
        }
    }
}
