using AuthorizationBlazorServer.Shared;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Helpers
{
    public  static class Helper
    {
        public static ICollection<string> GetGrantType(Flows flow)
        {
           ICollection<string> Type = flow switch
            {
                Flows.CodeAndClientCredentials => GrantTypes.CodeAndClientCredentials,
                Flows.DeviceFlow => GrantTypes.DeviceFlow,
                Flows.Hybrid => GrantTypes.Hybrid,
                Flows.HybridAndClientCredentials => GrantTypes.HybridAndClientCredentials,
                Flows.ResourceOwnerPassword => GrantTypes.ResourceOwnerPassword,
                Flows.ResourceOwnerPasswordAndClientCredentials => GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                Flows.Code => GrantTypes.Code,
                Flows.ClientCredentials => GrantTypes.ClientCredentials
            };
            return Type;
        }

        public static IdentityServer4.Models.Client ViewModelToClient(ClientViewModel clientViewModel)
        {
            IdentityServer4.Models.Client Client = new IdentityServer4.Models.Client
            {
                ClientId = clientViewModel.Name,
                ClientName = clientViewModel.Name,
                AllowedScopes = clientViewModel.Scopes,
                AlwaysIncludeUserClaimsInIdToken = clientViewModel.IsIncludeIDToken,
                AllowOfflineAccess = clientViewModel.IsIncludeRefreshToken
            };
            if (clientViewModel.IsPublicClient)
            {
                Client.RequireClientSecret = false;
            }
            else
            {
                Client.ClientSecrets = new[] 
                { new Secret(clientViewModel.Secret.Sha256())};
            }
            if(!string.IsNullOrEmpty(clientViewModel.PostLogoutRedirectUri)
                & !string.IsNullOrEmpty(clientViewModel.RedirectUri))
            {
                Client.RedirectUris = new[]{ clientViewModel.RedirectUri };
                Client.PostLogoutRedirectUris = new[] { clientViewModel.PostLogoutRedirectUri};
            }
            Client.AllowedGrantTypes = Helper.GetGrantType(clientViewModel.GrantType);
            return Client;
        }
    }
}
