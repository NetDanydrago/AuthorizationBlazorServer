using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Policies
{
    public class RolePolicies
    {
        public const string SAdmin = "sadmin";

        public static AuthorizationPolicy SAdminPolicy()
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .RequireRole(SAdmin)
                .Build();
        }

    }
}
