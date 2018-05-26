
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrmRegister.Connection
{
    public class Connection
    {
        static IOrganizationService _service;
        private static readonly Lazy<CrmConnection> _connection = new Lazy<CrmConnection>(() => new CrmConnection(), false);
        public static IOrganizationService ServiceByString(string connectionString)
        {
            _service = _connection.Value.GetServiceByConnectionString(connectionString);
            return _service;
        }
    }
}
