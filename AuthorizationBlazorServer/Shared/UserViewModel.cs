using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Shared
{
    public class UserViewModel
    {
       
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public string Password { get; set; }
        public string GivenName { get; set; }
        public string Email { get; set; }
        public string WebSite { get; set; }
        public string Street { get; set; }
        public string Locality { get; set; }
        public int PostalCode { get; set; }
        public string Country { get; set; }
        public List<string> Roles { get; set; }
    }
}
