using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister
{
    public class XrmWorkflowContext : IDisposable
    {
        public CodeActivityContext CodeActivityContext { get; private set; }
        public IWorkflowContext WorkflowContext { get; private set; }
        public IOrganizationServiceFactory ServiceFactory { get; private set; }
        public IOrganizationService OrganizationService { get; private set; }
        public ITracingService TracingService { get; private set; }
        internal XrmWorkflowContext(CodeActivityContext codeActivityContext)
        {
            if (codeActivityContext == null)
                throw new ArgumentNullException("codeActivityContext");

            this.CodeActivityContext = codeActivityContext;

            this.WorkflowContext = codeActivityContext.GetExtension<IWorkflowContext>();

            this.TracingService = codeActivityContext.GetExtension<ITracingService>();

            this.ServiceFactory = codeActivityContext.GetExtension<IOrganizationServiceFactory>();

            this.OrganizationService = this.ServiceFactory.CreateOrganizationService(this.WorkflowContext.UserId);
        }
        internal void Trace(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || this.TracingService == null)
            {
                return;
            }
            if (this.WorkflowContext == null)
            {
                this.TracingService.Trace(message);
            }
            else
            {
                this.TracingService.Trace(
                    "{0}, Correlation Id: {1}, Initiating User: {2}",
                    message,
                    this.WorkflowContext.CorrelationId,
                    this.WorkflowContext.InitiatingUserId);
            }
        }
        public void Dispose()
        {
            this.Trace("Dispose");
        }
    }
}
