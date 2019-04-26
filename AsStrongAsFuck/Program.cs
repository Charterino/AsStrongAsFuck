using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsStrongAsFuck
{
    class Program
    {
        public static Worker Worker { get; set; }
        public static void Main(string[] args)
        {
            Console.WriteLine("AsStrongAsFuck by Charter.");
            Console.Write("Input an assembly: ");
            string path = Console.ReadLine();
            Worker = new Worker(path);
            Runtime.Random = new OwnRandom();
            Console.WriteLine("Choose options to obfuscate: ");

            for (int i = 0; i < Worker.Obfuscations.Count; i++)
            {
                Console.WriteLine(i + 1 + ") " + Worker.Obfuscations[i]);
            }
            string opts = Console.ReadLine();
            Worker.ExecuteObfuscations(opts);
            Worker.Save();
            Console.ReadLine();
        }
    }
}
