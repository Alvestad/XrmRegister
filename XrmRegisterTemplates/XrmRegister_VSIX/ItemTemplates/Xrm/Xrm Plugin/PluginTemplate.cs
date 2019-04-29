using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrmRegister;

namespace $rootnamespace$
{
    public class $fileinputname$ : XrmPlugin
    {
        public $fileinputname$() : base(null, null) { }
        public $fileinputname$(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            
            base.TypeName = this.GetType().FullName;
            //Note: Muliple steps with same stage, message and entityname is not supported whitin the same xrmplugin class
            base.RegisteredSteps.Add(new PluginStep()
            {
                Name = "$safeitemname$",
                Rank = 1,
                StepMode = StepMode.Synchronous,
                Stage = StepStage.PostOperation,
                MessageName = XrmMessages.Create,
                ActionToInvoke = Execute,
                EntityName = null,
                FilteredAttributeMode = AttributeMode.Include,
                FilteringAttributes = new string[]
                {
                },
                Images = new System.Collections.ObjectModel.Collection<Image>()
                {
                }
            });
        }

        public void Execute(XrmPluginContext xrmPluginContext)
        {

        }
    }
}
