using StorageManagementKit.Core;
using System;
using System.Linq;
using System.Reflection;

namespace StorageManagementKit.Recover
{
    class Program
    {
        public static void Main()
        {
            Console.CursorVisible = false;

            try
            {
                // Excludes the program name
                string[] args = Environment.GetCommandLineArgs()
                    .Where(a => a.ToLower() != Assembly.GetExecutingAssembly().Location.ToLower())
                    .ToArray();

                new Engine(args).Process();
            }
            catch (SmkException ex)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"There was a problem:");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"There was an unknown error:");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
#if DEBUG

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine($"Press a enter to exit");
            Console.ReadLine();
#endif
        }
    }
}
