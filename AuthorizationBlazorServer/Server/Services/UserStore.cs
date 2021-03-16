using IdentityModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Services
{
    public class UserStore : DbContext
    {
        private DbSet<User> Users {get; set;}

        public UserStore(DbContextOptions<UserStore> options) : base(options)
        {
        }

        public bool ValidateCredentials(string username, string password)
        {
            bool Result = false;
            var User = FindByUsername(username);
            if (User != null)
            { 
                if (string.IsNullOrWhiteSpace(User.Password) && 
                    string.IsNullOrWhiteSpace(password))
                {
                    Result = true;
                }
                else
                {
                    Result = User.Password.Equals(password);
                }          
            }
            return Result;
        }

        public User FindByUserId(string id)
        {
            return Users.FirstOrDefault(x => x.Id == id);
        }

        public User FindByUsername(string username)
        {
            return Users.FirstOrDefault(x => x.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public User FindByExternalUserProvider(string provider, string userId)
        {
            return Users.FirstOrDefault(x =>
                x.ExternalName == provider &&
                x.ExternalId == userId);
        }


        public User AutoProvisionUser(string provider, string userId, List<Claim> claims)
        {
            //Crear una lista de claims que seran transferidos a nuestro contexto
            var filtered = new List<Claim>();

            foreach (var claim in claims)
            {
                //Si el provedor externo envia un nombre, sera mapeado al estandar OIDC (claim name)
                if (claim.Type == ClaimTypes.Name)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, claim.Value));
                }
                // if the JWT handler has an outbound mapping to an OIDC claim use that
                else if (JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.ContainsKey(claim.Type))
                {
                    filtered.Add(new Claim(JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[claim.Type], claim.Value));
                }
                // copy the claim as-is
                else
                {
                    filtered.Add(claim);
                }
            }

            //Si el nombre no fue proporcionada, tratar de construir en base al primer nombre o su apellido.
            if (!filtered.Any(x => x.Type == JwtClaimTypes.Name))
            {
                var first = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.GivenName)?.Value;
                var last = filtered.FirstOrDefault(x => x.Type == JwtClaimTypes.FamilyName)?.Value;
                if (first != null && last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
                }
                else if (first != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, first));
                }
                else if (last != null)
                {
                    filtered.Add(new Claim(JwtClaimTypes.Name, last));
                }
            }

            // Crear un unico id para el usuario en nuestro contexto 
            var sub = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);

            //Verificar si el nombre esta disponible, de lo contrario poner el id
            var name = filtered.FirstOrDefault(c => c.Type == JwtClaimTypes.Name)?.Value ?? sub;

            // Crear una nueva instancia del usuario
            var user = new User
            {
                Id = sub,
                UserName = name,
                ExternalName = provider,
                ExternalId = userId,
                Claims = filtered
            };

            // Agregar al Contexto
            Users.Add(user);

            return user;
        }

    }
}
