using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using XrmRegister.Utility;

namespace XrmRegister
{
    public class XrmRegister
    {
        public event Message ShowMessage;
        public delegate void Message(string message);

        private List<Plugin> pluginUnsecureConfig = new List<Plugin>();

        public XrmRegister()
        {

        }

        private void Log(string message)
        {
            if (ShowMessage != null)
                ShowMessage.Invoke(message);
        }

        public Tuple<string, string> GetSecureUnsecureConfiguration(string typeName, string stepName)
        {
            if (string.IsNullOrWhiteSpace(stepName))
                return new Tuple<string, string>(null, null);

            var item = pluginUnsecureConfig.Where(x => x.TypeName.ToLower() == typeName.ToLower() && x.StepName.ToLower() == stepName.ToLower()).FirstOrDefault();
            if (item != null)
            {
                return new Tuple<string, string>(item.UnsecureConfig, item.SecureConfig);
            }
            else
            {
                var orderList = pluginUnsecureConfig.Where(x => x.StepName == "*").OrderByDescending(x => x.NameSpaceRank);
                foreach (var _item in orderList)
                {
                    if (typeName.StartsWith(_item.TypeName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return new Tuple<string, string>(_item.UnsecureConfig, _item.SecureConfig);
                    }
                }
            }
            return new Tuple<string, string>(null, null); ;
        }

        public void RegisterReports(string connectionString, string solutionName, List<XrmReportConfig> reportConfig, bool contentCompare)
        {
            RegisterReports(connectionString, solutionName, reportConfig, contentCompare, Guid.Empty);
        }
        public void RegisterReports(string connectionString, string solutionName, List<XrmReportConfig> reportConfig, bool contentCompare, Guid @namespace)
        {
            Log($"Begin to register");
            Log($"Connection: {connectionString}");
            Log($"Solution: {solutionName}");

            var dnsNamespace = @namespace;
            if (dnsNamespace == Guid.Empty)
                dnsNamespace = GuidUtility.DnsNamespace;


            var client = Connection.CrmConnection.GetClientByConnectionString(connectionString);
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(client);

            var solutionId = Utility.Utility.FoundSolution(solutionName, client);

            if (solutionId.HasValue)
                Log($"Found solution with name {solutionName}");
            else
            {
                Log("Did not find solution, aborting");
                return;
            }

            var filetypes = "*.rdl";
            var files = Utility.Utility.GetReportFiles($"{Directory.GetCurrentDirectory()}", filetypes, System.IO.SearchOption.AllDirectories);
            Log($"Fetched reports from Project, count {files.Count}");

            var reports = Enumerable.Repeat(new { Id = Guid.Empty, Name = string.Empty, FileName = string.Empty, Body = string.Empty }, 0).ToList();

            if (contentCompare)
            {
                reports = (from sc in context.CreateQuery("solutioncomponent")
                           join r in context.CreateQuery("report") on (Guid)sc["objectid"] equals (Guid)r["reportid"]
                           where (int)sc["componenttype"] == 31 && (Guid)sc["solutionid"] == solutionId
                           select new
                           {
                               Id = (Guid)r["reportid"],
                               Name = (string)r["name"],
                               FileName = (string)r["filename"],
                               Body = (string)r["bodytext"]
                           }).ToList();
            }
            else
            {
                reports = (from sc in context.CreateQuery("solutioncomponent")
                           join r in context.CreateQuery("report") on (Guid)sc["objectid"] equals (Guid)r["reportid"]
                           where (int)sc["componenttype"] == 31 && (Guid)sc["solutionid"] == solutionId
                           select new
                           {
                               Id = (Guid)r["reportid"],
                               Name = (string)r["name"],
                               FileName = (string)r["filename"],
                               Body = string.Empty
                           }).ToList();
            }

            var reportEntities = (from sc in context.CreateQuery("solutioncomponent")
                                  join r in context.CreateQuery("report") on (Guid)sc["objectid"] equals (Guid)r["reportid"]
                                  join re in context.CreateQuery("reportentity") on (Guid)r["reportid"] equals (Guid)re["reportid"]
                                  where (int)sc["componenttype"] == 31 && (Guid)sc["solutionid"] == solutionId
                                  select new
                                  {
                                      Id = (Guid)r["reportid"],
                                      ReportEntityId = (Guid)re["reportentityid"],
                                      Entity = (string)re["objecttypecode"]
                                  }).ToList();

            var reportVisiblity = (from sc in context.CreateQuery("solutioncomponent")
                                   join r in context.CreateQuery("report") on (Guid)sc["objectid"] equals (Guid)r["reportid"]
                                   join rv in context.CreateQuery("reportvisibility") on (Guid)r["reportid"] equals (Guid)rv["reportid"]
                                   where (int)sc["componenttype"] == 31 && (Guid)sc["solutionid"] == solutionId
                                   select new
                                   {
                                       Id = (Guid)r["reportid"],
                                       ReportVisibilityId = (Guid)rv["reportvisibilityid"],
                                       Visibility = ((OptionSetValue)rv["visibilitycode"]).Value
                                   }).ToList();

            Log($"Fetched reports from CRM, count {reports.Count}");

            var ReportTypeCode = new
            {
                ReportingServicesReport = 1,
                OtherReport = 2,
                LinkedReport = 3
            };
            var ReportCategoryCode = new
            {
                SalesReports = 1,
                ServiceReports = 2,
                MarketingReports = 3,
                AdministrativeReports = 4
            };

            foreach (var file in files)
            {
                var _fileName = file.Substring(file.LastIndexOf("\\") + 1);
                var rc = reportConfig.Where(x => x.ReportFileName == _fileName).ToList();
                if (rc.Count == 0)
                    continue;

                var reportFileName = file.Substring(file.LastIndexOf("\\") + 1);

                var existingReport = reports.Where(x => x.FileName == $"{reportFileName}").FirstOrDefault();
                var rep = new Entity("report");
                var bodytext = File.ReadAllText(file);
                rep.Attributes.Add("bodytext", bodytext);

                if (string.IsNullOrWhiteSpace(rc.First().ReportName))
                    rep.Attributes.Add("name", $"{reportFileName.Replace(".rdl", string.Empty)}");
                else
                    rep.Attributes.Add("name", rc.First().ReportName);

                rep.Attributes.Add("filename", $"{reportFileName}");
                rep.Attributes.Add("languagecode", rc.First().LanguageCode);

                var reportId = Guid.Empty;

                if (existingReport != null)
                {
                    var skipReport = false;
                    rep.Attributes.Add("reportid", existingReport.Id);
                    reportId = existingReport.Id;
                    if (contentCompare)
                    {
                        var _bodytext = bodytext.Replace("\r", string.Empty);
                        if (existingReport.Body == _bodytext)
                        {
                            Log($"Skipping report {reportFileName}");
                            skipReport = true;
                        }
                    }
                    if (!skipReport)
                    {
                        var ur = new UpdateRequest { Target = rep };
                        var urr = (UpdateResponse)client.Execute(ur);
                        Log($"Updated report {reportFileName}");
                    }
                }
                else
                {
                    rep.Attributes.Add("reportid", GuidUtility.Create(dnsNamespace, file));
                    rep.Attributes.Add("reporttypecode", new OptionSetValue(ReportTypeCode.ReportingServicesReport));
                    rep.Attributes.Add("ispersonal", false);

                    var cr = new CreateRequest { Target = rep };
                    cr.Parameters.Add("SolutionUniqueName", solutionName);
                    var crr = (CreateResponse)client.Execute(cr);

                    Log($"Created report {reportFileName}");

                    reportId = GuidUtility.Create(dnsNamespace, file);
                }

                foreach (var r in rc)
                {
                    var existingReportEntity = reportEntities.Where(x => x.Id == reportId).ToList();
                    var entityLogicalNames = string.IsNullOrWhiteSpace(r.EntityLogicalName) ? new string[] { } : r.EntityLogicalName.Split(',');

                    if (!string.IsNullOrWhiteSpace(r.EntityLogicalName))
                    {
                        foreach (var entityLogicalName in entityLogicalNames)
                        {
                            if (existingReportEntity.Count == 0)
                            {
                                var re = new Entity("reportentity");
                                re.Attributes.Add("reportid", new EntityReference("report", reportId));
                                re.Attributes.Add("objecttypecode", r.EntityLogicalName);
                                var cr = new CreateRequest { Target = re };
                                cr.Parameters.Add("SolutionUniqueName", solutionName);
                                var crr = (CreateResponse)client.Execute(cr);
                                Log($"Connected report {_fileName} to entity {r.EntityLogicalName}");
                            }
                            else
                            {


                                var existingReportEntityLogicalName = existingReportEntity.Where(x => x.Entity == entityLogicalName).FirstOrDefault();
                                if (existingReportEntity == null)
                                {
                                    var re = new Entity("reportentity");
                                    re.Attributes.Add("reportid", new EntityReference("report", reportId));
                                    re.Attributes.Add("objecttypecode", entityLogicalName);
                                    var cr = new CreateRequest { Target = re };
                                    cr.Parameters.Add("SolutionUniqueName", solutionName);
                                    var crr = (CreateResponse)client.Execute(cr);
                                    Log($"Connected report {_fileName} to entity {r.EntityLogicalName}");
                                }
                            }
                        }
                    }

                    var toDeleteReportEnitity = (from er in existingReportEntity
                                                 join ex in entityLogicalNames on er.Entity equals ex into _ex
                                                 from ex in _ex.DefaultIfEmpty()
                                                 where ex == null
                                                 select er).ToList();

                    foreach (var toDelRE in toDeleteReportEnitity)
                    {
                        client.Delete("reportentity", toDelRE.ReportEntityId);
                        Log($"Disconnected report {_fileName} from entity {toDelRE.Entity}");
                    }


                    var existingReportVisiblity = reportVisiblity.Where(x => x.Id == reportId).ToList();
                    foreach (var rv in r.ReportVisibilityCodes)
                    {
                        if (existingReportVisiblity.Count == 0)
                        {
                            var re = new Entity("reportvisibility");
                            re.Attributes.Add("reportid", new EntityReference("report", reportId));
                            re.Attributes.Add("visibilitycode", new OptionSetValue((int)rv));
                            var cr = new CreateRequest { Target = re };
                            cr.Parameters.Add("SolutionUniqueName", solutionName);
                            var crr = (CreateResponse)client.Execute(cr);
                            Log($"Added visiblity {rv.ToString()} to report {_fileName}");
                        }
                        else
                        {
                            var existingReportVisiblityCode = existingReportVisiblity.Where(x => x.Visibility == (int)rv).FirstOrDefault();
                            if (existingReportVisiblityCode == null)
                            {
                                var re = new Entity("reportvisibility");
                                re.Attributes.Add("reportid", new EntityReference("report", reportId));
                                re.Attributes.Add("visibilitycode", new OptionSetValue((int)rv));
                                var cr = new CreateRequest { Target = re };
                                cr.Parameters.Add("SolutionUniqueName", solutionName);
                                var crr = (CreateResponse)client.Execute(cr);
                                Log($"Added visiblity {rv.ToString()} to report {_fileName}");
                            }
                        }
                    }

                    var toDeleteReportVisibility = (from erv in existingReportVisiblity
                                                    join rvv in r.ReportVisibilityCodes on erv.Visibility equals (int)rvv into _rvv
                                                    from rvv in _rvv.DefaultIfEmpty()
                                                    where rvv == 0
                                                    select new { erv, rvv }).ToList();

                    foreach (var toDelRV in toDeleteReportVisibility)
                    {
                        client.Delete("reportvisibility", toDelRV.erv.ReportVisibilityId);
                        Log($"Removed visiblity {toDelRV.erv.Visibility.ToString()} from report {_fileName}");
                    }
                }
            }
            Log("Done!");
        }

        public void RegisterWeb(string assemblyName, string connectionString, string solutionName)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, false);
        }
        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, Guid @namespace)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, false, @namespace);
        }
        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, bool contentCompare)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, new List<string>(), true, false, true, contentCompare, Guid.Empty);
        }
        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, bool contentCompare, Guid @namespace)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, new List<string>(), true, false, true, contentCompare, @namespace);
        }
        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, bool contentCompare, bool deleteMissing, bool promptForDelete)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, new List<string>(), true, deleteMissing, promptForDelete, contentCompare, Guid.Empty);
        }
        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, bool contentCompare, bool deleteMissing, bool promptForDelete, Guid @namespace)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, new List<string>(), true, deleteMissing, promptForDelete, contentCompare, @namespace);
        }

        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, bool contentCompare, bool deleteMissing, bool promptForDelete, Dictionary<string, string> spoofList)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, new List<string>(), true, deleteMissing, promptForDelete, contentCompare, spoofList, Guid.Empty);
        }

        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, bool contentCompare, bool deleteMissing, bool promptForDelete, Dictionary<string, string> spoofList, Guid @namespace)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, new List<string>(), true, deleteMissing, promptForDelete, contentCompare, spoofList, @namespace);
        }

        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, List<string> whiteList, bool iswhitelist, bool deleteMissing, bool promptForDelete, bool contentCompare)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, whiteList, iswhitelist, deleteMissing, promptForDelete, contentCompare, Guid.Empty);
        }

        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, List<string> whiteList, bool iswhitelist, bool deleteMissing, bool promptForDelete, bool contentCompare, Guid @namespace)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, whiteList, iswhitelist, deleteMissing, promptForDelete, contentCompare, new Dictionary<string, string>(), Guid.Empty);
        }

        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, List<string> whiteList, bool iswhitelist, bool deleteMissing, bool promptForDelete, bool contentCompare, Dictionary<string, string> spoofList)
        {
            RegisterWeb(assemblyName, connectionString, solutionName, whiteList, iswhitelist, deleteMissing, promptForDelete, contentCompare, spoofList, Guid.Empty);
        }
        public void RegisterWeb(string assemblyName, string connectionString, string solutionName, List<string> whiteList, bool iswhitelist, bool deleteMissing, bool promptForDelete, bool contentCompare, Dictionary<string, string> spoofList, Guid @namespace)
        {
            Log($"Begin to register");
            Log($"Assembly: {assemblyName}");
            Log($"Connection: {connectionString}");
            Log($"Solution: {solutionName}");

            var dnsNamespace = @namespace;
            if (dnsNamespace == Guid.Empty)
                dnsNamespace = GuidUtility.DnsNamespace;

            var client = Connection.CrmConnection.GetClientByConnectionString(connectionString);
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(client);

            string prefix = null;
            var solutionId = Utility.Utility.FoundSolution(solutionName, client, out prefix);

            if (solutionId.HasValue)
                Log($"Found solution with name {solutionName}");
            else
            {
                Log("Did not find solution, aborting");
                return;
            }


            var webresources = Enumerable.Repeat(new { Id = Guid.Empty, Type = new OptionSetValue(), Name = string.Empty, Content = string.Empty }, 0).ToList();

            if (contentCompare)
            {
                webresources = (from sc in context.CreateQuery("solutioncomponent")
                                join wr in context.CreateQuery("webresource") on (Guid)sc["objectid"] equals (Guid)wr["webresourceid"]
                                where (int)sc["componenttype"] == 61 && (Guid)sc["solutionid"] == solutionId
                                select new
                                {
                                    Id = (Guid)wr["webresourceid"],
                                    Type = (OptionSetValue)wr["webresourcetype"],
                                    Name = (string)wr["name"],
                                    Content = (string)wr["content"]
                                }).ToList();
            }
            else
            {
                webresources = (from sc in context.CreateQuery("solutioncomponent")
                                join wr in context.CreateQuery("webresource") on (Guid)sc["objectid"] equals (Guid)wr["webresourceid"]
                                where (int)sc["componenttype"] == 61 && (Guid)sc["solutionid"] == solutionId
                                select new
                                {
                                    Id = (Guid)wr["webresourceid"],
                                    Type = (OptionSetValue)wr["webresourcetype"],
                                    Name = (string)wr["name"],
                                    Content = string.Empty
                                }).ToList();
            }



            Log($"Fetched webresources from CRM, count {webresources.Count}");

            var filetypes = "*.htm|*.html|*.js|*.png|*.gif|*.jpg|*.xml|*.xap|*.xsl|*.ico|*.css";
            var files = Utility.Utility.GetFiles($"{Directory.GetCurrentDirectory()}", filetypes, prefix, System.IO.SearchOption.AllDirectories, spoofList);
            Log($"Fetched webresources from Project, count {files.Length}");
            var publishList = new List<Guid>();

            foreach (var file in files)
            {
                if (whiteList != null && whiteList.Count > 0)
                {
                    var _folders = whiteList.Where(x => x.EndsWith("*")).Select(x => x.Substring(0, x.LastIndexOf("\\*") + 1).ToLower()).ToList();
                    var _files = whiteList.Where(x => !x.EndsWith("*")).Select(x => x.ToLower()).ToList();

                    if (iswhitelist)
                    {
                        var toContinue1 = true;
                        var toContinue2 = true;
                        if (_files.Contains(file.Item4.ToLower()))
                            toContinue1 = false;
                        foreach (var _folder in _folders)
                            if (file.Item4.ToLower().StartsWith(_folder))
                            {
                                toContinue2 = false;
                                break;
                            }

                        if (toContinue1 && toContinue2)
                            continue;
                    }
                    else //blacklist
                    {
                        var toContinue1 = false;
                        var toContinue2 = false;
                        if (_files.Contains(file.Item4.ToLower()))
                            toContinue1 = true;
                        foreach (var _folder in _folders)
                            if (file.Item4.ToLower().StartsWith(_folder))
                            {
                                toContinue2 = true;
                                break;
                            }

                        if (toContinue1 || toContinue2)
                            continue;
                    }
                }

                var fileName = file.Item2;
                if(spoofList.ContainsKey(fileName))
                {
                   fileName = spoofList[fileName];
                }
                var existingWebresource = webresources.Where(x => x.Name == file.Item2).FirstOrDefault();
                var xmlbytes = Utility.Utility.GetBytesFromFile(file.Item1);
                var base64String = Convert.ToBase64String(xmlbytes);
                var wr = new Entity("webresource");

                wr.Attributes.Add(new KeyValuePair<string, object>("content", base64String));

                if (existingWebresource != null)
                {
                    if (contentCompare)
                    {
                        if (base64String == existingWebresource.Content)
                        {
                            Log($"Skipping webresource {file.Item2}");
                            continue;
                        }
                    }
                    wr.Attributes.Add(new KeyValuePair<string, object>("webresourceid", existingWebresource.Id));
                    var ur = new UpdateRequest { Target = wr };
                    var urr = (UpdateResponse)client.Execute(ur);
                    Log($"Updated webresource {file.Item2}");
                    publishList.Add(existingWebresource.Id);
                }
                else
                {
                    wr.Attributes.Add(new KeyValuePair<string, object>("webresourceid", GuidUtility.Create(dnsNamespace, file.Item1)));
                    wr.Attributes.Add(new KeyValuePair<string, object>("displayname", file.Item2));
                    wr.Attributes.Add(new KeyValuePair<string, object>("iscustomizable", true));
                    wr.Attributes.Add(new KeyValuePair<string, object>("ismanaged", false));
                    wr.Attributes.Add(new KeyValuePair<string, object>("canbedeleted", true));
                    wr.Attributes.Add(new KeyValuePair<string, object>("componentstate", new OptionSetValue { Value = 0 }));
                    wr.Attributes.Add(new KeyValuePair<string, object>("ishidden", false));
                    wr.Attributes.Add(new KeyValuePair<string, object>("name", file.Item2));
                    wr.Attributes.Add(new KeyValuePair<string, object>("webresourcetype", new OptionSetValue(file.Item3)));
                    var cr = new CreateRequest { Target = wr };
                    cr.Parameters.Add("SolutionUniqueName", solutionName);
                    var crr = (CreateResponse)client.Execute(cr);
                    Log($"Created webresource {file.Item2}");
                    publishList.Add(GuidUtility.Create(dnsNamespace, file.Item1));
                }
            }

            if (deleteMissing)
            {
                var filesToRemove = webresources.Select(x => x.Name).Except(files.Select(x => x.Item2)).ToList();

                if (filesToRemove.Count > 0)
                {
                    Log($"Found {filesToRemove.Count} webresources in CRM not found in the project:");
                    if (promptForDelete)
                    {
                        foreach (var file in filesToRemove)
                            Console.WriteLine(file);
                        Console.Write("Do you want to delete these webresources (Y/N):");
                        var answer = Console.ReadKey();
                        Console.WriteLine();
                        if (answer.Key == ConsoleKey.Y)
                        {
                            foreach (var file in filesToRemove)
                            {
                                var id = webresources.Where(x => x.Name == file).Select(x => x.Id).FirstOrDefault();
                                if (id != null)
                                {
                                    client.Delete("webresource", id);
                                    Log($"Deleted webresource {file}");
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var file in filesToRemove)
                        {
                            var id = webresources.Where(x => x.Name == file).Select(x => x.Id).FirstOrDefault();
                            if (id != null)
                            {
                                client.Delete("webresource", id);
                                Log($"Deleted webresource {file}");
                            }
                        }
                    }
                }
            }


            if (publishList != null && publishList.Count > 0)
            {
                var req = new PublishXmlRequest();
                req.ParameterXml = "<importexportxml><webresources>";
                foreach (var Id in publishList)
                    req.ParameterXml += "<webresource>{" + Id.ToString() + "}</webresource>";
                req.ParameterXml += "</webresources></importexportxml>";
                Log($"Publishing {publishList.Count} items");
                var preq = (PublishXmlResponse)client.Execute(req);
            }

            Log("Done!");
        }

        public void RegisterAssembly(string assemblyName, string connectionString, string solutionName)
        {
            RegisterAssembly(assemblyName, connectionString, solutionName, Guid.Empty);
        }
        public void RegisterAssembly(string assemblyName, string connectionString, string solutionName, Guid @namespace)
        {
            pluginUnsecureConfig = PluginConfig.GetConfigList();

            Log($"Begin to register");
            Log($"Assembly: {assemblyName}");
            Log($"Connection: {connectionString}");
            Log($"Solution: {solutionName}");

            var dnsNamespace = @namespace;
            if (dnsNamespace == Guid.Empty)
                dnsNamespace = GuidUtility.DnsNamespace;

            var client = Connection.CrmConnection.GetClientByConnectionString2(connectionString);

            var xrmMetaData = new XrmMetaData(client);

            var solutionId = Utility.Utility.FoundSolution(solutionName, client);

            if (solutionId.HasValue)
                Log($"Found solution with name {solutionName}");
            else
                Log("Did not find solution, using default solution");

            var ass = Assembly.LoadFrom(assemblyName);
            Version ver = ass.GetName().Version;
            var shortAssemblyName = ass.GetName().Name;

            Log("Retrieving configuration from Assembly");
            var assemblyConfig = XrmAssemblyConfiguration.GetConfiguration(ass);

            Log($"Assembly is of type {assemblyConfig.AssemblyConfig.XrmAssemblyType.ToString()}");

            Log("Retrieving existing configuration from CRM");

            XrmInstanceConfiguration instanseConfig = null;

            if (assemblyConfig.AssemblyConfig.XrmAssemblyType == XrmAssemblyType.Plugin)
                instanseConfig = XrmInstanceConfiguration.GetPluginTypesHiearki(shortAssemblyName, client);
            else if (assemblyConfig.AssemblyConfig.XrmAssemblyType == XrmAssemblyType.Workflow)
                instanseConfig = XrmInstanceConfiguration.GetWorkflowTypes(shortAssemblyName, client);
            else if (assemblyConfig.AssemblyConfig.XrmAssemblyType == XrmAssemblyType.Webhook)
                instanseConfig = XrmInstanceConfiguration.GetWebHookTypesHiearki(shortAssemblyName, client);

            if (instanseConfig.AssemblyRef != null)
                Log("Found assembly in CRM");
            else
                Log("Did not find assembly in CRM");


            if (assemblyConfig.AssemblyConfig.XrmAssemblyType == XrmAssemblyType.Workflow)
            {
                if (instanseConfig.AssemblyRef != null)
                    Log("Removing missing workflowtypes");

                var toRemoveWorkflowTypes = (from i in instanseConfig.WorkflowTypes
                                             join a in assemblyConfig.WorkFlowTypes on
                                              new { Id1 = i.Name }
                                              equals
                                              new { Id1 = a.Workflow.TypeName }
                                              into _a
                                             from a in _a.DefaultIfEmpty(null)
                                             where a == null
                                             select i).ToList();


                foreach (var toRemoveWorkflowType in toRemoveWorkflowTypes)
                {
                    client.Delete("plugintype", toRemoveWorkflowType.Id);
                    Log($"Removed workflowtype {toRemoveWorkflowType.Name}");
                }

                Log("Cleanup Done");
                Log("*");

                var pa = new Entity("pluginassembly");

                if (assemblyConfig.AssemblyConfig.SourceType == SourceType.Database)
                {
                    FileStream fs = new FileStream(assemblyName,
                                       FileMode.Open,
                                       FileAccess.Read);
                    byte[] filebytes = new byte[fs.Length];
                    fs.Read(filebytes, 0, Convert.ToInt32(fs.Length));
                    string encodedData = Convert.ToBase64String(filebytes);
                    pa.Attributes.Add("content", encodedData);
                }
                else if (assemblyConfig.AssemblyConfig.SourceType == SourceType.Disk)
                {
                    throw new NotImplementedException("Disk!");
                }
                else
                {
                    throw new Exception("No sourcetype defined!");
                }

                pa.Attributes.Add("sourcetype", new OptionSetValue((int)assemblyConfig.AssemblyConfig.SourceType));
                pa.Attributes.Add("isolationmode", new OptionSetValue((int)assemblyConfig.AssemblyConfig.IsolationMode));
                pa.Attributes.Add("version", ver.ToString());
                pa.Attributes.Add("name", shortAssemblyName);

                if (instanseConfig.AssemblyRef == null)
                {
                    Log("Creating assembly: " + assemblyName);
                    pa.Id = GuidUtility.Create(dnsNamespace, assemblyName);
                    pa.Id = client.Create(pa);
                    instanseConfig.AssemblyRef = pa.ToEntityReference();
                }
                else
                {
                    Log("Updating assembly: " + assemblyName);
                    pa.Id = instanseConfig.AssemblyRef.Id;
                    client.Update(pa);
                }

                if (solutionId.HasValue)
                {
                    AddSolutionComponentRequest addReq1 = new AddSolutionComponentRequest()
                    {
                        ComponentType = 91,
                        ComponentId = instanseConfig.AssemblyRef.Id,
                        SolutionUniqueName = solutionName
                    };
                    Log("Add assembly to solution: " + solutionName);
                    var result = client.Execute(addReq1);
                }

                foreach (var wt in assemblyConfig.WorkFlowTypes)
                {
                    var existingPluginTypeContainer = instanseConfig.WorkflowTypes.Where(x => x.Name == wt.Workflow.TypeName).FirstOrDefault(); // existingPlugintypes.Where(x => x.Name == typename).FirstOrDefault();

                    var ptype = new Entity("plugintype");
                    ptype.Attributes.Add("pluginassemblyid", pa.ToEntityReference());
                    ptype.Attributes.Add("name", wt.Workflow.NameInGroup);
                    ptype.Attributes.Add("typename", wt.Workflow.TypeName);
                    ptype.Attributes.Add("workflowactivitygroupname", wt.Workflow.Group);

                    if (existingPluginTypeContainer != null)
                    {
                        if (Utility.Utility.Compare(wt.Workflow, existingPluginTypeContainer))
                        {
                            Log($"Skipping workflowtype: {ptype.GetAttributeValue<string>("typename")}");
                        }
                        else
                        {
                            Log("Updating workflowtype: " + ptype.GetAttributeValue<string>("typename"));
                            ptype.Id = existingPluginTypeContainer.Id;
                            ptype.Attributes.Add("friendlyname", ptype.Id.ToString());
                            client.Update(ptype);
                        }
                    }
                    else
                    {
                        Log("Creating workflowtype: " + ptype.GetAttributeValue<string>("typename"));
                        ptype.Id = GuidUtility.Create(dnsNamespace, ptype.GetAttributeValue<string>("typename"));
                        ptype.Attributes.Add("friendlyname", ptype.Id.ToString());
                        ptype.Id = client.Create(ptype);
                    }

                }
                Log("Done");
            }
            else if (assemblyConfig.AssemblyConfig.XrmAssemblyType == XrmAssemblyType.Plugin)
            {
                //Delete Plugins/Steps/Images that no longer exist, beacuse can't update assembly if plugins have been removed
                //Remove images on existing steps
                if (instanseConfig.AssemblyRef != null)
                    Log("Removing missing Plugins/Steps/Images from Plugin");

                var assemblyConfigImages = assemblyConfig.GetImages();
                var instanceConfigImages = instanseConfig.GetImages();

                var toRemoveImages = (from i in instanceConfigImages
                                      join a in assemblyConfigImages on
                                      new { Id1 = i.Name, Id2 = i.XrmStepContainerName, Id3 = i.XrmPluginTypeName }
                                      equals
                                      new { Id1 = a.Name, Id2 = a.PluginEventName, Id3 = a.TypeName }
                                      into _a
                                      from a in _a.DefaultIfEmpty(null)
                                      where a == null
                                      select i).ToList();


                //Removed steps on existing pluginstypes
                var assemblyConfigSteps = assemblyConfig.GetSteps();
                var instanceConfigSteps = instanseConfig.GetSteps();

                var toRemoveSteps = (from i in instanceConfigSteps
                                     join a in assemblyConfigSteps on
                                      new { Id1 = i.Name, Id3 = i.XrmPluginTypeName }
                                      equals
                                      new { Id1 = a.Name, Id3 = a.TypeName }
                                      into _a
                                     from a in _a.DefaultIfEmpty(null)
                                     where a == null
                                     select i).ToList();


                var toRemovePluginTypes = (from i in instanseConfig.PluginTypes
                                           join a in assemblyConfig.PluginTypes on
                                            new { Id1 = i.Name }
                                            equals
                                            new { Id1 = a.TypeName }
                                            into _a
                                           from a in _a.DefaultIfEmpty(null)
                                           where a == null
                                           select i).ToList();


                Log("Removing missing images");
                foreach (var toRemoveImage in toRemoveImages)
                {
                    client.Delete("sdkmessageprocessingstepimage", toRemoveImage.Id);
                    Log($"Removed image {toRemoveImage.Name} on step {toRemoveImage.XrmStepContainerName} on plugintype {toRemoveImage.XrmPluginTypeName}");
                }

                Log("Removing missing steps");
                foreach (var toRemoveStep in toRemoveSteps)
                {
                    client.Delete("sdkmessageprocessingstep", toRemoveStep.Id);
                    Log($"Removed step {toRemoveStep.Name} on plugintype {toRemoveStep.XrmPluginTypeName}");
                    if (toRemoveStep.SecureConfigId.HasValue)
                    {
                        client.Delete("sdkmessageprocessingstepsecureconfig", toRemoveStep.SecureConfigId.Value);
                        Log($"Removed secure config for step {toRemoveStep.Name} on plugintype {toRemoveStep.XrmPluginTypeName}");
                    }
                }

                Log("Removing missing plugintypes");
                foreach (var toRemovePluginType in toRemovePluginTypes)
                {
                    client.Delete("plugintype", toRemovePluginType.Id);
                    Log($"Removed plugintype {toRemovePluginType.Name}");
                }

                Log("Cleanup Done");
                Log("*");

                //Register Assembly
                var pa = new Entity("pluginassembly");

                if (assemblyConfig.AssemblyConfig.SourceType == SourceType.Database)
                {
                    FileStream fs = new FileStream(assemblyName,
                                       FileMode.Open,
                                       FileAccess.Read);
                    byte[] filebytes = new byte[fs.Length];
                    fs.Read(filebytes, 0, Convert.ToInt32(fs.Length));
                    string encodedData = Convert.ToBase64String(filebytes);
                    pa.Attributes.Add("content", encodedData);
                }
                else if (assemblyConfig.AssemblyConfig.SourceType == SourceType.Disk)
                {
                    throw new NotImplementedException("Disk!");
                }
                else
                {
                    throw new Exception("No sourcetype defined!");
                }

                pa.Attributes.Add("sourcetype", new OptionSetValue((int)assemblyConfig.AssemblyConfig.SourceType));
                pa.Attributes.Add("isolationmode", new OptionSetValue((int)assemblyConfig.AssemblyConfig.IsolationMode));
                pa.Attributes.Add("version", ver.ToString());
                pa.Attributes.Add("name", shortAssemblyName);

                if (instanseConfig.AssemblyRef == null)
                {
                    Log("Creating assembly: " + assemblyName);
                    pa.Id = GuidUtility.Create(dnsNamespace, assemblyName);
                    pa.Id = client.Create(pa);
                    instanseConfig.AssemblyRef = pa.ToEntityReference();
                }
                else
                {
                    Log("Updating assembly: " + assemblyName);
                    pa.Id = instanseConfig.AssemblyRef.Id;
                    client.Update(pa);
                }

                if (solutionId.HasValue)
                {
                    AddSolutionComponentRequest addReq1 = new AddSolutionComponentRequest()
                    {
                        ComponentType = 91,
                        ComponentId = instanseConfig.AssemblyRef.Id,
                        SolutionUniqueName = solutionName
                    };
                    Log("Add assembly to solution: " + solutionName);
                    var result = client.Execute(addReq1);
                }


                foreach (var pluginType in assemblyConfig.PluginTypes)
                {
                    var existingPluginTypeContainer = instanseConfig.PluginTypes.Where(x => x.Name == pluginType.TypeName).FirstOrDefault(); // existingPlugintypes.Where(x => x.Name == typename).FirstOrDefault();

                    var ptype = new Entity("plugintype");
                    ptype.Attributes.Add("pluginassemblyid", pa.ToEntityReference());
                    ptype.Attributes.Add("name", pluginType.TypeName);
                    ptype.Attributes.Add("typename", pluginType.TypeName);

                    if (existingPluginTypeContainer == null)
                    {
                        Log($"Creating plugintype {ptype.GetAttributeValue<string>("typename")}");
                        ptype.Id = GuidUtility.Create(dnsNamespace, ptype.GetAttributeValue<string>("typename"));
                        ptype.Attributes.Add("friendlyname", ptype.Id.ToString());
                        ptype.Id = client.Create(ptype);
                    }
                    else
                    {
                        Log($"Skipping plugintype {ptype.GetAttributeValue<string>("typename")}");
                        ptype.Id = existingPluginTypeContainer.Id;
                        ptype.Attributes.Add("friendlyname", ptype.Id.ToString());
                    }


                    foreach (var stp in pluginType.Steps)
                    {
                        var overrideUpdateImages = false;

                        var step = new Entity("sdkmessageprocessingstep");
                        step.Attributes.Add("asyncautodelete", false);
                        step.Attributes.Add("stage", new Microsoft.Xrm.Sdk.OptionSetValue((int)stp.Stage));
                        step.Attributes.Add("mode", new OptionSetValue((int)stp.StepMode));
                        step.Attributes.Add("rank", stp.Rank);
                        step.Attributes.Add("name", stp.Name);
                        step.Attributes.Add("description", "");
                        step.Attributes.Add("supporteddeployment", new OptionSetValue(0));  //new OptionSetValue((int)stp.SupportedDeployment);
                        step.Attributes.Add("filteringattributes", null);

                        if (stp.FilteringAttributes != null && stp.FilteringAttributes.Length != 0 && stp.MessageName == "Update")
                        {
                            if (stp.FilteredAttributeMode == AttributeMode.Include)
                                step["filteringattributes"] = string.Join(",", stp.FilteringAttributes.Select(x => x.ToLower()));
                            else
                            {
                                var entityAttributes = xrmMetaData.GetFilteringAttributeMetaDataForEntitiy(stp.EntityName);
                                step["filteringattributes"] = string.Join(",", entityAttributes.Except(stp.FilteringAttributes.Select(x => x.ToLower())));
                            }
                        }

                        var configString = GetSecureUnsecureConfiguration(ptype.GetAttributeValue<string>("typename"), stp.Name);
                        step.Attributes.Add("configuration", configString.Item1);

                        var existingStepContainer = instanceConfigSteps.Where(x => x.Name == stp.Name && x.XrmPluginTypeName == stp.TypeName).FirstOrDefault();
                        var _StepId = Guid.Empty;

                        if (Utility.Utility.Compare(stp, existingStepContainer, step.GetAttributeValue<string>("filteringattributes"), step.GetAttributeValue<string>("configuration"), configString.Item2))
                        {
                            Log($"Skipping step {step.GetAttributeValue<string>("name") } ({ptype.GetAttributeValue<string>("typename")})");
                            _StepId = existingStepContainer.Id;
                        }
                        else
                        {
                            if (existingStepContainer != null)
                            {
                                if (stp.MessageName == "Create" && existingStepContainer.Message != "Create")
                                    overrideUpdateImages = true;
                                if (stp.MessageName != "Create" && existingStepContainer.Message == "Create")
                                    overrideUpdateImages = true;
                            }
                            //If secure config been added
                            if ((existingStepContainer == null || !existingStepContainer.SecureConfigId.HasValue) && !string.IsNullOrWhiteSpace(configString.Item2))
                            {
                                var sdkmessageprocessingstepsecureconfig = new Entity("sdkmessageprocessingstepsecureconfig");
                                sdkmessageprocessingstepsecureconfig.Attributes.Add("sdkmessageprocessingstepsecureconfigid", GuidUtility.Create(dnsNamespace, ptype.GetAttributeValue<string>("typeName") + step.GetAttributeValue<string>("name") + "secureconfig"));
                                sdkmessageprocessingstepsecureconfig.Attributes.Add("secureconfig", configString.Item2);
                                var sdkmessageprocessingstepsecureconfigid = client.Create(sdkmessageprocessingstepsecureconfig);
                                step.Attributes.Add("sdkmessageprocessingstepsecureconfigid", new EntityReference("sdkmessageprocessingstepsecureconfig", sdkmessageprocessingstepsecureconfigid));
                                Log($"Added secucureconfig on {step.GetAttributeValue<string>("name")} ({ptype.GetAttributeValue<string>("typename")})");
                            }
                            // If secure config been removed
                            else if (existingStepContainer != null && existingStepContainer.SecureConfigId.HasValue && string.IsNullOrWhiteSpace(configString.Item2))
                            {
                                //step.SdkMessageProcessingStepSecureConfigId = null;
                                step.Attributes.Add("sdkmessageprocessingstepsecureconfigid", null);
                            }
                            // If secure config been changed
                            else if (existingStepContainer != null && existingStepContainer.SecureConfigId.HasValue && !string.IsNullOrWhiteSpace(configString.Item2))
                            {
                                var sdkmessageprocessingstepsecureconfig = new Entity("sdkmessageprocessingstepsecureconfig");
                                sdkmessageprocessingstepsecureconfig.Attributes.Add("sdkmessageprocessingstepsecureconfigid", existingStepContainer.SecureConfigId.Value);
                                sdkmessageprocessingstepsecureconfig.Attributes.Add("secureconfig", configString.Item2);
                                client.Update(sdkmessageprocessingstepsecureconfig);
                                Log($"Updated secucureconfig on {step.GetAttributeValue<string>("name")} ({ptype.GetAttributeValue<string>("typename")})");
                            }

                            step.Attributes.Add("sdkmessageid", Utility.Utility.GetMessageId(stp.MessageName, client));
                            step.Attributes.Add("sdkmessagefilterid", Utility.Utility.GetSdkMessageFilterId(stp.EntityName, step.GetAttributeValue<EntityReference>("sdkmessageid").Id, client));
                            step.Attributes.Add("eventhandler", ptype.ToEntityReference());


                            if (existingStepContainer == null)
                            {
                                Log($"Creating step {step.GetAttributeValue<string>("name")} ({ptype.GetAttributeValue<string>("typename")})");
                                _StepId = step.Id = GuidUtility.Create(dnsNamespace, ptype.GetAttributeValue<string>("typename") + step.GetAttributeValue<string>("name"));
                                step.Id = client.Create(step);
                            }
                            else
                            {
                                Log($"Updating step {step.GetAttributeValue<string>("name")} ({ptype.GetAttributeValue<string>("typename")})");
                                _StepId = step.Id = existingStepContainer.Id;
                                client.Update(step);

                                if (step.Contains("sdkmessageprocessingstepsecureconfigid"))
                                {
                                    if (step["sdkmessageprocessingstepsecureconfigid"] == null)
                                    {
                                        client.Delete("sdkmessageprocessingstepsecureconfig", existingStepContainer.SecureConfigId.Value);
                                        Log($"Removed secucureconfig on {step.GetAttributeValue<string>("name")} ({ptype.GetAttributeValue<string>("typename")})");
                                    }
                                }
                            }

                            if (solutionId.HasValue)
                            {
                                AddSolutionComponentRequest addReq1 = new AddSolutionComponentRequest()
                                {
                                    ComponentType = 92,
                                    ComponentId = step.Id,
                                    SolutionUniqueName = solutionName
                                };
                                //Log("Adding step to solution: " + solutionName);
                                var result = client.Execute(addReq1);
                            }
                        }

                        foreach (var _image in stp.Images)
                        {
                            var image = new Entity("sdkmessageprocessingstepimage");

                            image.Attributes.Add("imagetype", new OptionSetValue((int)_image.ImageType));
                            image.Attributes.Add("name", _image.Name);
                            image.Attributes.Add("entityalias", _image.Name);
                            if (stp.MessageName == "Update" || stp.MessageName == "Delete")
                                image.Attributes.Add("messagepropertyname", "Target");
                            else if (stp.MessageName == "Create")
                                image.Attributes.Add("messagepropertyname", "Id");
                            image.Attributes.Add("sdkmessageprocessingstepid", new EntityReference("sdkmessageprocessingstep", _StepId)); // step.ToEntityReference();

                            if (_image.Attributes != null && _image.Attributes.Length != 0)
                            {
                                if (_image.AttributeMode == AttributeMode.Include)
                                    image.Attributes.Add("attributes", string.Join(",", _image.Attributes.Select(x => x.ToLower())));
                                else
                                {
                                    var entityAttributes = xrmMetaData.GetFilteringAttributeMetaDataForEntitiy(stp.EntityName);
                                    image.Attributes.Add("attributes", string.Join(",", entityAttributes.Except(stp.FilteringAttributes.Select(x => x.ToLower()))));
                                }
                            }
                            else
                            {
                                image.Attributes.Add("attributes", null);
                            }


                            var existingImageContainer = instanceConfigImages.Where(x =>
                            x.Name == _image.Name &&
                            x.XrmPluginTypeName == _image.TypeName &&
                            x.XrmStepContainerName == _image.PluginEventName
                            ).FirstOrDefault();

                            if (!overrideUpdateImages && Utility.Utility.Compare(_image, existingImageContainer, image.GetAttributeValue<string>("attributes")))
                            {
                                Log($"Skipping image {image.GetAttributeValue<string>("name")} ({stp.Name}|{ptype.GetAttributeValue<string>("typename")})");
                            }
                            else
                            {
                                if (existingImageContainer == null)
                                {
                                    Log($"Creating image {image.GetAttributeValue<string>("name")} ({stp.Name}|{ptype.GetAttributeValue<string>("typename")})");
                                    image.Id = GuidUtility.Create(dnsNamespace, image.GetAttributeValue<string>("name") + stp.Name + ptype.GetAttributeValue<string>("typename"));
                                    image.Id = client.Create(image);
                                }
                                else
                                {
                                    Log($"Updating image {image.GetAttributeValue<string>("name")} ({stp.Name}|{ptype.GetAttributeValue<string>("typename")})");
                                    image.Id = existingImageContainer.Id;
                                    client.Update(image);
                                }
                            }

                        }
                    }
                }
                Log("Done!");
            }
            else if (assemblyConfig.AssemblyConfig.XrmAssemblyType == XrmAssemblyType.Webhook)
            {
                //Delete Plugins/Steps/Images that no longer exist, beacuse can't update assembly if plugins have been removed
                //Remove images on existing steps
                //if (instanseConfig.AssemblyRef != null)
                 Log("Removing missing Plugins/Steps/Images from Webhook");

                var assemblyConfigImages = assemblyConfig.GetWebHookImages();
                var instanceConfigImages = instanseConfig.GetWebhookImages();

                var toRemoveImages = (from i in instanceConfigImages
                                      join a in assemblyConfigImages on
                                      new { Id1 = i.Name, Id2 = i.XrmStepContainerName, Id3 = i.XrmPluginTypeName }
                                      equals
                                      new { Id1 = a.Name, Id2 = a.PluginEventName, Id3 = a.TypeName }
                                      into _a
                                      from a in _a.DefaultIfEmpty(null)
                                      where a == null
                                      select i).ToList();


                //Removed steps on existing pluginstypes
                var assemblyConfigSteps = assemblyConfig.GetWebHookSteps();
                var instanceConfigSteps = instanseConfig.GetWebhookSteps();

                var toRemoveSteps = (from i in instanceConfigSteps
                                     join a in assemblyConfigSteps on
                                      new { Id1 = i.Name, Id3 = i.XrmPluginTypeName }
                                      equals
                                      new { Id1 = a.Name, Id3 = a.Name }
                                      into _a
                                     from a in _a.DefaultIfEmpty(null)
                                     where a == null
                                     select i).ToList();


                var toRemovePluginTypes = (from i in instanseConfig.PluginTypes
                                           join a in assemblyConfig.PluginTypes on
                                            new { Id1 = i.Name }
                                            equals
                                            new { Id1 = a.TypeName }
                                            into _a
                                           from a in _a.DefaultIfEmpty(null)
                                           where a == null
                                           select i).ToList();


                Log("Removing missing images");
                foreach (var toRemoveImage in toRemoveImages)
                {
                    client.Delete("sdkmessageprocessingstepimage", toRemoveImage.Id);
                    Log($"Removed image {toRemoveImage.Name} on step {toRemoveImage.XrmStepContainerName} on plugintype {toRemoveImage.XrmPluginTypeName}");
                }

                Log("Removing missing steps");
                foreach (var toRemoveStep in toRemoveSteps)
                {
                    client.Delete("sdkmessageprocessingstep", toRemoveStep.Id);
                    Log($"Removed step {toRemoveStep.Name} on plugintype {toRemoveStep.XrmPluginTypeName}");
                    if (toRemoveStep.SecureConfigId.HasValue)
                    {
                        client.Delete("sdkmessageprocessingstepsecureconfig", toRemoveStep.SecureConfigId.Value);
                        Log($"Removed secure config for step {toRemoveStep.Name} on plugintype {toRemoveStep.XrmPluginTypeName}");
                    }
                }

                Log("Removing missing plugintypes");
                foreach (var toRemovePluginType in toRemovePluginTypes)
                {
                    client.Delete("plugintype", toRemovePluginType.Id);
                    Log($"Removed plugintype {toRemovePluginType.Name}");
                }

                Log("Cleanup Done");
                Log("*");

                //Register Assembly
                //var pa = new Entity("pluginassembly");

                //if (assemblyConfig.AssemblyConfig.SourceType == SourceType.Database)
                //{
                //    FileStream fs = new FileStream(assemblyName,
                //                       FileMode.Open,
                //                       FileAccess.Read);
                //    byte[] filebytes = new byte[fs.Length];
                //    fs.Read(filebytes, 0, Convert.ToInt32(fs.Length));
                //    string encodedData = Convert.ToBase64String(filebytes);
                //    pa.Attributes.Add("content", encodedData);
                //}
                //else if (assemblyConfig.AssemblyConfig.SourceType == SourceType.Disk)
                //{
                //    throw new NotImplementedException("Disk!");
                //}
                //else
                //{
                //    throw new Exception("No sourcetype defined!");
                //}

                //pa.Attributes.Add("sourcetype", new OptionSetValue((int)assemblyConfig.AssemblyConfig.SourceType));
                //pa.Attributes.Add("isolationmode", new OptionSetValue((int)assemblyConfig.AssemblyConfig.IsolationMode));
                //pa.Attributes.Add("version", ver.ToString());
                //pa.Attributes.Add("name", shortAssemblyName);

                //if (instanseConfig.AssemblyRef == null)
                //{
                //    Log("Creating assembly: " + assemblyName);
                //    pa.Id = GuidUtility.Create(dnsNamespace, assemblyName);
                //    pa.Id = client.Create(pa);
                //    instanseConfig.AssemblyRef = pa.ToEntityReference();
                //}
                //else
                //{
                //    Log("Updating assembly: " + assemblyName);
                //    pa.Id = instanseConfig.AssemblyRef.Id;
                //    client.Update(pa);
                //}

                if (solutionId.HasValue)
                {
                    AddSolutionComponentRequest addReq1 = new AddSolutionComponentRequest()
                    {
                        ComponentType = 91,
                        ComponentId = instanseConfig.AssemblyRef.Id,
                        SolutionUniqueName = solutionName
                    };
                    Log("Add assembly to solution: " + solutionName);
                    var result = client.Execute(addReq1);
                }


                foreach (var webHookType in assemblyConfig.WebHookTypes)
                {
                    var existinWebHookTypeContainer = instanseConfig.WebHookTypes.Where(x => x.Name == webHookType.TypeName).FirstOrDefault(); // existingPlugintypes.Where(x => x.Name == typename).FirstOrDefault();

                    var sendpoint = new Entity("serviceendpoint");
                    sendpoint.Attributes.Add("name", webHookType.TypeName);
                    //ptype.Attributes.Add("pluginassemblyid", pa.ToEntityReference());
                    sendpoint.Attributes.Add("contract", new OptionSetValue(8));
                    sendpoint.Attributes.Add("connectionmode", new OptionSetValue(1));
                    sendpoint.Attributes.Add("authvalue", "");
                    sendpoint.Attributes.Add("url", ""); //app.config replacement

                    if (existinWebHookTypeContainer == null)
                    {
                        Log($"Creating webhook {sendpoint.GetAttributeValue<string>("name") }");
                        sendpoint.Id = GuidUtility.Create(dnsNamespace, sendpoint.GetAttributeValue<string>("name"));
                       // ptype.Attributes.Add("friendlyname", ptype.Id.ToString());
                        sendpoint.Id = client.Create(sendpoint);
                    }
                    else
                    {
                        Log($"Updating webhook { sendpoint.GetAttributeValue<string>("name")}");
                        sendpoint.Id = existinWebHookTypeContainer.Id;

                        client.Update(sendpoint);
                        //ptype.Attributes.Add("friendlyname", ptype.Id.ToString());
                    }


                    foreach (var stp in webHookType.Steps)
                    {
                        var overrideUpdateImages = false;

                        var step = new Entity("sdkmessageprocessingstep");
                        step.Attributes.Add("asyncautodelete", false);
                        step.Attributes.Add("stage", new Microsoft.Xrm.Sdk.OptionSetValue((int)stp.Stage));
                        step.Attributes.Add("mode", new OptionSetValue((int)stp.StepMode));
                        step.Attributes.Add("rank", stp.Rank);
                        step.Attributes.Add("name", stp.Name);
                        step.Attributes.Add("description", "");
                        step.Attributes.Add("supporteddeployment", new OptionSetValue(0));  //new OptionSetValue((int)stp.SupportedDeployment);
                        step.Attributes.Add("filteringattributes", null);

                        if (stp.FilteringAttributes != null && stp.FilteringAttributes.Length != 0 && stp.MessageName == "Update")
                        {
                            if (stp.FilteredAttributeMode == AttributeMode.Include)
                                step["filteringattributes"] = string.Join(",", stp.FilteringAttributes.Select(x => x.ToLower()));
                            else
                            {
                                var entityAttributes = xrmMetaData.GetFilteringAttributeMetaDataForEntitiy(stp.EntityName);
                                step["filteringattributes"] = string.Join(",", entityAttributes.Except(stp.FilteringAttributes.Select(x => x.ToLower())));
                            }
                        }

                        var existingStepContainer = instanceConfigSteps.Where(x => x.Name == stp.Name && x.XrmPluginTypeName == stp.Name).FirstOrDefault();
                        var _StepId = Guid.Empty;

                        if (Utility.Utility.Compare(stp, existingStepContainer, step.GetAttributeValue<string>("filteringattributes")))
                        {
                            Log($"Skipping step {step.GetAttributeValue<string>("name")} ({sendpoint.GetAttributeValue<string>("name")})");
                            _StepId = existingStepContainer.Id;
                        }
                        else
                        {
                            if (existingStepContainer != null)
                            {
                                if (stp.MessageName == "Create" && existingStepContainer.Message != "Create")
                                    overrideUpdateImages = true;
                                if (stp.MessageName != "Create" && existingStepContainer.Message == "Create")
                                    overrideUpdateImages = true;
                            }
                          
                           
                            step.Attributes.Add("sdkmessageid", Utility.Utility.GetMessageId(stp.MessageName, client));
                            step.Attributes.Add("sdkmessagefilterid", Utility.Utility.GetSdkMessageFilterId(stp.EntityName, step.GetAttributeValue<EntityReference>("sdkmessageid").Id, client));
                            step.Attributes.Add("eventhandler", sendpoint.ToEntityReference());


                            if (existingStepContainer == null)
                            {
                                Log($"Creating step {step.GetAttributeValue<string>("name")} ({sendpoint.GetAttributeValue<string>("name")})");
                                _StepId = step.Id = GuidUtility.Create(dnsNamespace, sendpoint.GetAttributeValue<string>("typename") + step.GetAttributeValue<string>("name"));
                                step.Id = client.Create(step);
                            }
                            else
                            {
                                Log($"Updating step {step.GetAttributeValue<string>("name")} ({sendpoint.GetAttributeValue<string>("name")})");
                                _StepId = step.Id = existingStepContainer.Id;
                                client.Update(step);
                            }

                            if (solutionId.HasValue)
                            {
                                AddSolutionComponentRequest addReq1 = new AddSolutionComponentRequest()
                                {
                                    ComponentType = 92,
                                    ComponentId = step.Id,
                                    SolutionUniqueName = solutionName
                                };
                                //Log("Adding step to solution: " + solutionName);
                                var result = client.Execute(addReq1);
                            }
                        }

                        foreach (var _image in stp.Images)
                        {
                            var image = new Entity("sdkmessageprocessingstepimage");

                            image.Attributes.Add("imagetype", new OptionSetValue((int)_image.ImageType));
                            image.Attributes.Add("name", _image.Name);
                            image.Attributes.Add("entityalias", _image.Name);
                            if (stp.MessageName == "Update" || stp.MessageName == "Delete")
                                image.Attributes.Add("messagepropertyname", "Target");
                            else if (stp.MessageName == "Create")
                                image.Attributes.Add("messagepropertyname", "Id");
                            image.Attributes.Add("sdkmessageprocessingstepid", new EntityReference("sdkmessageprocessingstep", _StepId)); // step.ToEntityReference();

                            if (_image.Attributes != null && _image.Attributes.Length != 0)
                            {
                                if (_image.AttributeMode == AttributeMode.Include)
                                    image.Attributes.Add("attributes", string.Join(",", _image.Attributes.Select(x => x.ToLower())));
                                else
                                {
                                    var entityAttributes = xrmMetaData.GetFilteringAttributeMetaDataForEntitiy(stp.EntityName);
                                    image.Attributes.Add("attributes", string.Join(",", entityAttributes.Except(stp.FilteringAttributes.Select(x => x.ToLower()))));
                                }
                            }
                            else
                            {
                                image.Attributes.Add("attributes", null);
                            }


                            var existingImageContainer = instanceConfigImages.Where(x =>
                            x.Name == _image.Name &&
                            x.XrmPluginTypeName == _image.TypeName &&
                            x.XrmStepContainerName == _image.PluginEventName
                            ).FirstOrDefault();

                            if (!overrideUpdateImages && Utility.Utility.Compare(_image, existingImageContainer, image.GetAttributeValue<string>("attributes")))
                            {
                                Log($"Skipping image {image.GetAttributeValue<string>("name")} ({stp.Name}|{sendpoint.GetAttributeValue<string>("typename")})");
                            }
                            else
                            {
                                if (existingImageContainer == null)
                                {
                                    Log($"Creating image {image.GetAttributeValue<string>("name")} ({stp.Name}|{sendpoint.GetAttributeValue<string>("typename")})");
                                    image.Id = GuidUtility.Create(dnsNamespace, image.GetAttributeValue<string>("name") + stp.Name + sendpoint.GetAttributeValue<string>("typename"));
                                    image.Id = client.Create(image);
                                }
                                else
                                {
                                    Log($"Updating image {image.GetAttributeValue<string>("name")} ({stp.Name}|{sendpoint.GetAttributeValue<string>("typename")})");
                                    image.Id = existingImageContainer.Id;
                                    client.Update(image);
                                }
                            }

                        }
                    }
                }
                Log("Done!");
            }

        }

        public void GenerateMessagesStruct(string connectionString, string filePath)
        {
            var client = Connection.CrmConnection.GetClientByConnectionString(connectionString);
            var xrmMetaData = new XrmMetaData(client);

            xrmMetaData.GenerateMessagesStruct(filePath);
        }
    }
}
