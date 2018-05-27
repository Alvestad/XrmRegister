using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmEarlyBoundTemplate.Utility
{
    public class Prompt
    {
        public enum Option
        {
            GenerateMetaDataStructs,
            GenereateEarlyBound,
            None
        }

        public static Option PromptOption()
        {
            Console.WriteLine("1: Generate Metadata Structs");
            Console.WriteLine("2: Genereate Earlybound");
            Option option = Option.None;
            Console.Write("Choose: ");
            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.D2)
                {
                    if (key.Key == ConsoleKey.D1)
                        option = Option.GenerateMetaDataStructs;
                    else
                        option = Option.GenereateEarlyBound;
                    break;
                }
            }
            Console.WriteLine();
            return option;
        }
    }
}
