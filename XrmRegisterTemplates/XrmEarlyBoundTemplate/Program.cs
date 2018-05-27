using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmEarlyBoundTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionstring = System.Configuration.ConfigurationManager.ConnectionStrings["CRM"].ConnectionString;
            var generator = new XrmEarlyBound.XrmEarlyBoundGenerator();
            generator.ShowMessage += Generator_ShowMessage;

            var prompt = Utility.Prompt.PromptOption();

            if (prompt == Utility.Prompt.Option.GenereateEarlyBound)
            {
                Console.WriteLine("Generate earlybound");
                var entities = new List<string>()
                {
                };

                var globalOptionSets = new List<string>()
                {
                };

                var actions = new List<string>
                {
                };

                generator.GenerateEarlyBoundEntities(
                    connectionstring,
                    entities,
                    globalOptionSets,
                    actions,
                    "Entities",
                    "..\\..\\Entities.cs"
                    );

                Console.WriteLine("Done");
            }
            else if (prompt == Utility.Prompt.Option.GenerateMetaDataStructs)
            {
                Console.WriteLine("Generate structs");
                generator.GenerateActionsMetaDataStruct(connectionstring, "..\\..\\Structs\\XrmActions.cs");
                generator.GenerateOptionSetMetaDataStruct(connectionstring, "..\\..\\Structs\\XrmOptionSets.cs");
                generator.GenerateEntitiesMetaDataStruct(connectionstring, "..\\..\\Structs\\XrmEntities.cs");
                Console.WriteLine("Done");
            }
            Console.ReadKey();
        }

        private static void Generator_ShowMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
