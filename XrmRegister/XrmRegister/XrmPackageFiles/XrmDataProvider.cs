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
    public class XrmDataProvider : IPlugin
    {
        internal XrmDataProvider(string unsecureConfig, string secureConfig)
        {
            this.UnsecureConfig = unsecureConfig;
            this.SecureConfig = secureConfig;
        }

        protected DataProvider DataProvider { get; set; }
        public string UnsecureConfig { get; private set; }
        public string SecureConfig { get; private set; }

        public string TypeName { get; set; }
        
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");

            using (var xrmDataProviderContext = new XrmDataProviderContext(serviceProvider))
            {
                var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", this.TypeName));
                try
                {
                    if(this.DataProvider != null)
                    {
                        if (xrmDataProviderContext.PluginExecutionContext.MessageName.ToLowerInvariant() == XrmMessages.Retrieve.ToLowerInvariant())
                        {
                            tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.{1}()", this.TypeName, XrmMessages.Retrieve));
                            if (this.DataProvider.Retrieve_ActionToInvoke != null)
                                this.DataProvider.Retrieve_ActionToInvoke.Invoke(xrmDataProviderContext);
                        }
                        if (xrmDataProviderContext.PluginExecutionContext.MessageName.ToLowerInvariant() == XrmMessages.RetrieveMultiple.ToLowerInvariant())
                        {
                            tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.{1}()", this.TypeName, XrmMessages.RetrieveMultiple));
                            if (this.DataProvider.RetrieveMultiple_ActionToInvoke != null)
                                this.DataProvider.RetrieveMultiple_ActionToInvoke.Invoke(xrmDataProviderContext);
                        }
                        if (xrmDataProviderContext.PluginExecutionContext.MessageName.ToLowerInvariant() == XrmMessages.Create.ToLowerInvariant())
                        {
                            tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.{1}()", this.TypeName, XrmMessages.Create));
                            if (this.DataProvider.Create_ActionToInvoke != null)
                                this.DataProvider.Create_ActionToInvoke.Invoke(xrmDataProviderContext);
                        }
                        if (xrmDataProviderContext.PluginExecutionContext.MessageName.ToLowerInvariant() == XrmMessages.Update.ToLowerInvariant())
                        {
                            tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.{1}()", this.TypeName, XrmMessages.Update));
                            if (this.DataProvider.Update_ActionToInvoke != null)
                                this.DataProvider.Update_ActionToInvoke.Invoke(xrmDataProviderContext);
                        }
                        if (xrmDataProviderContext.PluginExecutionContext.MessageName.ToLowerInvariant() == XrmMessages.Delete.ToLowerInvariant())
                        {
                            tracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.{1}()", this.TypeName, XrmMessages.Delete));
                            if (this.DataProvider.Delete_ActionToInvoke != null)
                                this.DataProvider.Delete_ActionToInvoke.Invoke(xrmDataProviderContext);
                        }

                    }
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
    #region DataProvider

    [System.Runtime.Serialization.DataContract]
    public partial class DataProvider
    {
        public Action<XrmDataProviderContext> Retrieve_ActionToInvoke { get; set; }
        public Action<XrmDataProviderContext> RetrieveMultiple_ActionToInvoke { get; set; }
        public Action<XrmDataProviderContext> Create_ActionToInvoke { get; set; }
        public Action<XrmDataProviderContext> Update_ActionToInvoke { get; set; }
        public Action<XrmDataProviderContext> Delete_ActionToInvoke { get; set; }
    }
    #endregion
}
