using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrmRegister;
using XrmRegister.Utility;

namespace XrmRegisterReportDeployTemplate
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionstring = System.Configuration.ConfigurationManager.ConnectionStrings["CRM"].ConnectionString;
            var solution = "SolutionName";

            var xrmregister = new XrmRegister.XrmRegister();
            xrmregister.ShowMessage += Xrmregister_ShowMessage;

            var config = new List<XrmReportConfig>();

            //config.Add(new XrmReportConfig
            //{
            //    ReportFileName = "Report.rdl",
            //    EntityLogicalName = "account,contact",
            //    ReportVisibilityCodes = new List<ReportVisibilityCode> {
            //        ReportVisibilityCode.ReportsGrid,
            //        ReportVisibilityCode.Form
            //    }
            //});

            xrmregister.RegisterReports(connectionstring, solution, config, false);

            if (!args.Contains("nostop"))
                Console.ReadKey();
        }

        private static void Xrmregister_ShowMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
