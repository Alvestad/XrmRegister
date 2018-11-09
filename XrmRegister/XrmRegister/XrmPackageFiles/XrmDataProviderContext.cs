using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace XrmRegister
{
    public class XrmDataProviderContext : IDisposable
    {
        internal IServiceProvider ServiceProvider { get; private set; }
        internal IOrganizationServiceFactory ServiceFactory { get; private set; }
        internal IOrganizationService OrganizationService { get; private set; }
        internal IPluginExecutionContext PluginExecutionContext { get; private set; }
        internal ITracingService TracingService { get; private set; }
        internal XrmDataProviderContext(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");

            this.PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            this.TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            this.OrganizationService = factory.CreateOrganizationService(this.PluginExecutionContext.UserId);
        }

        internal void Trace(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || this.TracingService == null)
            {
                return;
            }
            if (this.PluginExecutionContext == null)
            {
                this.TracingService.Trace(message);
            }
            else
            {
                this.TracingService.Trace(
                    "{0}, Correlation Id: {1}, Initiating User: {2}",
                    message,
                    this.PluginExecutionContext.CorrelationId,
                    this.PluginExecutionContext.InitiatingUserId);
            }
        }
        internal EntityReference Target
        {
            get
            {
                return this.PluginExecutionContext.GetTargetRef();
            }
        }

        internal QueryExpression Query
        {
            get
            {
                return this.PluginExecutionContext.GetQuery();
            }
        }
        public void Dispose()
        {
            this.Trace("Dispose");
        }
    }
}
