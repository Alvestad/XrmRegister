using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrmRegister;

namespace $rootnamespace$
{
    public class $fileinputname$ : XrmWorkflow
    {
        //[Input("Account")]
        //[ReferenceTarget("account")]
        //[RequiredArgument]
        //public InArgument<EntityReference> _account { get; set; }

        //[Output("NoOfContacts")]
        //public OutArgument<int> Tull { get; set; }

        public $fileinputname$()
        {
            base.workflowActiviy = new WorkflowActivity
            {
                TypeName = this.GetType().FullName,
                Name = this.GetType().FullName,
                Group = "",
                NameInGroup = ""
            };
        }

        public override void Execute(XrmWorkflowContext context)
        {
           
        }
    }
}
