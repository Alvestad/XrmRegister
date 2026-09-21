using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister
{
    public abstract class XrmWebHook
    {
        internal XrmWebHook(string config)
        {
            this.Config = config;
        }

        private Collection<WebHookStep> registeredSteps;
        protected Collection<WebHookStep> RegisteredSteps
        {
            get
            {
                if (this.registeredSteps == null)
                    this.registeredSteps = new Collection<WebHookStep>();
                return this.registeredSteps;
            }
        }

        private Collection<AuthValue> authValues;
        protected Collection<AuthValue> AuthValues
        {
            get
            {
                if (this.authValues == null)
                    this.authValues = new Collection<AuthValue>();
                return this.authValues;
            }
        }

        public string TypeName { get; set; }
        public string Url { get; set; }
        public WebhookAuthentication Authentication { get; set; }
        public string WebhookKeyValue { get; set; }
        public string Config { get; private set; }


        public string WebHookStepCollection
        {
            get
            {
                var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Collection<WebHookStep>));
                var ms = new MemoryStream();
                json_ser.WriteObject(ms, RegisteredSteps);

                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                return sr.ReadToEnd();
            }
        }

        public string AuthValuesCollection
        {
            get
            {
                var json_ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Collection<AuthValue>));
                var ms = new MemoryStream();
                json_ser.WriteObject(ms, AuthValues);

                ms.Position = 0;
                StreamReader sr = new StreamReader(ms);
                return sr.ReadToEnd();
            }
        }
    }

    #region WebHookStep

    public enum WebhookAuthentication
    {
        HttpQueryString = 6,
        Webhook = 4,
        HttpHeader = 5
    }

    [System.Runtime.Serialization.DataContract]
    public class AuthValue
    {
        [System.Runtime.Serialization.DataMember]
        public string Key { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string Value { get; set; }
    }

    [System.Runtime.Serialization.DataContract]
    public partial class WebHookStep
    {
        [System.Runtime.Serialization.DataMember]
        public string Name { get; set; }
        [System.Runtime.Serialization.DataMember]
        public StepStage Stage { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string EntityName { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string MessageName { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string[] FilteringAttributes { get; set; }
        [System.Runtime.Serialization.DataMember]
        public Collection<Image> Images { get; set; } = new Collection<Image>();
        [System.Runtime.Serialization.DataMember]
        public AttributeMode FilteredAttributeMode { get; set; }
        [System.Runtime.Serialization.DataMember]
        public int Rank { get; set; }
        [System.Runtime.Serialization.DataMember]
        public StepMode StepMode { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string Description { get; set; }
    }
    #endregion
}
