using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister
{
    public class XrmPluginContext : IDisposable
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IOrganizationServiceFactory ServiceFactory { get; private set; }
        public IOrganizationService OrganizationService { get; private set; }
        public IPluginExecutionContext PluginExecutionContext { get; private set; }
        public ITracingService TracingService { get; private set; }
        internal XrmPluginContext(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");

            this.PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            this.TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            this.ServiceProvider = serviceProvider;

            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            this.ServiceFactory = factory;

            this.OrganizationService = factory.CreateOrganizationService(this.PluginExecutionContext.UserId);
        }

        public void Trace(string message)
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

        public IOrganizationService OrganizationServiceImpersonate(Guid userId)
        {
            return this.ServiceFactory.CreateOrganizationService(userId);
        }

        public Entity Target
        {
            get
            {
                return this.PluginExecutionContext.GetTarget<Entity>();
            }
        }

        public Entity PreImage
        {
            get
            {
                return this.PluginExecutionContext.GetPreImage(null);
            }
        }
        public Entity PostImage
        {
            get
            {
                return this.PluginExecutionContext.GetPostImage(null);
            }
        }
        public void Dispose()
        {
            this.Trace("Dispose");
        }
    }
}
