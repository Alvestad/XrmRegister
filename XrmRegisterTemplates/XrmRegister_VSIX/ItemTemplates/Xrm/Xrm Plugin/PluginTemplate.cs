using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XrmRegister;

namespace $rootnamespace$
{
    public class $fileinputname$ : XrmBasePlugin
    {
        public $fileinputname$() : base(null, null) { }
        public $fileinputname$(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            base.TypeName = this.GetType().FullName;

            base.RegisteredSteps.Add(new PluginStep()
            {
                Name = "$safeitemname$",
                Rank = 1,
                StepMode = StepMode.Synchronous,
                Stage = StepStage.PostOperation,
                MessageName = XrmMessages.Create,
                Action = ExecutePluginContext,
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

        public void ExecutePluginContext(IServiceProvider serviceProvider)
        {
            try
            {
                using (var localContext = new CrmPluginContext(serviceProvider))
                {

                }

            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }
    }
}
