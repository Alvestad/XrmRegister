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
    public class XrmPlugin : IPlugin
    {
        internal XrmPlugin(string unsecureConfig, string secureConfig)
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

    //public enum StepStage
    //{
    //    PreValidation = 10,
    //    PreOperation = 20,
    //    PostOperation = 40
    //}

    //public enum StepDeployment
    //{
    //    ServerOnly = 0,
    //    OfflineOnly = 1,
    //    Both = 2
    //}

    //public enum StepMode
    //{
    //    Asynchronous = 1,
    //    Synchronous = 0
    //}

    //public enum StepInvocationSource
    //{
    //    Parent = 0,
    //    Child = 1
    //}

    //public enum ImageType
    //{
    //    PreImage = 0,
    //    PostImage = 1,
    //    Both = 2
    //}

    //public enum AttributeMode
    //{
    //    Include = 0,
    //    Exclude = 1
    //}

    //[System.Runtime.Serialization.DataContract]
    //public partial class Image
    //{
    //    public Image(params string[] attributes)
    //    {
    //        Attributes = attributes;
    //    }

    //    [System.Runtime.Serialization.DataMember]
    //    public ImageType ImageType { get; set; }

    //    [System.Runtime.Serialization.DataMember]
    //    public string Name { get; set; }
    //    [System.Runtime.Serialization.DataMember]
    //    public string[] Attributes { get; set; }

    //    [System.Runtime.Serialization.DataMember]
    //    public AttributeMode AttributeMode { get; set; }

    //}

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
    }
    #endregion
}
