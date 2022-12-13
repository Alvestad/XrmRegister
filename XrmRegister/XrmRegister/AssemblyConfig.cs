using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister
{
    public class AssemblyConfig : XrmAssemblyConfig
    {
        public AssemblyConfig()
        {
            this.configItem = new XrmAssemblyConfigItem
            {
                IsolationMode = IsolationMode.None,
                SourceType = SourceType.Database,
                XrmAssemblyType = XrmAssemblyType.Plugin
            };
        }
    }
}
