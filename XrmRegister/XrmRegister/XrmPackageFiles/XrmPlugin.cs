using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister
{
    public abstract class XrmPlugin : IPlugin
    {
        protected XrmPlugin(string unsecureConfig, string secureConfig)
        {
            this.UnsecureConfig = unsecureConfig;
            this.SecureConfig = secureConfig;
        }

        private Collection<PluginStep> registeredSteps;
        protected Collection<PluginStep> RegisteredSteps
        {
            get
            {
                if (this.registeredSteps == null)
                    this.registeredSteps = new Collection<PluginStep>();
                return this.registeredSteps;
            }
        }
        public string UnsecureConfig { get; private set; }
        public string SecureConfig { get; private set; }

        public string TypeName { get; set; }
        public string PluginStepCollection
        {
            get
            {
                var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Collection<PluginStep>));
                var ms = new MemoryStream();
                json_ser.WriteObject(ms, RegisteredSteps);

                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                return sr.ReadToEnd();
            }
        }
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");

            using (var xrmPluginContext = new XrmPluginContext(serviceProvider))
            {
                var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", this.TypeName));
                try
                {
                    var actionToInvoke = this.RegisteredSteps.Where(
                        x => (int)x.Stage == xrmPluginContext.PluginExecutionContext.Stage
                        && x.MessageName.ToLowerInvariant() == xrmPluginContext.PluginExecutionContext.MessageName.ToLowerInvariant()
                        && (string.IsNullOrWhiteSpace(x.EntityName) ? true : x.EntityName.ToLowerInvariant() == xrmPluginContext.PluginExecutionContext.PrimaryEntityName.ToLowerInvariant())
                        ).Select(x => x.ActionToInvoke).FirstOrDefault();

                    if (actionToInvoke != null)
                        actionToInvoke.Invoke(xrmPluginContext);

                }
                catch (Exception ex)
                {
                    tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", ex.ToString()));
                    throw;
                }
                finally
                {
                    tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", this.TypeName));
                }
            }
        }
    }
    #region PluginStep

    [System.Runtime.Serialization.DataContract]
    public partial class PluginStep
    {
        [System.Runtime.Serialization.DataMember]
        public string Name { get; set; }
        [System.Runtime.Serialization.DataMember]
        public StepStage Stage { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string EntityName { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string MessageName { get; set; }
        public Action<XrmPluginContext> ActionToInvoke { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string[] FilteringAttributes { get; set; }
        [System.Runtime.Serialization.DataMember]
        public Collection<Image> Images { get; set; } = new Collection<Image>();
        [System.Runtime.Serialization.DataMember]
        public AttributeMode FilteredAttributeMode { get; set; }
        [System.Runtime.Serialization.DataMember]
        public int Rank { get; set; }
        [System.Runtime.Serialization.DataMember]
        public StepMode StepMode { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string Description { get; set; }
    }
    #endregion
}
