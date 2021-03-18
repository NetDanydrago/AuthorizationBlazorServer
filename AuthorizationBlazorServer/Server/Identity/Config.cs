using System;
using System.Collections.Generic;
using IdentityServer4.Models;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Test;

namespace IdentityServerApp.Identity
{
    public static class Config
    {

        public static IEnumerable<ApiScope> ApiScopes =>
           new List<ApiScope>
           {
                new ApiScope("weatherapi.read","Read Weather API"),
                new ApiScope("weatherapi.write","Write Weather API"),
           };

        public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Address(),
                new IdentityResources.Email(),
                new IdentityResource("roles","User Roles",new string[] {"group"})

        };


        public static IEnumerable<ApiResource> ApiResources =>
            new List<ApiResource>
            {
                new ApiResource("api://weatherapi", "WeatherApi",new[]{ "role"})
                {
                    Scopes =
                    {
                        "weatherapi.read","weatherapi.write"
                    }
                }
            };

        public static IEnumerable<Client> Clients =>
            new List<Client>()
            {
                new Client
                {
                    ClientId = "ClientApp",
                    ClientSecrets =
                    {
                        new Secret("Secret".Sha256())
                    },
                    AllowedScopes =
                    {
                         "weatherapi.read","weatherapi.write"
                    },
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPasswordAndClientCredentials,
                },
                new Client
                {
                    ClientId = "MVCClientApp",
                    ClientSecrets =
                    {
                        new Secret("Secret".Sha256())
                    },
                    AllowedScopes =
                    {
                         IdentityServerConstants.StandardScopes.OpenId,
                         IdentityServerConstants.StandardScopes.Profile,
                         IdentityServerConstants.StandardScopes.Email,
                         IdentityServerConstants.StandardScopes.Address,
                         "roles",
                         "weatherapi.read",
                         "weatherapi.write",
                    },
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris =
                    {
                        "https://localhost:44302/signin-oidc"
                    },
                    PostLogoutRedirectUris =
                    {
                        "https://localhost:44302/signout-callback-oidc"
                    },
                    AlwaysIncludeUserClaimsInIdToken = true,
                    AllowOfflineAccess = true
                }
                ,
                new Client
                {
                    ClientId = "JavaScriptClient",
                    ClientName = "JavaScriptClient",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RedirectUris = {"https://localhost:44303/callback.html" },
                    PostLogoutRedirectUris ={"https://localhost:44303/index.html" },
                    AllowedScopes = {
                         IdentityServerConstants.StandardScopes.OpenId,
                         IdentityServerConstants.StandardScopes.Profile,
                         "weatherapi.read",
                         "weatherapi.write"
                    },
                    AllowedCorsOrigins =
                    {
                        "https://localhost:44303"
                    }
                },
                     new Client
                {
                    ClientId = "BlazorWebAssemblyClient",
                    ClientName = "BlazorWebAssemblyClient",
                    AllowedGrantTypes = GrantTypes.Code,
                    RequireClientSecret = false,
                    RedirectUris = {"https://localhost:44304/authentication/login-callback" },
                    PostLogoutRedirectUris ={"https://localhost:44304" },
                    AllowedScopes = {
                         IdentityServerConstants.StandardScopes.OpenId,
                         IdentityServerConstants.StandardScopes.Profile,
                         "weatherapi.read",
                         "weatherapi.write"
                    },
                    AllowedCorsOrigins =
                    {
                        "https://localhost:44304"
                    }
                }
            };
    }
}
