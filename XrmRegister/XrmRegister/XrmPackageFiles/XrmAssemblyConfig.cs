using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister
{
    public abstract partial class XrmAssemblyConfig
    {
        internal XrmAssemblyConfig()
        {

        }

        protected XrmAssemblyConfigItem configItem { get; set; }

        public string AssemblyConfig
        {
            get
            {
                var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(XrmAssemblyConfigItem));
                var ms = new MemoryStream();
                json_ser.WriteObject(ms, configItem);
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                return sr.ReadToEnd();
            }
        }
    }

    public enum SourceType
    {
        Database = 0,
        Disk = 1
    }

    public enum IsolationMode
    {
        Sandbox = 2,
        None = 1
    }

    public enum XrmAssemblyType
    {
        Plugin = 0,
        Workflow = 1,
        DataProvider = 2
    }


    public class XrmAssemblyConfigItem
    {
        public SourceType SourceType { get; set; }
        public IsolationMode IsolationMode { get; set; }
        public XrmAssemblyType XrmAssemblyType { get; set; }
    }
}
