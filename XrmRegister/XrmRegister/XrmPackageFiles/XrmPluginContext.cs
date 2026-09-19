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
        internal IServiceProvider ServiceProvider { get; private set; }
        internal IOrganizationServiceFactory ServiceFactory { get; private set; }
        internal IOrganizationService OrganizationService { get; private set; }
        internal IPluginExecutionContext PluginExecutionContext { get; private set; }
        internal ITracingService TracingService { get; private set; }
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

        internal IOrganizationService OrganizationServiceImpersonate(Guid userId)
        {
            return this.ServiceFactory.CreateOrganizationService(userId);
        }

        internal Entity Target
        {
            get
            {
                return this.PluginExecutionContext.GetTarget<Entity>();
            }
        }

        /// <summary>
        /// The record on messages without Target, like SetStateDynamicEntity
        /// </summary>
        internal EntityReference EntityMoniker
        {
            get
            {
                return this.GetInputParameter<EntityReference>("EntityMoniker");
            }
        }

        internal OptionSetValue State
        {
            get
            {
                return this.GetInputParameter<OptionSetValue>("State");
            }
        }

        internal OptionSetValue Status
        {
            get
            {
                return this.GetInputParameter<OptionSetValue>("Status");
            }
        }

        internal T GetInputParameter<T>(string name)
        {
            if (this.PluginExecutionContext == null)
                return default(T);
            if (this.PluginExecutionContext.InputParameters == null)
                return default(T);
            if (!this.PluginExecutionContext.InputParameters.Contains(name))
                return default(T);

            var value = this.PluginExecutionContext.InputParameters[name];
            return value is T ? (T)value : default(T);
        }

        internal Entity PreImage
        {
            get
            {
                return this.PluginExecutionContext.GetPreImage(null);
            }
        }
        internal Entity PostImage
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
