using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace XrmRegister.Utility
{
    public class Plugin : ConfigurationElement
    {
        [ConfigurationProperty("typeName", IsRequired = true)]
        public string TypeName
        {
            get { return (string)this["typeName"]; }
            set { this["typeName"] = value; }

        }

        [ConfigurationProperty("stepName", IsRequired = true)]
        public string StepName
        {
            get { return (string)this["stepName"]; }
            set { this["stepName"] = value; }

        }

        [ConfigurationProperty("secureConfig", IsRequired = false)]
        public string SecureConfig
        {
            get { return (string)this["secureConfig"]; }
            set { this["secureConfig"] = value; }
        }

        [ConfigurationProperty("unsecureConfig", IsRequired = false)]
        public string UnsecureConfig
        {
            get { return (string)this["unsecureConfig"]; }
            set { this["unsecureConfig"] = value; }
        }

        public int NameSpaceRank
        {
            get { return TypeName.Count(x => x == '.');  }
        }
    }

    public class Plugins : ConfigurationElementCollection
    {
        public Plugin this[int index]
        {
            get
            {
                return base.BaseGet(index) as Plugin;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }

        public new Plugin this[string responseString]
        {
            get { return (Plugin)BaseGet(responseString); }
            set
            {
                if (BaseGet(responseString) != null)
                {
                    BaseRemoveAt(BaseIndexOf(BaseGet(responseString)));
                }
                BaseAdd(value);
            }
        }

        protected override System.Configuration.ConfigurationElement CreateNewElement()
        {
            return new Plugin();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return $"{((Plugin)element).TypeName}-{((Plugin)element).StepName}";
        }
    }

    public class PluginConfig
        : ConfigurationSection
    {

        public static List<Plugin> GetConfigList()
        {
            var config = GetConfig();
            var list = new List<Plugin>();

            if (config == null || config.Plugins == null || config.Plugins.Count == 0)
                return list;

            foreach (var p in config.Plugins)
                list.Add((Plugin)p);

            return list;
        }
        public static PluginConfig GetConfig()
        {
            return (PluginConfig)System.Configuration.ConfigurationManager.GetSection("PluginConfig") ?? new PluginConfig();
        }

        [System.Configuration.ConfigurationProperty("Plugins")]
        [ConfigurationCollection(typeof(Plugins), AddItemName = "Plugin")]
        public Plugins Plugins
        {
            get
            {
                object o = this["Plugins"];
                return o as Plugins;
            }
        }
    }
}
