using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Services
{
    public class UserProfileService : IProfileService
    {
        protected readonly ILogger Logger;

        protected readonly UserContext Users;

        public UserProfileService(UserContext users, ILogger<UserProfileService> logger)
        {
            Users = users;
            Logger = logger;
        }

        /// <summary>
        /// Este metodo es llamado cuando un claim del usuario es requerido
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.LogProfileRequest(Logger);
            if(context.RequestedClaimTypes.Any())
            {
                var User = Users.FindByUserId(
                    context.Subject.GetSubjectId());
                if(User != null)
                {
                    context.AddRequestedClaims(User.UserClaims.ConvertAll
                    (x => new Claim(x.ClaimName, x.ClaimValue)));
                }
                context.LogIssuedClaims(Logger);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Este metodo es llamado para cuando el servidor de identity server necesita determinar
        /// si el usuario el valido o esta activo.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task IsActiveAsync(IsActiveContext context)
        {
            Logger.LogDebug("IsActive called from: {caller}", context.Caller);
            var User = Users.FindByUserId(context.Subject.GetSubjectId());
            context.IsActive = User?.IsActive == true;
            return Task.CompletedTask;
        }
    }
}
