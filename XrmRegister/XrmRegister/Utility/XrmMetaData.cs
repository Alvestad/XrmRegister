using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XrmRegister.Utility
{
    public class XrmMetaData
    {
        Dictionary<string, string[]> attributeStore = new Dictionary<string, string[]>();

        private readonly IOrganizationService _client;
        public XrmMetaData(IOrganizationService client)
        {
            _client = client;
        }


        //plug: https://github.com/Innofactor/PluginRegistration
        public string[] GetFilteringAttributeMetaDataForEntitiy(string entityName)
        {
            if (attributeStore.ContainsKey(entityName))
                return attributeStore[entityName];

            var retrieveEntityRequest = new RetrieveEntityRequest
            {
                EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Attributes,
                LogicalName = entityName,
                RetrieveAsIfPublished = false
            };

            var result = (RetrieveEntityResponse)_client.Execute(retrieveEntityRequest);

            var returnValue = new List<string>();

            foreach(var attribute in result.EntityMetadata.Attributes.Where(x => x.IsValidForRead.Value == true && null == x.AttributeOf))
            switch (attribute.AttributeType)
            {
                case AttributeTypeCode.Boolean:
                case AttributeTypeCode.Customer:
                case AttributeTypeCode.DateTime:
                case AttributeTypeCode.Decimal:
                case AttributeTypeCode.Double:
                case AttributeTypeCode.Integer:
                case AttributeTypeCode.Lookup:
                case AttributeTypeCode.Memo:
                case AttributeTypeCode.Money:
                case AttributeTypeCode.Owner:
                case AttributeTypeCode.PartyList:
                case AttributeTypeCode.Picklist:
                case AttributeTypeCode.State:
                case AttributeTypeCode.Status:
                case AttributeTypeCode.String:
                        returnValue.Add(attribute.LogicalName);
                    break;
                case AttributeTypeCode.CalendarRules:
                case AttributeTypeCode.Uniqueidentifier:
                case AttributeTypeCode.Virtual:
                    continue;
            }

            returnValue.Add(result.EntityMetadata.PrimaryIdAttribute);

            attributeStore.Add(entityName, returnValue.OrderBy(x => x).ToArray());

            return returnValue.OrderBy(x => x).ToArray();

        }

        public void GenerateMessagesStruct(string filepath)
        {
            var context = new Microsoft.Xrm.Sdk.Client.OrganizationServiceContext(_client);

            var sdkmessages = (from sm in context.CreateQuery("sdkmessage")
                               where (bool)sm["isprivate"] == false
                               select new { Name = (string)sm["name"] }).ToList();

            var outpath = GetPath(filepath);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outpath))
            {
                file.WriteLine("public struct XrmMessages");
                file.WriteLine("{");
                foreach (var x in sdkmessages.OrderBy(x => x.Name))
                {
                    file.WriteLine($"\tpublic static readonly string {x.Name} = \"{x.Name}\";");
                }
                file.WriteLine("}");
            }
        }

        private string GetPath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;
            else
            {
                string filePath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                var outpath = Path.GetFullPath($"{Path.GetDirectoryName(filePath)}\\{path}");
                return outpath;
            }
        }
    }
}
