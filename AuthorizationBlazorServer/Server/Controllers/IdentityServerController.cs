using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using IdentityServer4.EntityFramework.DbContexts;
using System.Threading.Tasks;
using IdentityServer4.Models;
using AuthorizationBlazorServer.Server.Repository;
using AuthorizationBlazorServer.Shared;
using AuthorizationBlazorServer.Server.Helpers;
using Microsoft.EntityFrameworkCore;
using AuthorizationBlazorServer.Server.Services;
using Microsoft.AspNetCore.Authorization;

namespace AuthorizationBlazorServer.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityServerController : ControllerBase
    {

        private readonly Repository.Repository Repository;
        public IdentityServerController(ConfigurationDbContext dbContext)
        {
            Repository = new Repository.Repository(dbContext);
        }

        #region HttpPost
        [HttpPost]
        public async Task<IActionResult> DefaultIdentityResources()
        {
            ActionResult Result = Ok();
           var IdentityResources = new List<IdentityResource>
                            {
                                new IdentityResources.OpenId(),
                                new IdentityResources.Profile(),
                                new IdentityResources.Address(),
                                new IdentityResources.Email(),
                            };
            bool IsSuccess = await Repository.AddRangeIdentityResource(IdentityResources);
            if (!IsSuccess)
            {
                Result = Problem("Error to add IdentityResource", null, 500, "", null);
            }
            return Result;
        }

        [HttpPost("Client")]
        public async Task<ActionResult> RegisterClient(ClientViewModel clientViewModel)
        {
            ActionResult Result = Ok();
            var Client = Helper.ViewModelToClient(clientViewModel);
            bool IsSuccess = await Repository.AddClient(Client);
            if (!IsSuccess)
            {
                Result = Problem("Error to add client",null,500,"",null);
            }
            return Result;
        }

        [HttpPost("ApiScope")]
        public async Task<ActionResult> RegisterScope(ScopeViewModel scopeViewModel)
        {
            ActionResult Result = Ok();
            IdentityServer4.Models.ApiScope Scope = new IdentityServer4.Models.ApiScope 
            {
                Name = scopeViewModel.Name,
                DisplayName = scopeViewModel.DisplayName
            };
            bool IsSuccess = await Repository.AddApiScope(Scope);
            if (!IsSuccess)
            {
                Result = Problem("Error to add scope", null, 500, "", null);
            }
            return Result;
        }

        [HttpPost("ApiResource")]
        public async Task<ActionResult> RegisterApiResouce(ApiResourceViewModel resourceViewModel)
        {
            ActionResult Result = Ok();
            IdentityServer4.Models.ApiResource Resource = new ApiResource
                (resourceViewModel.Name,resourceViewModel.DisplayName);
            if (!string.IsNullOrEmpty(resourceViewModel.Claim))
            {
                Resource.UserClaims = new[] { resourceViewModel.Claim };
            }
            Resource.Scopes = resourceViewModel.Scopes;
            bool IsSuccess = await Repository.AddApiResource(Resource);
            if (!IsSuccess)
            {
                Result = Problem("Error to add ApiResource", null, 500, "", null);
            }
            return Result;
        }

        [HttpPost("IdentityResource")]
        public async Task<ActionResult> RegisterApiIdentityResouce(IdentityResourceViewModel resourceViewModel)
        {
            ActionResult Result = Ok();
            IdentityServer4.Models.IdentityResource Resource = new IdentityResource
             (resourceViewModel.Name, resourceViewModel.DisplayName, 
             new [] { resourceViewModel.Claim });
            bool IsSuccess = await Repository.AddIdentityResource(Resource);
            if (!IsSuccess)
            {
                Result = Problem("Error to add IdentityResource", null, 500, "", null);
            }
            return Result;
        }

        #endregion

        #region HttpGet

        [HttpGet("ApiResouces")]
        public List<ApiResourceViewModel> GetApiResouces()
        {
            List<ApiResourceViewModel> Result = new List<ApiResourceViewModel>();
            List<ApiResource> ApiResources  = Repository.GetApiResources();
            if(ApiResources.Count > 0)
            {
                Result = ApiResources.ConvertAll(x => new ApiResourceViewModel()
                {
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                });
            }
            return Result;
        }

        [HttpGet("IdentityResource")]
        public ActionResult<List<IdentityResourceViewModel>> GetIdentityResources()
        {
            List<IdentityResourceViewModel> Result = new List<IdentityResourceViewModel>();
            List<IdentityResource> IdentityResources = Repository.GetIdentityResources();
            if (IdentityResources.Count > 0)
            {
                Result = IdentityResources.ConvertAll(x => new IdentityResourceViewModel()
                {
                    Name = x.Name,
                    DisplayName = x.DisplayName
                });
            }
            return Result;
        }
        
        [HttpGet("ApiScope")]
        public ActionResult<List<ScopeViewModel>> GetApiScopes()
        {
            List<ScopeViewModel> Result = new List<ScopeViewModel>();
            List<ApiScope> Scopes = Repository.GetApiScopes();
            if(Scopes.Count > 0)
            {
                Result = Scopes.ConvertAll(x => new ScopeViewModel()
                {
                    Name = x.Name,
                    DisplayName = x.DisplayName
                });
            }
            return Result;
        }

        [HttpGet("Client")]
        public ActionResult<List<ClientViewModel>> GetClients()
        {
            List<ClientViewModel> Result = new List<ClientViewModel>();
            List<IdentityServer4.Models.Client> Clients = Repository.GetClients();
            if (Clients.Count > 0)
            {
                Result = Clients.ConvertAll(x => new ClientViewModel()
                {
                    Name = x.ClientId,
                    Scopes = x.AllowedScopes.ToArray(),          
                });
            }
            return Result;
        }
        #endregion

        #region HttpDelete


        [HttpDelete("ApiResource/{name}")]
        public async Task<IActionResult> DeleteApiResources(string name)
        {
            ActionResult Result = Ok();
            bool IsSuccess = await Repository.RemoveApiResource(name);
            if (!IsSuccess)
            {
                Result = Problem("Error to delete ApiResource", null, 500, "", null);
            }
            return Result;
        }

        [HttpDelete("IdentityResource/{name}")]
        public async Task<IActionResult> DeleteIdentityResources(string name)
        {
            ActionResult Result = Ok();
            bool IsSuccess = await Repository.RemoveIdentityResource(name);
            if (!IsSuccess)
            {
                Result = Problem("Error to add IdentityResource", null, 500, "", null);
            }
            return Result;
        }
        [HttpDelete("ApiScope/{name}")]
        public async Task<IActionResult> DeleteApiScope(string name)
        {
            ActionResult Result = Ok();
            bool IsSuccess = await Repository.RemoveApiScope(name);
            if (!IsSuccess)
            {
                Result = Problem("Error to delete apiscope", null, 500, "", null);
            }
            return Result;
        }
        [HttpDelete("Client/{clientid}")]
        public async Task<IActionResult> DeleteClient(string clientid)
        {
            ActionResult Result = Ok();
            bool IsSuccess = await Repository.RemoveClient(clientid);
            if (!IsSuccess)
            {
                Result = Problem("Error to delete client", null, 500, "", null);
            }
            return Result;
        }
        #endregion
    }
}
