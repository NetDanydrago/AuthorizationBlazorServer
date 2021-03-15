using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Shared
{
    public class ApiResourceViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Claim { get; set; }
        public string[] Scopes { get; set; }
    }
}
