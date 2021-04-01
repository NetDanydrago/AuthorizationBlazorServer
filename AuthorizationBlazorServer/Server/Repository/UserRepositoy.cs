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
            try
            {
                await Context.AddAsync(user);
                Result = await Context.SaveChangesAsync() > 0;
            }
            catch(Exception e)
            {
                var Message = e;
            }
            return Result;
        }

        public List<User> GetUsers()
        {
            List<User> Result = new List<User>();
            Result = Context.Users.ToList();
            return Result;
        }

        public User GetUserById(string id)
        {
            return Context.Users.Where
                (u => u.Id == id).FirstOrDefault();
        }

        public async Task<bool> UpdateUser(User user)
        {
            bool Result = false;
            Context.Users.Update(user);
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

        public async Task<bool> RemoveUser(string Id)
        {
            bool Result = false;
            var Entity = Context.Users.Where(x => x.Id == Id).
                Include(x => x.UserClaims).FirstOrDefault();
            Context.UserClaims.RemoveRange(Entity.UserClaims);
            Context.Users.Remove(Entity);
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }
    }
}
