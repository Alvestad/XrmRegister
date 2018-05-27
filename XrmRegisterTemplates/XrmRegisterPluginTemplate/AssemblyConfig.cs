using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrmRegister;

namespace XrmRegisterPluginTemplate
{
    public class AssemblyConfig : XrmAssemblyConfig
    {
        public AssemblyConfig()
        {
            base.configItem = new XrmAssemblyConfigItem {
                IsolationMode = IsolationMode.None,
                SourceType = SourceType.Database,
                XrmAssemblyType = XrmAssemblyType.Plugin
            };
        }
    }
}
