using System;
using System.Threading;

namespace Server_DCO
{
    internal class Program
    {
        private static Thread _threadConsole;
        
        public static void Main()
        {
            _threadConsole = new Thread(ConsoleThread);
            _threadConsole.Start();
            
            Database.Analytics = new Analytics();

            NetworkConfig.InitNetwork();
            NetworkConfig.socket.StartListening(8000,5, 1);
            Database.CorrectPath();

            Console.WriteLine("Server started.");
        }

        private static void ConsoleThread()
        {
            while (true)
            {
                
            }
        }
    }
}