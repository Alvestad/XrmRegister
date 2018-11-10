using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister.Utility
{
    public class XrmSolution
    {
        public Guid Id { get; set; }
        public string UniqueName { get; set; }
        public Guid PublisherId { get; set; }
        public string PublisherName { get; set; }
        public string PublisherPrefix { get; set; }
    }
}
