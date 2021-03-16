using IdentityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Services
{
    /// <summary>
    /// Manejar contexto de usuarios personalizado basado en TestUser de IdentityServer4
    /// </summary>
    public class User
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ExternalName { get; set;}
        public string ExternalId { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Claim> Claims { get; set; } = new HashSet<Claim>(new ClaimComparer());

    }
}
