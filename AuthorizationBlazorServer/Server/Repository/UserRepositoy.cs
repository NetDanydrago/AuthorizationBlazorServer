using AuthorizationBlazorServer.Server.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthorizationBlazorServer.Server.Repository
{
    public class UserRepositoy
    {

        readonly UserContext Context;


        public UserRepositoy(UserContext dbContext)
        {
            Context = dbContext;
        }

        public async Task<bool> AddUser(Services.User user)
        {
            bool Result = false;
            await Context.AddAsync(user);
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

        public List<User> GetUsers()
        {
            List<User> Result = new List<User>();
            Result = Context.Users.ToList();
            return Result;
        }

        public async Task<bool> RemoveUser(string Id)
        {
            bool Result = false;
            var Entity = Context.Users.Where(x => x.Id == Id).FirstOrDefault();
            var Claims = Context.UserClaims.Where(x => x.UserId == Id).ToList();
            Context.UserClaims.RemoveRange(Claims);
            Context.Users.Remove(Entity);
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }
    }
}
