using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Options;

namespace AsStrongAsFuck
{
    class Program
    {
        static string file = System.IO.Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        public static Worker Worker { get; set; }
        public static void Main(string[] args)
        {

            bool show_help = false;
            string input_path="";
            string output_path="";
            int[] obfuscations = { };
            List<string> extra;


            var p = new OptionSet() {

                { "i|input=", "Input assembly", option => input_path=option },
                { "o|output=", "Destination of the asssembly", option => output_path=option },
                { "b|obfuscations=", "Obfuscations", option =>
                        {
                string[] obf_str=option.Split(',');
                obfuscations=new int[obf_str.Length];
                for (int i=0; i<obf_str.Length;i++) {
                                obfuscations[i]=int.Parse(obf_str[i]);
                                }
                    }
                        },
                { "?|help|h", "Prints out the options.", option => show_help = option != null }



            };

            try
            {
                extra = p.Parse(args);
                if (show_help)
                {
                    ShowHelp(p);
                    return;
                }
                if (String.IsNullOrEmpty(input_path))
                {
                    throw new OptionException("input_path is required","input_path");
                }
                if (String.IsNullOrEmpty(output_path))
                {
                    throw new OptionException("output_path is required", "input_path");
                }
                if (obfuscations.Length == 0)
                {
                    throw new OptionException("You must specify at least one obfuscation", "obfuscations");
                }
            }
            catch (OptionException e)
            {
                Console.Write($"{file}: ");
                Console.WriteLine(e.Message);
                Console.WriteLine($"Try `{file} --help' for more information.");
                return;
            }
            Console.WriteLine("AsStrongAsFuck by Charter.");

            Worker = new Worker(input_path);
            //Console.WriteLine("Choose options to obfuscate: ");

            //for (int i = 0; i < Worker.Obfuscations.Count; i++)
            //{
            //    Console.WriteLine(i + 1 + ") " + Worker.Obfuscations[i]);
            //}
            //string opts = Console.ReadLine();
            Worker.ExecuteObfuscations(obfuscations);
            Worker.Save(output_path);
            //Console.ReadLine();
        }
        private static void ShowHelp(OptionSet p)
        {
            //Console.WriteLine("Showing help");
            Console.WriteLine($"Usage {file}: ");
            p.WriteOptionDescriptions(Console.Out);
            var w = new Worker(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName); // ugly, but I CBA
            for (int i = 0; i < w.Obfuscations.Count; i++)
            {
                Console.WriteLine(i + 1 + ") " + w.Obfuscations[i]);
            }
        }
    } // Program

}
