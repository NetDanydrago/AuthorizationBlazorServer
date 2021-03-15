using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Shared
{
    public enum Flows
    {
        Code = 0,
        ClientCredentials = 1,
        CodeAndClientCredentials = 2,
        DeviceFlow = 3,
        Hybrid = 4,
        HybridAndClientCredentials = 5,
        ResourceOwnerPassword = 6,
        ResourceOwnerPasswordAndClientCredentials = 7
    }

    public class ClientViewModel
    {
        public string Name { get; set; }
        public string Secret { get; set; }
        public string[] Scopes { get; set; }
        public Flows GrantType { get; set; }
        public bool IsPublicClient { get; set; }
        public string RedirectUri { get; set; }
        public string PostLogoutRedirectUri { get; set; }
        public bool IsIncludeIDToken { get; set; }
        public bool IsIncludeRefreshToken { get; set; }
        public bool IsEnableCors { get; set; }

    }
}
