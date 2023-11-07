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

        public Collection<Image> GetWebHookImages()
        {
            var result = new Collection<Image>();
            foreach (var webhooktype in this.WebHookTypes)
                foreach (var step in webhooktype.Steps)
                    foreach (var image in step.Images)
                        result.Add(image);
            return result;
        }

        public Collection<WebHookStep> GetWebHookSteps()
        {
            var result = new Collection<WebHookStep>();
            foreach (var webhooktype in this.WebHookTypes)
                foreach (var step in webhooktype.Steps)
                    result.Add(step);
            return result;
        }

        public XrmAssemblyConfigItem AssemblyConfig { get; set; }
        public Collection<XrmPluginType> PluginTypes { get; set; }
        public Collection <XrmWorkflowType> WorkFlowTypes { get; set; }
        public Collection<XrmWebHookType> WebHookTypes { get; set; }

        public static XrmAssemblyConfiguration GetConfiguration(Assembly assembly)
        {
            var config = new XrmAssemblyConfiguration();
            config.PluginTypes = new Collection<XrmPluginType>();
            config.WorkFlowTypes = new Collection<XrmWorkflowType>();
            config.WebHookTypes = new Collection<XrmWebHookType>();

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
                else if(type.BaseType.Name == "XrmWebHook" && type.IsInterface == false)
                {
                    Type _type = assembly.GetType("XrmRegister.XrmWebHook");
                    object instanceOfMyType = Activator.CreateInstance(type, new object[] {
                       System.Configuration.ConfigurationManager.AppSettings[type.FullName]
                    });
                    var tt = instanceOfMyType.GetType();

                    //XrmWebHookType Test = (XrmWebHookType)Activator.CreateInstance(type, new object[] { });

                    IList<PropertyInfo> props = new List<PropertyInfo>(tt.GetProperties());

                    var EventCollectionProp = props.Where(x => x.Name == "WebHookStepCollection").FirstOrDefault();
                    var TypeNameProp = props.Where(x => x.Name == "TypeName").FirstOrDefault();
                    var UrlProp = props.Where(x => x.Name == "Url").FirstOrDefault();
                    var AuthProp = props.Where(x => x.Name == "Authentication").FirstOrDefault();
                    var AuthCollectionProp = props.Where(x => x.Name == "AuthValuesCollection").FirstOrDefault();
                    var WebhookKeyProp = props.Where(x => x.Name == "WebhookKeyValue").FirstOrDefault();

                    var EventCollectionValue = (string)EventCollectionProp.GetValue(instanceOfMyType, null);
                    var TypeNameValue = (string)TypeNameProp.GetValue(instanceOfMyType, null);
                    var UrlValue = (string)UrlProp.GetValue(instanceOfMyType, null);
                    var AuthType = (int)AuthProp.GetValue(instanceOfMyType, null);
                    var AuthCollectionValues = (string)AuthCollectionProp.GetValue(instanceOfMyType, null);
                    var WebhookKeyValue = (string)WebhookKeyProp.GetValue(instanceOfMyType, null);


                    if (!string.IsNullOrWhiteSpace(TypeNameValue))
                    {

                        var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Collection<WebHookStep>));
                        var ms = new MemoryStream(Encoding.UTF8.GetBytes(EventCollectionValue));
                        var webhook = json_ser.ReadObject(ms) as Collection<WebHookStep>;

                        var json_ser_AuthValues = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Collection<AuthValue>));
                        var ms_Authvalues = new MemoryStream(Encoding.UTF8.GetBytes(AuthCollectionValues));
                        var authvalues = json_ser_AuthValues.ReadObject(ms_Authvalues) as Collection<AuthValue>;

                        ms.Close();
                        config.WebHookTypes.Add(new XrmWebHookType { Steps = webhook, TypeName = TypeNameValue, Url = UrlValue, AuthType = AuthType, AuthValues = authvalues, WebhookKeyValue = WebhookKeyValue });
                    }

                }
                else if(type.BaseType.Name == "XrmWorkflow")
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

            foreach (var webhookType in config.WebHookTypes)
            {
                for (int i = 0; i < webhookType.Steps.Count; i++)
                {

                    webhookType.Steps[i].TypeName = webhookType.TypeName;
                    if (webhookType.Steps[i].Images != null)
                    {
                        for (int j = 0; j < webhookType.Steps[i].Images.Count; j++)
                        {
                            webhookType.Steps[i].Images[j].TypeName = webhookType.TypeName;
                            webhookType.Steps[i].Images[j].PluginEventName = webhookType.Steps[i].Name;
                        }
                    }
                }
            }

            return config;
        }
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

    public class XrmWebHookType
    {
        public string TypeName { get; set; }
        public string Url { get; set; }
        public int AuthType { get; set; }
        public string WebhookKeyValue { get; set; }
        public Collection<WebHookStep> Steps { get; set; }
        public Collection<AuthValue> AuthValues { get; set; }

    }

}
