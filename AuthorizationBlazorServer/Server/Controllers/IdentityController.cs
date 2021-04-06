using AuthorizationBlazorServer.Server.Services;
using AuthorizationBlazorServer.Shared;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthorizationBlazorServer.Server.Helpers;
using Microsoft.AspNetCore.Authorization;
using AuthorizationBlazorServer.Server.Policies;
using IdentityModel;
using IdentityServer4.Models;
using System.Linq;

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
        [HttpGet("ResendEmail/{id}")]
        public ActionResult ResendEmail(string id)
        {
            ActionResult Result = BadRequest();
            var User = Repository.GetUserById(id);
            if(User != null)
            {
               var WasVerified  = User.UserClaims.Find
                         (x => x.ClaimName == "email_verified").ClaimValue;
                if (WasVerified == null)
                {
                    //Enviar Correo Metodo
                    //SendEmail(User.UserName,User.Id,User.ValidationCode)
                    Result = Ok("El correo fue renviado");
                }
                else if (WasVerified.Equals("true"))
                {
                    Result = Problem("El correo ya fue verificado");
                }
              
            }
            return Result;
        }


        [AllowAnonymous]
        [HttpGet("VerifiedEmail")]
        public async Task<ActionResult> VerifiedEmail([FromQuery]string id,[FromQuery]string code)
        {
            ActionResult Result = BadRequest();
            var User = Repository.GetUserById(id);
            if(User?.ValidationCode == code)
            {
                var UserClaims = new List<UserClaim>()
                {
                      new UserClaim(){ UserId = User.Id,
                        ClaimName = "email_verified",
                        ClaimValue = "true"},
                    new UserClaim(){ UserId = User.Id,
                        ClaimName = "email",
                        ClaimValue = User.UserName},          
                };
                var IsSuccess = await Repository.AddUserClaims(UserClaims);
                if (IsSuccess)
                {
                    Result = Redirect($"~/IdentityServer4/AccountStepTwo/{User.Id}");
                }
            }
            return Result;
        }

        [AllowAnonymous]
        [HttpGet("User/{id}")]
        public ActionResult GetUser(string id)
        {
            ActionResult Result = NotFound();
            var User = Repository.GetUserById(id);
            if (User != null)
            {
                var WasVerified = User.UserClaims.Find
                         (x => x.ClaimName == "email_verified").ClaimValue;
                if (WasVerified.Equals("true"))
                {
                    Result = Ok(Helper.UserToUserViewModel(User));
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
