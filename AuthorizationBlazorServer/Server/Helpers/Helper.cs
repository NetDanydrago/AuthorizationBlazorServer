using AuthorizationBlazorServer.Shared;
using IdentityModel;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
                _ => GrantTypes.ClientCredentials
            };
            return Type;
        }

        public static Services.User ViewModelToUser(UserViewModel user)
        {
            Services.User User = new Services.User()
            {
                Id = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex),
                UserName = user.UserName,
                Password = new Secret(user.Password.Sha256()).Value,
                IsActive = true,
            };
            var UserClaims = new List<Services.UserClaim>()
            {
                new Services.UserClaim()
                {
                    UserId = User.Id,
                    ClaimName = "name",
                    ClaimValue = user.Name
                },
                new Services.UserClaim()
                {
                    UserId = User.Id,
                    ClaimName = "family_name",
                    ClaimValue = user.Name
                },
                new Services.UserClaim()
                {
                    UserId = User.Id,
                    ClaimName = "given_name",
                    ClaimValue = user.GivenName
                },
                new Services.UserClaim()
                {
                    UserId = User.Id,
                    ClaimName = "email",
                    ClaimValue = user.Email
                },
                new Services.UserClaim()
                {
                    UserId = User.Id,
                    ClaimName = "email_verified",
                    ClaimValue = "true"
                },
                  new Services.UserClaim()
                {
                    UserId = User.Id,
                    ClaimName = "website",
                    ClaimValue = user.WebSite
                },
            };
            if (!string.IsNullOrEmpty(user.Country))
            {
                UserClaims.Add(new Services.UserClaim()
                {
                    UserId = User.Id,
                    ClaimName = "address",
                    ClaimValue = JsonSerializer.Serialize(new {
                        street_address = user.Street,
                        locality = user.Locality,
                        postal_code = user.PostalCode,
                        country = user.Country
                    })
                });
            }
            if (user.Roles.Any())
            {
                foreach(var role in user.Roles)
                {
                    UserClaims.Add(new Services.UserClaim()
                    {
                        UserId = User.Id,
                        ClaimName = "role",
                        ClaimValue = role
                    });
                }
            }
            User.UserClaims = UserClaims;
            return User;        
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
            if (!string.IsNullOrEmpty(clientViewModel.UrlCors))
            {
                Client.AllowedCorsOrigins = new[] { clientViewModel.UrlCors };
            }
            Client.AllowedGrantTypes = Helper.GetGrantType(clientViewModel.GrantType);
            return Client;
        }
    }
}
