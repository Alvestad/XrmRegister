using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace XrmRegister.Utility
{
    public class XrmAssemblyConfiguration
    {
        public Collection<Image> GetImages()
        {
            var result = new Collection<Image>();
            foreach (var plugintype in this.PluginTypes)
                foreach (var step in plugintype.Steps)
                    foreach (var image in step.Images)
                        result.Add(image);
            return result;
        }

        public Collection<PluginStep> GetSteps()
        {
            var result = new Collection<PluginStep>();
            foreach (var plugintype in this.PluginTypes)
                foreach (var step in plugintype.Steps)
                    result.Add(step);
            return result;
        }

        public XrmAssemblyConfigItem AssemblyConfig { get; set; }
        public Collection<XrmPluginType> PluginTypes { get; set; }
        public Collection <XrmWorkflowType> WorkFlowTypes { get; set; }
        public Collection<XrmDataProviderType> DataProviderTypes { get; set; }

        public static XrmAssemblyConfiguration GetConfiguration(Assembly assembly)
        {
            var config = new XrmAssemblyConfiguration();
            config.PluginTypes = new Collection<XrmPluginType>();
            config.WorkFlowTypes = new Collection<XrmWorkflowType>();
            config.DataProviderTypes = new Collection<XrmDataProviderType>();

            foreach (var type in assembly.GetLoadableTypes())
            {
                if (type.BaseType == null)
                    continue;
                if (type.BaseType.Name == "XrmPlugin" && type.IsInterface == false)
                {
                    Type _type = assembly.GetType("XrmRegister.XrmPlugin");
                    object instanceOfMyType = Activator.CreateInstance(type, new object[] { null, null });
                    var tt = instanceOfMyType.GetType();

                    IList<PropertyInfo> props = new List<PropertyInfo>(tt.GetProperties());

                    var EventCollectionProp = props.Where(x => x.Name == "PluginStepCollection").FirstOrDefault();
                    var TypeNameProp = props.Where(x => x.Name == "TypeName").FirstOrDefault();

                    var EventCollectionValue = (string)EventCollectionProp.GetValue(instanceOfMyType, null);
                    var TypeNameValue = (string)TypeNameProp.GetValue(instanceOfMyType, null);

                    if (!string.IsNullOrWhiteSpace(TypeNameValue))
                    {

                        var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Collection<PluginStep>));
                        var ms = new MemoryStream(Encoding.UTF8.GetBytes(EventCollectionValue));
                        var plugin = json_ser.ReadObject(ms) as Collection<PluginStep>;
                        ms.Close();
                        config.PluginTypes.Add(new XrmPluginType { Steps = plugin, TypeName = TypeNameValue });
                    }
                }
                else if (type.BaseType.Name == "XrmWorkflow")
                {
                    Type _type = assembly.GetType("XrmRegister.XrmWorkflow");
                    object instanceOfMyType = Activator.CreateInstance(type);
                    var tt = instanceOfMyType.GetType();

                    IList<PropertyInfo> props = new List<PropertyInfo>(tt.GetProperties());

                    var WorkflowActivityConfig = props.Where(x => x.Name == "WorkflowActivityConfig").FirstOrDefault();

                    var EventCollectionValue = (string)WorkflowActivityConfig.GetValue(instanceOfMyType, null);

                    var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(WorkflowActivity));

                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(EventCollectionValue));
                    var workflow = json_ser.ReadObject(ms) as WorkflowActivity;
                    ms.Close();
                    config.WorkFlowTypes.Add(new XrmWorkflowType { Workflow = workflow });
                }
                else if (type.BaseType.Name == "XrmDataProvider" && type.IsInterface == false)
                {
                    Type _type = assembly.GetType("XrmRegister.XrmDataProvider");
                    object instanceOfMyType = Activator.CreateInstance(type, new object[] { null, null });
                    var tt = instanceOfMyType.GetType();

                    IList<PropertyInfo> props = new List<PropertyInfo>(tt.GetProperties());

                    var TypeNameProp = props.Where(x => x.Name == "TypeName").FirstOrDefault();
                    var TypeNameValue = (string)TypeNameProp.GetValue(instanceOfMyType, null);

                    if (!string.IsNullOrWhiteSpace(TypeNameValue))
                    {
                        config.DataProviderTypes.Add(new XrmDataProviderType { TypeName = TypeNameValue });
                    }
                }
                else if (type.BaseType.Name == "XrmAssemblyConfig")
                {
                    Type _type = assembly.GetType("XrmRegister.XrmAssemblyConfig");
                    object instanceOfMyType = Activator.CreateInstance(type);
                    var tt = instanceOfMyType.GetType();

                    IList<PropertyInfo> props = new List<PropertyInfo>(tt.GetProperties());

                    var EventCollectionProp = props.Where(x => x.Name == "AssemblyConfig").FirstOrDefault();

                    var EventCollectionValue = (string)EventCollectionProp.GetValue(instanceOfMyType, null);
                    var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(XrmAssemblyConfigItem));

                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(EventCollectionValue));
                    config.AssemblyConfig = json_ser.ReadObject(ms) as XrmAssemblyConfigItem;
                    ms.Close();
                }
            }

            foreach (var pluginType in config.PluginTypes)
            {
                for (int i = 0; i < pluginType.Steps.Count; i++)
                {

                    pluginType.Steps[i].TypeName = pluginType.TypeName;
                    if (pluginType.Steps[i].Images != null)
                    {
                        for (int j = 0; j < pluginType.Steps[i].Images.Count; j++)
                        {
                            pluginType.Steps[i].Images[j].TypeName = pluginType.TypeName;
                            pluginType.Steps[i].Images[j].PluginEventName = pluginType.Steps[i].Name;
                        }
                    }
                }
            }

            return config;
        }
    }

    public class XrmDataProviderType
    {
        public string TypeName { get; set; }
    }

    public class XrmPluginType
    {
        public string TypeName { get; set; }
        public Collection<PluginStep> Steps { get; set; }
    }

    public class XrmWorkflowType
    {
        public WorkflowActivity Workflow { get; set; }
    }

}
