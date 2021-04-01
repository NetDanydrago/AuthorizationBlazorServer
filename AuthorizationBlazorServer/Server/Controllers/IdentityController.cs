using AuthorizationBlazorServer.Server.Services;
using AuthorizationBlazorServer.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorizationBlazorServer.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using AuthorizationBlazorServer.Server.Policies;
using IdentityModel;
using IdentityServer4.Models;

namespace AuthorizationBlazorServer.Server.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = RolePolicies.SAdmin)]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private readonly Repository.UserRepositoy Repository;

        public IdentityController(UserContext dbContext)
        {
            Repository = new Repository.UserRepositoy(dbContext);
        }

        #region HttpPost
        [HttpPost("User")]
        public async Task<ActionResult> RegisterUser(UserViewModel userViewModel)
        {
            ActionResult Result = Ok();
            var User = Helper.ViewModelToUser(userViewModel);
            bool IsSuccess = await Repository.AddUser(User);
            if (!IsSuccess)
            {
                Result = Problem("Error to add client", null, 500, "", null);
            }
            return Result;
        }

        [AllowAnonymous]
        [HttpPost("UserStepOne")]
        public async Task<ActionResult> RegisterUserStepOne(UserViewModel userViewModel)
        {
            ActionResult Result;
            var User = new User()
            {
                Id = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex),
                UserName = userViewModel.UserName,
                Password = new Secret(userViewModel.Password.Sha256()).Value,
                IsActive = false,
                ValidationCode = Guid.NewGuid().ToString()
            };
            bool IsSuccess = await Repository.AddUser(User);
            if (IsSuccess)
            {
                //Enviar Correo Metodo
                //SendEmail(User.UserName,User.Id,User.ValidationCode)
                Result = Ok(User.Id);
            }
            else
            {
                Result = Problem("Error to add client", null, 500, "", null);
            }
            return Result;
        }
        #endregion

        #region HttpGet
        [AllowAnonymous]
        [HttpGet("VerifiedEmail")]
        public async Task<ActionResult> VerifiedEmail([FromQuery]string id,[FromQuery]string code)
        {
            ActionResult Result = BadRequest();
            var User = Repository.GetUserById(id);
            if(User?.ValidationCode == code)
            {
                User.IsActive = true;
                User.ValidationCode = string.Empty;
                var IsSuccess = await Repository.UpdateUser(User);
                if (IsSuccess)
                {
                    Result = Redirect($"~/IdentityServer4/AccountStepTwo/{User.Id}/{User.UserName}");
                }
            }
            return Result;
        }

        [HttpGet("Users")]
        public List<UserViewModel> GetApiResouces()
        {
            List<UserViewModel> Result = new List<UserViewModel>();
            List<User> Users = Repository.GetUsers();
            Result = Users.ConvertAll(x => new UserViewModel()
            {
                Id = x.Id,
                UserName = x.UserName,
            });
            return Result;
        }
        #endregion

        #region HttpDelete
        [HttpDelete("User/{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            ActionResult Result = Ok();
            bool IsSuccess = await Repository.RemoveUser(id);
            if (!IsSuccess)
            {
                Result = Problem("Error to delete client", null, 500, "", null);
            }
            return Result;
        }
        #endregion
    }
}
