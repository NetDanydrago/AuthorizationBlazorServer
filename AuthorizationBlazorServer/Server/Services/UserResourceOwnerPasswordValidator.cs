using IdentityModel;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Services
{
    /// <summary>
    /// Validar usuarios para el flujo Resource Owner Password
    /// </summary>
    public class UserResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly UserContext Users;
        private readonly ISystemClock Clock;

        public UserResourceOwnerPasswordValidator(UserContext users, ISystemClock clock)
        {
            Users = users;
            Clock = clock;
        }

        public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            if (Users.ValidateCredentials(context.UserName, context.Password))
            {
                var user = Users.FindByUsername(context.UserName);
                context.Result = new GrantValidationResult(
                    user.Id ?? throw new ArgumentException("ID not set", nameof(user.Id)),
                    OidcConstants.AuthenticationMethods.Password, Clock.UtcNow.UtcDateTime,
                    user.Claims);
            }
            return Task.CompletedTask;
        }
    }
}
