using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConnection
{
    class Program
    {
        static void Main(string[] args)
        {
           

            var assembly = "Legejobber.XRM.CorePlugins.dll";
            var connectionstring = System.Configuration.ConfigurationManager.ConnectionStrings["CRM"].ConnectionString;
            var solution = "LegejobberXRMCorePlugins";

            var xrmregister = new XrmRegister.XrmRegister();
            xrmregister.ShowMessage += Xrmregister_ShowMessage;
            xrmregister.RegisterAssembly(assembly, connectionstring, solution);
        }

        private static void Xrmregister_ShowMessage(string message)
        {
            Console.WriteLine(message);
            //throw new NotImplementedException();
        }
    }
}
