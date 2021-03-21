using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Services
{
    public class UserClaim
    {
        public int Id { get; set; }
        public string ClaimName { get; set; }
        public string ClaimValue { get; set; }
        public string UserId { get; set; }
    }
}
