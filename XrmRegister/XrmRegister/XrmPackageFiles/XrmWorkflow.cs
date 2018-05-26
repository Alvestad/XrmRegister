using System;
using System.Activities;
using System.Globalization;
using System.IO;

namespace XrmRegister
{
    public abstract class XrmWorkflow : CodeActivity
    {
        internal XrmWorkflow()
        {
        }
        protected override void Execute(CodeActivityContext context)
        {
            using (var xrmWorkflowContext = new XrmWorkflowContext(context))
            {
                xrmWorkflowContext.TracingService.Trace(string.Format(
                           CultureInfo.InvariantCulture,
                           "{0}.Exceute() is firing. Name:{1}, Group:{2}, NameInGroup: {3}",
                           this.workflowActiviy.TypeName,
                           this.workflowActiviy.Name,
                           this.workflowActiviy.Group,
                           this.workflowActiviy.NameInGroup));
                try
                {
                    Execute(xrmWorkflowContext);
                }
                catch (Exception ex)
                {
                    xrmWorkflowContext.TracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", ex.ToString()));
                    throw;
                }
                finally
                {
                    xrmWorkflowContext.TracingService.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", this.workflowActiviy.TypeName));
                }
            }
        }

        public abstract void Execute(XrmWorkflowContext context);

        internal WorkflowActivity workflowActiviy { get; set; }
        public string WorkflowActivityConfig
        {
            get
            {
                var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(WorkflowActivity));
                var ms = new MemoryStream();
                json_ser.WriteObject(ms, workflowActiviy);
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                return sr.ReadToEnd();
            }
        }
    }
    public class WorkflowActivity
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string NameInGroup { get; set; }
        public string TypeName { get; set; }
    }
}

