using StorageManagementKit.Core;
using System;
using System.Linq;
using System.Reflection;

namespace StorageManagementKit.Copy
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

                using (Engine engine = new Engine(args))
                {
                    engine.Process();
                }
            }
            catch (JboBackupException)
            {
                Console.WriteLine("Execution failed, see the log file");
            }
            catch (Exception)
            {
                Console.WriteLine("Internal Error");
            }
#if DEBUG
            Console.WriteLine();
            Console.WriteLine($"Press a enter to exit");
            Console.ReadLine();
#endif
        }
    }
}
