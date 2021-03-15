using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;

namespace AuthorizationBlazorServer.Server.Repository
{
    public class Repository
    {
        readonly ConfigurationDbContext Context;
        public Repository(ConfigurationDbContext context)
        {
            Context = context;
        }

        public async Task<bool> AddClient(IdentityServer4.Models.Client client)
        {
            bool Result = false;
            try
            {
                await Context.Clients.AddAsync(client.ToEntity());
                Result = await Context.SaveChangesAsync() > 0;
            }
            catch (Exception E)
            {
                var Message = E.Message;
            }
            return Result;
        }

        public async Task<bool> AddApiScope(IdentityServer4.Models.ApiScope scope)
        {
            bool Result = false;
            await Context.ApiScopes.AddAsync(scope.ToEntity());
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

        public async Task<bool> AddApiResource(IdentityServer4.Models.ApiResource resource)
        {
            bool Result = false;
            await Context.ApiResources.AddAsync(resource.ToEntity());
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

        public async Task<bool> AddIdentityResource(IdentityServer4.Models.IdentityResource resource)
        {
            bool Result = false;
            await Context.IdentityResources.AddAsync(resource.ToEntity());
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

        public async Task<bool> AddRangeIdentityResource(List<IdentityServer4.Models.IdentityResource> resources)
        {
            bool Result = false;
            await Context.IdentityResources.AddRangeAsync(resources.Select(a => a.ToEntity()).ToList());
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

        public  List<IdentityResource> GetIdentityResources()
        {
            List<IdentityServer4.Models.IdentityResource> Result = new List<IdentityServer4.Models.IdentityResource>();
            Result = Context.IdentityResources.ToList().ConvertAll(x => x.ToModel());
            return Result;
        }

        public List<ApiResource> GetApiResources()
        {
            List<IdentityServer4.Models.ApiResource> Result = new List<IdentityServer4.Models.ApiResource>();
            Result = Context.ApiResources.ToList().ConvertAll(x => x.ToModel());
            return Result;
        }

        public List<ApiScope> GetApiScopes()
        {
            List<IdentityServer4.Models.ApiScope> Result = new List<IdentityServer4.Models.ApiScope>();
            Result = Context.ApiScopes.ToList().ConvertAll(x => x.ToModel());
            return Result;
        }

        public List<IdentityServer4.Models.Client> GetClients()
        {
            List<IdentityServer4.Models.Client> Result = new List<IdentityServer4.Models.Client>();
            Result = Context.Clients.ToList().ConvertAll(x => x.ToModel());
            return Result;
        }

        public async Task<bool> RemoveClient(string clientId)
        {
            bool Result = false;
            var Entity = Context.Clients.Where(x => x.ClientId == clientId).FirstOrDefault();
            Context.Clients.Remove(Entity);
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

        public async Task<bool> RemoveApiScope(string name)
        {
            bool Result = false;
            var Entity = Context.ApiScopes.Where(x => x.Name == name).FirstOrDefault();
            Context.ApiScopes.Remove(Entity);
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

        public async Task<bool> RemoveApiResource(string name)
        {
            bool Result = false;
            var Entity = Context.ApiResources.Where(x => x.DisplayName == name).FirstOrDefault();
            Context.ApiResources.Remove(Entity);
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }


        public async Task<bool> RemoveIdentityResource(string name)
        {
            bool Result = false;
            var Entity = Context.IdentityResources.Where(x => x.Name == name).FirstOrDefault();
            Context.IdentityResources.Remove(Entity);
            Result = await Context.SaveChangesAsync() > 0;
            return Result;
        }

    }
}
