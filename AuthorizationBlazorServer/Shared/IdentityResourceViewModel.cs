using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Shared
{
    public class IdentityResourceViewModel
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Claim { get; set; }
    }
}
