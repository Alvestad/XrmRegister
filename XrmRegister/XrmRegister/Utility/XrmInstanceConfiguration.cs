using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister.Utility
{
    public class XrmInstanceConfiguration
    {
        public Collection<XrmPluginTypeContainer> PluginTypes { get; set; } = new Collection<XrmPluginTypeContainer>();

        public Collection<XrmWorkflowTypeContainer> WorkflowTypes { get; set; } = new Collection<XrmWorkflowTypeContainer>();

        public EntityReference AssemblyRef { get; set; }
        public Collection<XrmImageContainer> GetImages()
        {
            var result = new Collection<XrmImageContainer>();
            foreach (var plugintype in this.PluginTypes)
                foreach (var step in plugintype.Steps)
                    foreach (var image in step.Images)
                        result.Add(image);
            return result;
        }

        public Collection<XrmStepContainer> GetSteps()
        {
            var result = new Collection<XrmStepContainer>();
            foreach (var plugintype in this.PluginTypes)
                foreach (var step in plugintype.Steps)
                    result.Add(step);
            return result;
        }


        public static XrmInstanceConfiguration GetPluginTypesHiearki(string assemblyName, IOrganizationService service)
        {
            var ass = GetAssembly(assemblyName, service);
            if (ass == null)
                return new XrmInstanceConfiguration();

            return GetPluginTypesHiearki(ass, service);
        }

        public static XrmInstanceConfiguration GetWorkflowTypes(string assemblyName, IOrganizationService service)
        {
            var ass = GetAssembly(assemblyName, service);
            if (ass == null)
                return new XrmInstanceConfiguration();

            return GetWorkflowTypes(ass, service);
        }

        public static XrmInstanceConfiguration GetWorkflowTypes(EntityReference assemblyRef, IOrganizationService service)
        {
            var assemblyId = assemblyRef.Id;

            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);

            var plugintype = (from pt in context.CreateQuery("plugintype")
                              where (Guid)pt["pluginassemblyid"] == assemblyId
                              select pt).ToList();

            var ptdic = new Collection<XrmWorkflowTypeContainer>();
            foreach (var pt in plugintype)
            {
                var workflowTypeContainer = new XrmWorkflowTypeContainer {
                    Name = pt.GetAttributeValue<string>("typename"),
                    Id = pt.Id,
                    Group = pt.GetAttributeValue<string>("workflowactivitygroupname"),
                    NameInGroup = pt.GetAttributeValue<string>("name")
                };
                ptdic.Add(workflowTypeContainer);
            }

            return new XrmInstanceConfiguration { WorkflowTypes = ptdic, AssemblyRef = assemblyRef };

        }

        public static EntityReference GetAssembly(string assemblyName, IOrganizationService service)
        {
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);
            var ass = (from a in context.CreateQuery("pluginassembly") where (string)a["name"] == assemblyName select a).FirstOrDefault();

            EntityReference assRef = null;

            if (ass != null)
                assRef = ass.ToEntityReference();

            return assRef;
        }
        public static XrmInstanceConfiguration GetPluginTypesHiearki(EntityReference assemblyRef, IOrganizationService service)
        {
            var assemblyId = assemblyRef.Id;

            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);
            var plugintype = (from pt in context.CreateQuery("plugintype")
                              where (Guid)pt["pluginassemblyid"] == assemblyId
                              select pt).ToList();

          
            var steps1 = (from spt in context.CreateQuery("sdkmessageprocessingstep")
                          join pt in context.CreateQuery("plugintype") on (Guid)spt["plugintypeid"] equals (Guid)pt["plugintypeid"]
                          where (Guid)pt["pluginassemblyid"] == assemblyId && (EntityReference)spt["sdkmessagefilterid"] == null
                          select new { Step = spt, EntityName = string.Empty }).ToList();

            var steps2 = (from spt in context.CreateQuery("sdkmessageprocessingstep")
                          join m in context.CreateQuery("sdkmessagefilter") on (Guid)spt["sdkmessagefilterid"] equals (Guid)m["sdkmessagefilterid"]
                          join pt in context.CreateQuery("plugintype") on (Guid)spt["plugintypeid"] equals (Guid)pt["plugintypeid"]
                          where (Guid)pt["pluginassemblyid"] == assemblyId
                          select new { Step = spt, EntityName = m.GetAttributeValue<string>("primaryobjecttypecode") }).ToList();

            var steps = steps1.Union(steps2).ToList();



            var images = (from img in context.CreateQuery("sdkmessageprocessingstepimage")
                          join spt in context.CreateQuery("sdkmessageprocessingstep") on (Guid)img["sdkmessageprocessingstepid"] equals (Guid)spt["sdkmessageprocessingstepid"]
                          join pt in context.CreateQuery("plugintype") on (Guid)spt["plugintypeid"] equals (Guid)pt["plugintypeid"]
                          where (Guid)pt["pluginassemblyid"] == assemblyId
                          select img).ToList();

            var secureConfigs = (from spt in context.CreateQuery("sdkmessageprocessingstep")
                                join pt in context.CreateQuery("plugintype") on (Guid)spt["plugintypeid"] equals (Guid)pt["plugintypeid"]
                                join sc in context.CreateQuery("sdkmessageprocessingstepsecureconfig") on (Guid)spt["sdkmessageprocessingstepsecureconfigid"] equals (Guid)sc["sdkmessageprocessingstepsecureconfigid"]
                                where (Guid)pt["pluginassemblyid"] == assemblyId
                                select new
                                {
                                    Step = new {
                                        Id = (Guid)spt["sdkmessageprocessingstepid"],
                                        SecureConfig = (string)sc["secureconfig"],
                                        SecureConfigId = (Guid)sc["sdkmessageprocessingstepsecureconfigid"]
                                    }
                                }).ToList();


            var ptdic = new Collection<XrmPluginTypeContainer>();

            foreach (var pt in plugintype)
            {
                var pluginTypeContainer = new XrmPluginTypeContainer { Name = pt.GetAttributeValue<string>("typename"), Id = pt.Id, Steps = new Collection<XrmStepContainer>() };

                var pluginsteps = steps.Where(x => x.Step.GetAttributeValue<EntityReference>("plugintypeid").Id == pt.Id).ToList();
                foreach (var step in pluginsteps)
                {
                    var secureConfig = secureConfigs.Where(x => x.Step.Id == step.Step.Id).FirstOrDefault();

                    var stepContainer = new XrmStepContainer {
                        Name = step.Step.GetAttributeValue<string>("name"),
                        Id = step.Step.Id, Images = new Collection<XrmImageContainer>(),
                        XrmPluginTypeName = pt.GetAttributeValue<string>("name"),
                        Entity = step.EntityName,
                        Message = step.Step.GetAttributeValue<EntityReference>("sdkmessageid").Name,
                        FilteringAttributes = step.Step.GetAttributeValue<string>("filteringattributes"),
                        Rank = step.Step.GetAttributeValue<int?>("rank").Value,
                        Mode = step.Step.GetAttributeValue<OptionSetValue>("mode").Value,
                        Stage = step.Step.GetAttributeValue<OptionSetValue>("stage").Value,
                        UnsecureConfig = step.Step.GetAttributeValue<string>("configuration"),
                        SecureConfig = secureConfig != null ? secureConfig.Step.SecureConfig : null,
                        SecureConfigId = secureConfig != null ? (Guid?)secureConfig.Step.SecureConfigId : null
                    };


                    var stepimages = images.Where(x => x.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid").Id == step.Step.Id).ToList();
                    foreach (var image in stepimages)
                    {
                        var imageContainer = new XrmImageContainer {
                            Id = image.Id,
                            Name = image.GetAttributeValue<string>("name"),
                            Type = image.GetAttributeValue<OptionSetValue>("imagetype").Value,
                            Attributes = image.GetAttributeValue<string>("attributes"),
                            XrmPluginTypeName = pt.GetAttributeValue<string>("name"),
                            XrmStepContainerName = step.Step.GetAttributeValue<string>("name"),
                            
                        };
                        stepContainer.Images.Add(imageContainer);

                    }
                    pluginTypeContainer.Steps.Add(stepContainer);
                }
                ptdic.Add(pluginTypeContainer);
            }

            return new XrmInstanceConfiguration { PluginTypes = ptdic, AssemblyRef = assemblyRef };
        }
    }

    public class XrmWorkflowTypeContainer
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string NameInGroup { get; set; }
    }

    public class XrmPluginTypeContainer
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Collection<XrmStepContainer> Steps { get; set; } = new Collection<XrmStepContainer>();
    }

    public class XrmStepContainer
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Collection<XrmImageContainer> Images { get; set; } = new Collection<XrmImageContainer>();
        public string XrmPluginTypeName { get; set; }
        public int Rank { get; set; }
        public string Message { get; set; }
        public string Entity { get; set; }
        public string FilteringAttributes { get; set; }
        public int Mode { get; set; }
        public int Stage { get; set; }
        public string UnsecureConfig { get; set; }
        public string SecureConfig { get; set; }
        public Guid? SecureConfigId { get; set; }
    }

    public class XrmImageContainer
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public string Attributes { get; set; }
        public string XrmStepContainerName { get; set; }
        public string XrmPluginTypeName { get; set; }
    }
}
