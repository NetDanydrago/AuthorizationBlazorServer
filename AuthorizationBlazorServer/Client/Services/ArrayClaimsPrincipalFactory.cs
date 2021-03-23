using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Client.Services
{
    public class ArrayClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount>
    {
        // Este constructor es requerido.
        public ArrayClaimsPrincipalFactory(
        IAccessTokenProviderAccessor accessor) : base(accessor)
        {
        }
        // Sobrescribir el método que crea el ClaimsPrincipal
        public override async ValueTask<ClaimsPrincipal> CreateUserAsync(
        RemoteUserAccount account, RemoteAuthenticationUserOptions options)
        {
            // Crear el ClaimsPrincipal predeterminado
            var User = await base.CreateUserAsync(account, options);
            // ¿El usuario está autenticado?
            if (User.Identity.IsAuthenticated)
            {
                // Obtener los Claims generados
                var ClaimsIdentity = (ClaimsIdentity)User.Identity;
                // Utilizaremos este diccionario para almacenar los 
                // claims que son arreglos y sus elementos como nuevos
                // claims.
                // Esto nos permitirá eliminar claims originales y 
                // agregar los nuevos claims generados.
                var ArrayClaims = new Dictionary<Claim, Claim[]>();
                // Recorrer los claims del usuario
                foreach (var Claim in ClaimsIdentity.Claims)
                {
                    // Intentar convertir el valor del claim en un elemento JSON
                    try
                    {
                        JsonElement? Element =
                        JsonSerializer
                       .Deserialize<JsonElement>(Claim.Value);
                        if (Element != null &&
                        Element.Value.ValueKind ==
                       JsonValueKind.Array)
                        {
                            // Es un claim de arreglo.
                            // Agregar el claim al Diccionario junto con
                            // sus elementos como claims.
                            ArrayClaims.Add(Claim,
                             Element.Value.EnumerateArray()
                             .Select(s =>
                             new Claim(Claim.Type,
                            s.GetString())).ToArray());
                        }
                    }
                    catch { }
                }
                // Eliminar Claims procesados y agregar los nuevos claims
                foreach (var Claim in ArrayClaims)
                {
                    ClaimsIdentity.RemoveClaim(Claim.Key);
                    ClaimsIdentity.AddClaims(Claim.Value);
                }
            }
            return User;
        }
    }
}
