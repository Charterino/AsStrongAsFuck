using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck
{
    public class Logger
    {
        public static void LogMessage(string pre, string past, ConsoleColor PastColor)
        {
            Console.Write(pre);
            Console.ForegroundColor = PastColor;
            Console.WriteLine(past);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
