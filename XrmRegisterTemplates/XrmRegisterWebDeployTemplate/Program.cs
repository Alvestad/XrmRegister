﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegisterWebDeployTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            var assembly = "AssemblyName.dll";
            var connectionstring = System.Configuration.ConfigurationManager.ConnectionStrings["CRM"].ConnectionString;
            var solution = "SolutionName";

            var xrmregister = new XrmRegister.XrmRegister();
            xrmregister.ShowMessage += Xrmregister_ShowMessage;

            xrmregister.RegisterWeb(assembly, connectionstring, solution, true);
            
            if (args.Length == 0)
                Console.ReadKey();
        }

        private static void Xrmregister_ShowMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
