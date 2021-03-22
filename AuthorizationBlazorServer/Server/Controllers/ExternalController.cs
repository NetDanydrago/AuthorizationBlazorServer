using AuthorizationBlazorServer.Server.Attributes;
using AuthorizationBlazorServer.Server.Extensions;
using AuthorizationBlazorServer.Server.Services;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [SecurityHeaders]
    [AllowAnonymous]
    public class ExternalController : Controller
    {
        // Servicio para que la UI se comunique con IdentityServer 
        // para validación y recuperación del contexto de autorización.
        private readonly IIdentityServerInteractionService Interaction;
        // Servicio para recuperar la configuración del cliente
        private readonly IClientStore ClientStore;
        // Servicio de Log de ASP.NET Core
        private readonly ILogger<ExternalController> Logger;
        // Servicio de eventos de IS para informar operaciones realizadas 
        // (Éxito, falla, categoría, detalle, etc.)
        private readonly IEventService Events;
        // Servicio que proporciona un almacén de usuarios de prueba.
        private readonly UserContext Users;

        public ExternalController(
             IIdentityServerInteractionService interaction,
             IClientStore clientStore,
             ILogger<ExternalController> logger,
             IEventService events,
             UserContext users = null) =>
             (Interaction, ClientStore, Logger, Events, Users) =
             (interaction, clientStore, logger, events, users);


        [HttpPost("Challenge")]
        public IActionResult Challenge([FromForm]string scheme, [FromForm] string returnUrl)
        {
            // Establecer la dirección de retorno del usuario
            // después de haber finalizado el proceso de autenticación.
            if (string.IsNullOrEmpty(returnUrl)) returnUrl = "~/";
            // Validar el returnUrl para ver si es un URL OIDC válido o 
            // de regreso a una página local.
            // IdentityServer "sabe" cuáles son los endpoints OIDC.
            if (Url.IsLocalUrl(returnUrl) == false &&
            Interaction.IsValidReturnUrl(returnUrl) == false)
            {
                // Nunca debería caer aquí.
                throw new Exception("Invalid return URL");
            }
            var Props = new AuthenticationProperties
            {
                // Endpoint al que queremos que sea redirigido el 
                // usuario después de haber sido autenticado por
                // el proveedor externo: ~/external/callback.
                // En ese endpoint finalizaremos el proceso de autenticación.
                RedirectUri = Url.Action(nameof(Callback)),
                    Items = {
                                {"returnUrl", returnUrl },
                                { "scheme", scheme}
                            }
            };
            // Iniciar el Challenge... ir al proveedor externo...
            return Challenge(Props, scheme);
        }

        (User User, string Provider, string ProviderUserId, IEnumerable<Claim>)
        FindUserFromExternalProvider(AuthenticateResult result)
        {
            var ExternalUser = result.Principal;
            // Obtener el Id del usuario asignado por el proveedor externo.
            // Normalmente al claim "sub" o "NameIdentifier" lo representan.
            // Lanzar una excepción si no es encontrado.
            var UserIdClaim = ExternalUser.FindFirst(JwtClaimTypes.Subject) ??
            ExternalUser.FindFirst(ClaimTypes.NameIdentifier) ??
            throw new Exception("Unknown UserId");
            // Eliminar el Claim para no incluirlo como un claim adicional 
            // al crear un nuevo usuario.
            var Claims = ExternalUser.Claims.ToList();
            Claims.Remove(UserIdClaim);
            // Obtener el esquema de autenticación y el identificador de usuario
            // para poder realizar la búsqueda.
            var Provider = result.Properties.Items["scheme"];
            var ProviderUserId = UserIdClaim.Value;
            // Buscar al usuario en nuestro almacén
            var User = Users.FindByExternalUserProvider(Provider, ProviderUserId);
            return (User, Provider, ProviderUserId, Claims);
        }

        User AutoProvisionUser(string provider, string providerUserId, IEnumerable<Claim> claims)
        {
            // Crea un nuevo usuario local asociado a una cuenta externa.
            var User = Users.AutoProvisionUser(
            provider, providerUserId, claims.ToList());
            return User;
        }

        private void ProcessLoginCallback(AuthenticateResult externalResult,
        List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
            // Si el sistema externo envió un Claim de Id de sesión, debemos 
            // copiarlo para poder utilizarlo durante el signout.
            Claim SessionIdClaim =
            externalResult.Principal.Claims.FirstOrDefault(
            c => c.Type == JwtClaimTypes.SessionId);
            if (SessionIdClaim != null)
            {
                localClaims.Add(
                new Claim(JwtClaimTypes.SessionId, SessionIdClaim.Value));
            }
            // Si el proveedor externo emitió un id_token, guardarlo para 
            // usarlo en el signout
            var TokenId =
            externalResult.Properties.GetTokenValue("id_token");
            if (TokenId != null)
            {
                localSignInProps.StoreTokens(new[]
                {
                        new AuthenticationToken
                        {
                            Name= "id_token", Value = TokenId
                        }
                 });
            }
        }

        [HttpGet("Callback")]
        public async Task<IActionResult> Callback()
        {
            // Generar una Cookie de autenticación temporal. 
            AuthenticateResult AuthenticateResult =
            await HttpContext.AuthenticateAsync(
            IdentityServerConstants.ExternalCookieAuthenticationScheme);

            if (AuthenticateResult?.Succeeded != true)
            {
                // No se puede autenticar al usuario
                throw new Exception("External authentication error");
            }
            // Se pudo autenticar al usuario. 
            // Mostrar los claims si está habilitado el nivel de log Debug.
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                var ExternalClaims = AuthenticateResult.Principal.Claims
                    .Select(c => $"{c.Type}: {c.Value}");
                Logger.LogDebug(
                "External Claims: {ExternalClaims}", ExternalClaims);
            }
            // Buscar la información local del usuario
            var (User, Provider, ProviderUserId, Claims) =
            FindUserFromExternalProvider(AuthenticateResult);
            // ¿Se encontró el usuario local mapeado al usuario externo?
            if (User == null)
            {
                // No se encontró al usuario. Crear su cuenta de usuario local.
                // Aquí podemos poner nuestra lógica para indicar al usuario que 
                // no está asociado a una cuenta local y dar instrucciones para 
                // registrar una cuenta.
                User = AutoProvisionUser(Provider, ProviderUserId, Claims);
            }
            // Recolectar Claims adicionales o propiedades de protocolos 
            // específicos utilizados y almacenarlas en la cookie de autenticación 
            // local. Esto se utiliza normalmente para almacenar los datos 
            // necesarios para cerrar sesión en esos protocolos.
            var AdditionalLocalClaims = new List<Claim>();
            var LocalSignInProps = new AuthenticationProperties();
            ProcessLoginCallback(
            AuthenticateResult, AdditionalLocalClaims, LocalSignInProps);
            // Emitir la cookie de autenticación para el usuario
            IdentityServerUser IdentityServerUser =
            new IdentityServerUser(User.Id)
            {
                DisplayName = User.UserName,
                IdentityProvider = Provider,
                AdditionalClaims = AdditionalLocalClaims
            };
            // SignInAsyc es una extension de IdentityServer. 
            // Debemos importar Microsoft.AspNetCore.Http
            await HttpContext.SignInAsync(IdentityServerUser, LocalSignInProps);
            // Eliminar la cookie temporal utilizada durante la 
            // autenticación externa
            await HttpContext.SignOutAsync(
            IdentityServerConstants.ExternalCookieAuthenticationScheme);
            // Obtener el Url de retorno
            var ReturnUrl =
            AuthenticateResult.Properties.Items["returnUrl"] ?? "~/";
            // Verificar si el login externo está en el contexto de 
            // una petición OIDC.
            var Context =
            await Interaction.GetAuthorizationContextAsync(ReturnUrl);
            // Registrar el SignIn exitoso
            await Events.RaiseAsync(new UserLoginSuccessEvent(
            Provider, ProviderUserId, User.Id, User.UserName, true,
            Context?.Client.ClientId));
            IActionResult Result = Redirect(ReturnUrl);
            if (Context != null)
            {
                if (Context.IsNativeClient())
                {
                    // El cliente es nativo (desktop/movíl). 
                    // Cambiar la forma de devolver la respuesta para mejorar
                    // la experiencia de usuario (UX) del usuario final.
                    Result = this.LoadingPage("Redirect", ReturnUrl);
                }
            }
            return Result;
        }


    }
}
