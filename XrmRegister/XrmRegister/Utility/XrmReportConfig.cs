using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister.Utility
{
    public class XrmReportConfig
    {
        public string EntityLogicalName { get; set; }
        public string ReportFileName { get; set; }
        public List<ReportVisibilityCode> ReportVisibilityCodes { get; set; }
        public int LanguageCode { get; set; }
        public string ReportName { get; set; }
    }

    public enum ReportVisibilityCode
    {
        ReportsGrid = 1,
        Form = 2,
        Grid = 3,
    };
}
