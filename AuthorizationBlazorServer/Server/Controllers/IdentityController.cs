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

namespace AuthorizationBlazorServer.Server.Controllers
{
    [Route("api/[controller]")]
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
        public async Task<ActionResult> RegisterClient(UserViewModel userViewModel)
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
        #endregion

        #region HttpGet
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
