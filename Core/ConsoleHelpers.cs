using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StorageManagementKit.Core
{
    public static class ConsoleHelpers
    {
        /// <summary>
        /// Determines if an invalid command has been used
        /// </summary>
        public static bool AllCommandExist(string[] arguments, Type constants)
        {
            FieldInfo[] fields = constants.GetFields(BindingFlags.Static | BindingFlags.Public);

            List<string> names = new List<string>();

            fields.ToList().ForEach(f => names.Add(f.GetValue(null).ToString().ToLower()));

            List<string> arg2 = arguments.ToList().GetRange(0, arguments.Length - 1);
            var errors = arg2.Where(a => !names.Any(n => n.Equals(GetCommandName(a.ToLower())))).ToList();

            if (errors.Count > 0)
            {
                Console.WriteLine($"Unknown command '{errors[0]}'. Type ? to help more help");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the argument is included with command line arguments.
        /// </summary>
        public static bool CommandArgExists(string[] args, string command)
        {
            string command2a = $"/{command}=".ToLower();
            string command2b = $"/{command}".ToLower();

            return args.Any(a => a.ToLower().StartsWith(command2a) || a.ToLower() == command2b);
        }

        /// <summary>
        /// Returns the name of the command
        /// </summary>
        public static string GetCommandName(string arg)
        {
            arg = arg.Replace("/", "");

            int i = arg.IndexOf("=");

            if (i == -1)
                return arg.ToLower();
            else
                return arg.Substring(0, i);
        }

        /// <summary>
        /// Extract the value from the command line arguments
        /// </summary>
        public static string GetCommandArgValue(string[] args, string command)
        {
            string command2 = $"/{command}=".ToLower();
            string dest = args.FirstOrDefault(a => a.ToLower().StartsWith(command2));

            if ((dest == null) || (dest.Length <= command2.Length))
                return null;

            return dest
                .Substring(command2.Length, dest.Length - command2.Length)
                .ToLower();
        }
    }
}
