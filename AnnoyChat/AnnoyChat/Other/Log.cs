using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnnoyChat
{
    public class Log
    {
        public static bool showWarnings = true;
        public static bool sendErrorsToChannel;
        public static void Normal(string msg)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"[LOG] {msg}");
            Console.ForegroundColor = ConsoleColor.White;
            UpdateTextFile($"[{DateTime.Now.ToString("HH:mm:ss")}] LOG: {msg}");
        }

        public static void Info(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[INFO] {msg}");
            Console.ForegroundColor = ConsoleColor.White;
            UpdateTextFile($"[{DateTime.Now.ToString("HH:mm:ss")}] INFO: {msg}");
        }

        public static void Warning(string msg)
        {
            if (showWarnings)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[WARNING] {msg}");
                Console.ForegroundColor = ConsoleColor.White;
                UpdateTextFile($"[{DateTime.Now.ToString("HH:mm:ss")}] WARNING: {msg}");
            }
        }

        public static async void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {msg}");
            Console.ForegroundColor = ConsoleColor.White;
            UpdateTextFile($"[{DateTime.Now.ToString("HH:mm:ss")}] ERROR: {msg}");
        }

        public static void UpdateTextFile(string message)
        {
            //Update text file:
            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"/Data/log.txt";
            var contents = new List<string>(File.ReadAllLines(path).Where(s => !s.Equals("") && !s.StartsWith("#")));

            contents.Add(message);

            File.WriteAllLines(path, contents);
        }

    }
}
