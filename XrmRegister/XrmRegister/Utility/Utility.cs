using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister.Utility
{
    public class Utility
    {
        public static Dictionary<string, EntityReference> _messages = new Dictionary<string, EntityReference>();
        public static Dictionary<string, EntityReference> _sdkmessages = new Dictionary<string, EntityReference>();
        public static Dictionary<string, EntityReference> _plugintypes = new Dictionary<string, EntityReference>();

        public static EntityReference GetMessageId(string messageName, IOrganizationService service)
        {
            if (_messages.ContainsKey(messageName))
                return _messages[messageName];

            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);

            var sdkmessage = (from sm in context.CreateQuery("sdkmessage") where (string)sm["name"] == messageName select sm).First();

            _messages.Add(sdkmessage.GetAttributeValue<string>("name"), sdkmessage.ToEntityReference());

            return sdkmessage.ToEntityReference();
        }


        public static bool Compare(Image image1, XrmImageContainer image2, string attributes)
        {
            if (image2 == null)
                return false;

            if (image1.Name != image2.Name)
                return false;

            if ((int)image1.ImageType != image2.Type)
                return false;

            var a1 = attributes;
            var a2 = image2.Attributes;

            if (string.IsNullOrWhiteSpace(a1))
                a1 = null;
            if (string.IsNullOrWhiteSpace(a2))
                a2 = null;

            if (a1 != null && a2 == null)
                return false;
            if (a1 == null && a2 != null)
                return false;

            if (a1 != null && a2 != null)
            {
                var fa1 = a1.Split(',').OrderBy(x => x).ToArray();
                var fa2 = a2.Split(',').OrderBy(x => x).ToArray();

                if (fa1.Length != fa2.Length)
                    return false;

                for (int i = 0; i < fa1.Length; i++)
                {
                    if (fa1[i] != fa2[i])
                        return false;
                }
            }

            return true;
        }
        public static bool Compare(WorkflowActivity work1, XrmWorkflowTypeContainer work2)
        {
            if (work1 == null)
                return false;

            var group1 = string.IsNullOrWhiteSpace(work1.Group) ? null : work1.Group;
            var group2 = string.IsNullOrWhiteSpace(work2.Group) ? null : work2.Group;

            var nameInGroup1 = string.IsNullOrWhiteSpace(work1.NameInGroup) ? null : work1.NameInGroup;
            var nameInGroup2 = string.IsNullOrWhiteSpace(work2.NameInGroup) ? null : work2.NameInGroup;

            if (group1 != group2)
                return false;
            if (nameInGroup1 != nameInGroup2)
                return false;

            return true;
        }

        public static bool Compare(PluginStep step1, XrmStepContainer step2, string filteringAttributes, string unsecureconfig, string secureconfig)
        {
            if (step2 == null)
                return false;

            var name1 = step2.Entity;
            var name2 = step1.EntityName;

            if (string.IsNullOrWhiteSpace(name1))
                name1 = string.Empty;
            if (string.IsNullOrWhiteSpace(name2))
                name2 = string.Empty;

            if (name1 == "none")
                name1 = string.Empty;
            if (name2 == "none")
                name2 = string.Empty;

            if (name1 != name2)
                return false;
            if (step2.Message != step1.MessageName)
                return false;
            if (step2.Rank != step1.Rank)
                return false;
            if (step2.Mode != (int)step1.StepMode)
                return false;
            if (step2.Stage != (int)step1.Stage)
                return false;

            var unsecureconfig2 = string.IsNullOrWhiteSpace(step2.UnsecureConfig) ? null : step2.UnsecureConfig;
            var unsecureconfig1 = string.IsNullOrWhiteSpace(unsecureconfig) ? null : unsecureconfig;
            if (unsecureconfig2 != unsecureconfig1)
                return false;

            var secureconfig2 = string.IsNullOrWhiteSpace(step2.SecureConfig) ? null : step2.SecureConfig;
            var secureconfig1 = string.IsNullOrWhiteSpace(secureconfig) ? null : secureconfig;
            if (secureconfig2 != secureconfig1)
                return false;

            if (filteringAttributes != null && step2.FilteringAttributes == null)
                return false;
            if (filteringAttributes == null && step2.FilteringAttributes != null)
                return false;

            if (filteringAttributes != null && step2.FilteringAttributes != null)
            {
                var fa1 = filteringAttributes.Split(',').OrderBy(x => x).ToArray();
                var fa2 = step2.FilteringAttributes.Split(',').OrderBy(x => x).ToArray();

                if (fa1.Length != fa2.Length)
                    return false;

                for (int i = 0; i < fa1.Length; i++)
                {
                    if (fa1[i] != fa2[i])
                        return false;
                }
            }

            return true;
        }

        public static bool Compare(WebHookStep step1, XrmStepContainer step2, string filteringAttributes)
        {
            if (step2 == null)
                return false;

            var name1 = step2.Entity;
            var name2 = step1.EntityName;

            if (string.IsNullOrWhiteSpace(name1))
                name1 = string.Empty;
            if (string.IsNullOrWhiteSpace(name2))
                name2 = string.Empty;

            if (name1 == "none")
                name1 = string.Empty;
            if (name2 == "none")
                name2 = string.Empty;

            if (name1 != name2)
                return false;
            if (step2.Message != step1.MessageName)
                return false;
            if (step2.Rank != step1.Rank)
                return false;
            if (step2.Mode != (int)step1.StepMode)
                return false;
            if (step2.Stage != (int)step1.Stage)
                return false;


            if (filteringAttributes != null && step2.FilteringAttributes == null)
                return false;
            if (filteringAttributes == null && step2.FilteringAttributes != null)
                return false;

            if (filteringAttributes != null && step2.FilteringAttributes != null)
            {
                var fa1 = filteringAttributes.Split(',').OrderBy(x => x).ToArray();
                var fa2 = step2.FilteringAttributes.Split(',').OrderBy(x => x).ToArray();

                if (fa1.Length != fa2.Length)
                    return false;

                for (int i = 0; i < fa1.Length; i++)
                {
                    if (fa1[i] != fa2[i])
                        return false;
                }
            }

            return true;
        }



        public static EntityReference GetSdkMessageFilterId(string entityName, Guid messageId, IOrganizationService service)
        {
            if (string.IsNullOrWhiteSpace(entityName))
                entityName = "none";
            if (_sdkmessages.ContainsKey(entityName + messageId.ToString()))
                return _sdkmessages[entityName + messageId.ToString()];

            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);
            
            var sdkmessagefilter = (from smf in context.CreateQuery("sdkmessagefilter")
                                    where (string)smf["primaryobjecttypecode"] == entityName.ToLower()
                                    && (Guid)smf["sdkmessageid"] == messageId
                                    select smf).FirstOrDefault();

            if (entityName != "none" && sdkmessagefilter == null)
                throw new Exception($"Could not find messagefilter for {entityName} on message {messageId}");
            else if (sdkmessagefilter == null)
                return null;
            else
            {
                _sdkmessages.Add(entityName + messageId.ToString(), sdkmessagefilter.ToEntityReference());
                return sdkmessagefilter.ToEntityReference();
            }
        }

        public static EntityReference GetPluginTypeId(Guid assemblyId, string PluginTypeName, IOrganizationService service)
        {
            if (_plugintypes.ContainsKey(PluginTypeName + assemblyId.ToString()))
                return _plugintypes[PluginTypeName + assemblyId.ToString()];

            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);


            var plugintype = (from pt in context.CreateQuery("plugintype")
                              where (Guid)pt["pluginassemblyid"] == assemblyId && (string)pt["name"] == PluginTypeName
                              select pt).First();

            _plugintypes.Add(PluginTypeName + assemblyId.ToString(), plugintype.ToEntityReference());

            return plugintype.ToEntityReference();
        }

        public static EntityReference GetAssembly(string assemblyName, IOrganizationService service)
        {
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);
            var assembly = (from a in context.CreateQuery("pluginassembly") where (string)a["name"] == assemblyName select a).FirstOrDefault();

            EntityReference assemblyRef = null;

            if (assembly != null)
                assemblyRef = assembly.ToEntityReference();

            return assemblyRef;
        }

        public static Dictionary<string, EntityReference> GetPluginTypes(Guid assemblyId, IOrganizationService service)
        {
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);

            var plugintype = (from pt in context.CreateQuery("plugintype")
                              where ((EntityReference)pt["pluginassemblyid"]).Id == assemblyId
                              select pt).ToList();

            var ptDictionary = new Dictionary<string, EntityReference>();
            foreach (var pt in plugintype)
                ptDictionary.Add(pt.GetAttributeValue<string>("typename") + assemblyId.ToString(), pt.ToEntityReference());

            return ptDictionary;
        }


        public static Dictionary<string, EntityReference> GetSteps(Guid assemblyId, IOrganizationService service)
        {
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);
            
            var steps = (from spt in context.CreateQuery("sdkmessageprocessingstep")
                         join pt in context.CreateQuery("plugintype") on (Guid)spt["plugintypeid"] equals pt.Id
                         where (Guid)pt["pluginassemblyid"] == assemblyId
                         select spt).ToList();

            var stepDictionary = new Dictionary<string, EntityReference>();
            foreach (var step in steps)
            {
                stepDictionary.Add(step.GetAttributeValue<string>("name") + step.GetAttributeValue<EntityReference>("plugintypeid").Id.ToString(), step.ToEntityReference());
            }

            return stepDictionary;
        }

        public static Dictionary<string, EntityReference> GetImages(Guid assemblyId, IOrganizationService service)
        {
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);

            var images = (from img in context.CreateQuery("sdkmessageprocessingstepimage")
                          join spt in context.CreateQuery("sdkmessageprocessingstep") on (Guid)img["sdkmessageprocessingstepid"] equals spt.Id
                          join pt in context.CreateQuery("plugintype") on (Guid)spt["plugintypeid"] equals pt.Id
                          where (Guid)pt["pluginassemblyid"] == assemblyId
                          select img).ToList();

            var imagedic = new Dictionary<string, EntityReference>();
            foreach (var im in images)
            {
                imagedic.Add(im.GetAttributeValue<string>("entityalias") + im.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid").Id.ToString(), im.ToEntityReference());
            }

            return imagedic;
        }

        public static Guid? FoundSolution(string uniqeName, IOrganizationService service)
        {
            if (string.IsNullOrWhiteSpace(uniqeName))
                return null;

            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);

            var sol = (from s in context.CreateQuery("solution") where (string)s["uniquename"] == uniqeName select new { s.Id }).FirstOrDefault();

            if (sol != null)
                return sol.Id;
            return null;


        }

        public static Guid? FoundSolution(string uniqeName, IOrganizationService service, out string publisherPrefix)
        {
            publisherPrefix = null;
            if (string.IsNullOrWhiteSpace(uniqeName))
                return null;

            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);

            var sol = (from s in context.CreateQuery("solution")
                       join p in context.CreateQuery("publisher") on (Guid)s["publisherid"] equals (Guid)p["publisherid"]
                       where (string)s["uniquename"] == uniqeName select new { s.Id, Prefix = (string)p["customizationprefix"] }).FirstOrDefault();

            if (sol != null)
            {
                publisherPrefix = sol.Prefix.ToLower();
                return sol.Id;
            }
            return null;


        }

        public static string FoundSolution(Guid id, IOrganizationService service)
        {
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(service);

            var sol = (from s in context.CreateQuery("solution") where s.Id == id select s).FirstOrDefault();

            if (sol != null)
                return (string)sol.GetAttributeValue<string>("uniquename");
            return null;
        }

        public static List<string> GetReportFiles(string path, string searchPattern, SearchOption searchOption)
        {
            var directories = Directory.GetDirectories(path);
            string[] searchPatterns = searchPattern.Split('|');
            List<string> files = new List<string>();
            foreach (var dir in directories)
            {
                foreach (string sp in searchPatterns)
                {
                    files.AddRange(System.IO.Directory.GetFiles(dir, sp, searchOption).Select(
                        x => x));
                }
            }

            return files.Distinct().ToList();
        }

        public static Tuple<string, string, int, string>[] GetFiles(string path, string searchPattern, string prefix, SearchOption searchOption, Dictionary<string, string> spoofList)
        {
            var directories = Directory.GetDirectories(path);
            string[] searchPatterns = searchPattern.Split('|');
            List<Tuple<string, string, int, string>> files = new List<Tuple<string, string,int,string>>();
            foreach (var dir in directories)
            {
                foreach (string sp in searchPatterns)
                {
                    files.AddRange(System.IO.Directory.GetFiles(dir, sp, searchOption).Select(
                        x => new Tuple<string, string, int, string>(
                            x,
                            spoofList.ContainsKey($"{prefix}_{x.Replace(Directory.GetCurrentDirectory(), string.Empty).Replace("\\", "/")}") ?
                            spoofList[$"{prefix}_{x.Replace(Directory.GetCurrentDirectory(), string.Empty).Replace("\\", "/")}"] :
                            $"{prefix}_{x.Replace(Directory.GetCurrentDirectory(), string.Empty).Replace("\\", "/")}",
                            WebresourceType(x),
                            x.Replace($"{Directory.GetCurrentDirectory()}\\", string.Empty)
                            )));
                }
            }

            return files.Distinct().ToArray();
        }

        public static int WebresourceType(string file)
        {
            var typevalue = -1;
            if (string.IsNullOrWhiteSpace(file))
                return typevalue;

            var fileExtension = file.Substring(file.LastIndexOf(".") + 1).ToLower();
            
            switch (fileExtension)
            {
                case "html":
                case "htm":
                    typevalue = 1;
                    break;
                case "css":
                    typevalue = 2;
                    break;
                case "js":
                    typevalue = 3;
                    break;
                case "xml":
                    typevalue = 4;
                    break;
                case "png":
                    typevalue = 5;
                    break;
                case "jpg":
                    typevalue = 6;
                    break;
                case "gif":
                    typevalue = 7;
                    break;
                case "xap":
                    typevalue = 8;
                    break;
                case "xsl":
                    typevalue = 9;
                    break;
                case "ico":
                    typevalue = 10;
                    break;
            }
            return typevalue;
        }

        public static byte[] GetBytesFromFile(string fullFilePath)
        {
            FileStream fs = File.OpenRead(fullFilePath);
            try
            {
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
                fs.Close();
                return bytes;
            }
            finally
            {
                fs.Close();
            }

        }
    }
}
