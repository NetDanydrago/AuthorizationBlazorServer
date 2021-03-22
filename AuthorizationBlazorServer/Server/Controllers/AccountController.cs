using AuthorizationBlazorServer.Server.Attributes;
using AuthorizationBlazorServer.Server.Extensions;
using AuthorizationBlazorServer.Server.Services;
using AuthorizationBlazorServer.Shared.Models;
using AuthorizationBlazorServer.Shared.ViewModels;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserContext Users;
        private readonly IIdentityServerInteractionService Interaction;
        private readonly IClientStore ClientStore;
        private readonly IAuthenticationSchemeProvider SchemeProvider;
        private readonly IEventService Events;

        public AccountController(
        IIdentityServerInteractionService interaction,
        IClientStore clientStore,
        IAuthenticationSchemeProvider schemeProvider,
        IEventService events,
         UserContext users)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            // this is where you would plug in your own custom identity management library (e.g. ASP.NET Identity)
            Users = users;
            Interaction = interaction;
            ClientStore = clientStore;
            SchemeProvider = schemeProvider;
            Events = events;
        }

        [HttpGet("Login")]
        public IActionResult Login(string returnUrl)
        {
            return Redirect($"~/identityserver4/Login?{returnUrl}");
        }

        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn(LoginViewModel loginViewModel)
        {
            ActionResult Result;
            // Construir el modelo de la vista
            var Model = await BuildLoginViewModelAsync(loginViewModel.ReturnUrl);
            // ¿La autenticación la debe hacer un proveedor externo?
            if (Model.IsExternalLoginOnly)
            {
                // La autenticación será externa, redirigir hacia el 
                // controlador ExternalController que crearemos
                // posteriormente.
                // El método Challenge es miembro de ControllerBase.
                Result = RedirectToAction(nameof(Challenge), "External", new
                { scheme = Model.ExternalLoginScheme, loginViewModel.ReturnUrl });
            }
            else
            {
                Result = Ok(Model);
            }
            return Result;
        }


        [HttpPost("Cancel")]
        public async Task<IActionResult> Cancel(LoginInputModel model)
        {
            IActionResult Result = null;
            // Verificar si estamos en el contexto de una petición de autorización
            AuthorizationRequest Context =
            await Interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            if (Context != null)
            {
                // Si el usuario canceló, enviar un resultado a IdentityServer 
                // como si se negara el consentimiento, aunque este cliente 
                // no requiera consentimiento. Esto enviará una respuesta de 
                // error OIDC Access Denied al cliente.
                await Interaction.DenyAuthorizationAsync(
                 Context, AuthorizationError.AccessDenied);
                // ¿El cliente es nativo?
                if (Context.IsNativeClient())
                {
                    // El cliente es nativo (desktop o móvil). 
                    // Cambiar la forma de devolver la respuesta para una mejor 
                    // UX del usuario final.
                    Result = this.LoadingPage("Redirection", model.ReturnUrl);
                }
                else
                {
                    Result = Ok(Redirect(model.ReturnUrl).Url);
                }
            }
            else
            {
                // No tenemos un contexto válido, simplemente regresamos
                // a la página Home
                Result = Ok(Redirect("~/").Url);
            }
            return Result;
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginInputModel model)
        {
            IActionResult Result = null;
            // Verificar si estamos en el contexto de una petición de autorización
            AuthorizationRequest Context =
            await Interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                // ¿Es un usuario válido?
                if (Users.ValidateCredentials(model.Username, model.Password))
                {
                    // Es un usuario válido. Obtener los datos del usuario 
                    // buscándolo por su nombre.
                    var User = Users.FindByUsername(model.Username);
                    // Lanzar un evento login exitoso para el log.
                    await Events.RaiseAsync(new UserLoginSuccessEvent(User.UserName, User.Id, User.UserName, clientId: Context?.Client.ClientId));

                    // Solo establecer expiración explicita si un usuario 
                    // selecciona "remember me", de otra forma, tomamos la 
                    // configuración del middleware Cookie.
                    AuthenticationProperties Props = null;
                    if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                    {
                        Props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                        };
                    };

                    // Emitir la cookie de autenticación con el 
                    // subject id y username.
                    var isuser = new IdentityServerUser(User.Id)
                    {
                        DisplayName = User.UserName
                    };

                    // Context almacena la configuración del usuario.
                    // ¿Hay información almacenada?
                    await HttpContext.SignInAsync(isuser, Props);

                    if (Context != null)
                    {
                        if (Context.IsNativeClient())
                        {
                            Result = this.LoadingPage("Redirect", model.ReturnUrl);
                        }
                        else
                        {
                            Result = Ok(Redirect(model.ReturnUrl).Url);

                        }
                    }
                    else
                    {
                        // Contexto = null 
                        if (Url.IsLocalUrl(model.ReturnUrl))
                        {
                            // Redireccionar a una página local.
                            Result = Ok(Redirect(model.ReturnUrl));
                        }
                        else if (string.IsNullOrEmpty(model.ReturnUrl))
                        {
                            Result = Ok(Redirect("~/"));
                        }
                        else
                        {
                            // Es un URL incorrecto. No debería suceder
                            throw new Exception("Invalid return URL");
                        }
                    }
                }
                else
                {
                    // Credenciales no válidas
                    await Events.RaiseAsync(new UserLoginFailureEvent(
                     model.Username, "Invalid Credentials",
                    clientId: Context?.Client.ClientId));

                }
            if (Result == null)
            {
                Result = BadRequest("Usuario O Contraseña Invalido");
            }
            return Result;
        }


        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout(string logoutId)
        {
            IActionResult Result;
            // Contruir el Modelo de la Vista
            LogoutViewModel Model =
            await BuildLogoutViewModelAsync(logoutId);

            if (Model.ShowLogoutPrompt)
            {
                Result = View(Model);
            }
            else
            {
                // Si la petición de Logout fue propiamente autenticada por 
                // IdentityServer entonces no necesitamos mostrar al usuario 
                // la página de confirmación de cerrar sesión, simplemente 
                // cerramos sesión invocando a Logout HTTP Post.
                Result = await Logout(Model);
            }
            return Result;
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Logout(LogoutInputModel logoutInputModel)
        {
            IActionResult Result;
            // Construir el modelo de la vista.
            var Model =
            await BuildLoggedOutViewModelAsync(logoutInputModel.LogoutId);
            if (User?.Identity.IsAuthenticated == true)
            {
                // Si el usuario está autenticado, eliminar la Cookie local.
                await HttpContext.SignOutAsync();
                // Lanzar un evento para el log.
                await Events.RaiseAsync(
                new UserLogoutSuccessEvent(
                User.GetSubjectId(), User.GetDisplayName()));
            }
            // Verificar si necesitamos hacer Signout de un proveedor de identidad.
            if (Model.TriggerExternalSignout)
            {
                // Construir un URL de retorno para que el proveedor externo pueda 
                // redirigir al usuario de regreso después de que haya cerrado su 
                // sesión. El Url de retorno será el HTTP GET Logout. Esto nos 
                // permitirá completar el proceso de cierre de sesión. 
                // Cuando llegue al Logout HTTP Get ya no estará autenticado 
                // localmente ya que el código anterior de este método hizo un 
                // SignOutAsync.
                string ReturnUrl = Url.Action(nameof(Logout),
                new { logoutId = Model.LogoutId });
                Result = SignOut(new AuthenticationProperties
                {
                    RedirectUri = ReturnUrl
                }, Model.ExternalAuthenticationScheme);
            }
            else
            {
                // Mostrar la página de sesión cerrada.
                Result  = Redirect(Model.PostLogoutRedirectUri);

            }
            return Result;
        }

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return Redirect($"~/identityserver4/AccessDenied");
        }



        #region Helpers Account
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            LoginViewModel LoginViewModel;
            // Obtener el contexto de autorización basándose en el 
            // Url de retorno pasado a Login.
            // AuthorizationRequest contiene el cliente que inició
            // la petición, el Uri de redireccionamiento, etc
            AuthorizationRequest Context =
                        await Interaction.GetAuthorizationContextAsync(returnUrl);
            // ¿Hay un identificador de proveedor de identidad?
            if (Context?.IdP != null && await SchemeProvider.GetSchemeAsync(Context.IdP) != null)
            {
                // ¿En un proveedor de identidad local el que autenticará?

                var IsLocalIdentityProvider = Context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // Construir el LoginViewModel
                LoginViewModel = new LoginViewModel
                {
                    EnableLocalLogin = IsLocalIdentityProvider,
                    ReturnUrl = returnUrl,
                    // El username que se espera que escriba el usuario.
                    // Se mostrará automáticamente en la página de login.
                    Username = Context.LoginHint,
                };

                // Construir el LoginViewModel
                LoginViewModel = new LoginViewModel
                {
                    EnableLocalLogin = IsLocalIdentityProvider,
                    ReturnUrl = returnUrl,
                    // El username que se espera que escriba el usuario.
                    // Se mostrará automáticamente en la página de login.
                    Username = Context.LoginHint,
                };
            }
            else
            {
                // No hay Identificador de proveedor de identidad.
                // Obtener la lista de esquemas configurados en la aplicación.
                IEnumerable<AuthenticationScheme> Schemes =
                await SchemeProvider.GetAllSchemesAsync();
                // Extraer de la lista de esquemas, la lista de proveedores
                List<ExternalProvider> Providers = Schemes
                .Where(s => s.DisplayName != null)
               .Select(s => new ExternalProvider
               {
                   DisplayName = s.DisplayName,
                   AuthenticationScheme = s.Name
               })
               .ToList();
                // Permitir login local
                bool AllowLocal = true;
                // ¿Hay un identificador de Cliente?
                if (Context?.Client.ClientId != null)
                {
                    // Obtener datos del Cliente por su Id y que esté habilitado.
                    IdentityServer4.Models.Client Client =
                     await ClientStore.FindEnabledClientByIdAsync(
                     Context.Client.ClientId);
                    if (Client != null)
                    {
                        AllowLocal = Client.EnableLocalLogin;
                        // ¿Hay restricciones de uso de proveedores de identidad 
                        // para el Cliente?
                        if (Client.IdentityProviderRestrictions != null &&
                         Client.IdentityProviderRestrictions.Any())
                        {
                            // Obtener los proveedores de identidad que puede 
                            // utilizar este cliente.
                            Providers = Providers
                             .Where(p => Client.IdentityProviderRestrictions
                            .Contains(p.AuthenticationScheme))
                            .ToList();
                        }
                    }
                }

                // Construimos el ViewModel
                LoginViewModel = new LoginViewModel
                {
                    AllowRememberLogin = AccountOptions.AllowRememberLogin,
                    EnableLocalLogin = AllowLocal &&
                AccountOptions.AllowLocalLogin,
                    ReturnUrl = returnUrl,
                    Username = Context?.LoginHint,
                    ExternalProviders = Providers
                };

            }

            return LoginViewModel;
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(
       LoginInputModel model)
        {
            LoginViewModel LoginViewModel =
            await BuildLoginViewModelAsync(model.ReturnUrl);
            LoginViewModel.Username = model.Username;
            LoginViewModel.RememberLogin = model.RememberLogin;
            return LoginViewModel;
        }


        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(
            string logoutId)
        {
            var LogoutViewModel = new LogoutViewModel
            {
                LogoutId = logoutId,
                ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt
            };
            if (User?.Identity.IsAuthenticated != true)
            {
                // Si el usuario no está autenticado solo mostrar la página 
                // de usuario con sesión cerrada y no mostrar la página de 
                // confirmación para cerrar sesión.
                LogoutViewModel.ShowLogoutPrompt = false;
            }
            else
            {
                // El usuario está autenticado.
                // Obtener el contexto de Logout, por ejemplo, el Uri de 
                // redireccionamiento.
                LogoutRequest Context =
                await Interaction.GetLogoutContextAsync(logoutId);
                // ¿Mostrar la página de cerrar sesión?
                if (Context?.ShowSignoutPrompt == false)
                {
                    // No mostrar, es seguro que puede cerrar sesión.
                    LogoutViewModel.ShowLogoutPrompt = false;
                }
            }
            return LogoutViewModel;
        }


        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // Obtener información del contexto (nombre del cliente, 
            // Uri de redireccionamiento, IFrame para signout federado, etc.)
            LogoutRequest Logout =
            await Interaction.GetLogoutContextAsync(logoutId);
            // Crear el ViewModel
            var LoggedOutViewModel = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut =
            AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = Logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(Logout?.ClientName) ?
                Logout?.ClientId : Logout?.ClientName,
                SignOutIframeUrl = Logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };
            if (User?.Identity.IsAuthenticated == true)
            {
                // Buscar el proveedor de identidad
                var Idp = User.FindFirst(
                JwtClaimTypes.IdentityProvider)?.Value;
                if (Idp != null &&
                Idp != IdentityServerConstants.LocalIdentityProvider)
                {
                    // Está cerrando sesión de un proveedor de identidad 
                    // externo. 
                    // ¿El proveedor soporta SignOut?
                    var ProviderSupportsSignout =
                     await HttpContext.GetSchemeSupportsSignOutAsync(Idp);
                    if (ProviderSupportsSignout)
                    {
                        if (LoggedOutViewModel.LogoutId == null)
                        {
                            // No hay contexto de logout actual, necesitamos 
                            // crear uno para capturar los datos necesarios 
                            // del usuario actual autenticado antes de cerrar 
                            // sesión y dirigir al usuario al sitio del 
                            // proveedor externo para cerrar sesión también ahí.
                            LoggedOutViewModel.LogoutId =
                             await Interaction.CreateLogoutContextAsync();
                        }
                        LoggedOutViewModel.ExternalAuthenticationScheme = Idp;
                    }
                }
            }
            return LoggedOutViewModel;
        }
        #endregion
    }
}
