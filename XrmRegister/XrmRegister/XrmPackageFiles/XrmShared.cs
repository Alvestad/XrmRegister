using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister
{
    public enum StepStage
    {
        PreValidation = 10,
        PreOperation = 20,
        PostOperation = 40
    }

    public enum StepDeployment
    {
        ServerOnly = 0,
        OfflineOnly = 1,
        Both = 2
    }

    public enum StepMode
    {
        Asynchronous = 1,
        Synchronous = 0
    }

    public enum StepInvocationSource
    {
        Parent = 0,
        Child = 1
    }

    public enum ImageType
    {
        PreImage = 0,
        PostImage = 1,
        Both = 2
    }

    public enum AttributeMode
    {
        Include = 0,
        Exclude = 1
    }

    [System.Runtime.Serialization.DataContract]
    public partial class Image
    {
        public Image(params string[] attributes)
        {
            Attributes = attributes;
        }

        [System.Runtime.Serialization.DataMember]
        public ImageType ImageType { get; set; }

        [System.Runtime.Serialization.DataMember]
        public string Name { get; set; }
        [System.Runtime.Serialization.DataMember]
        public string[] Attributes { get; set; }

        [System.Runtime.Serialization.DataMember]
        public AttributeMode AttributeMode { get; set; }

    }
}
