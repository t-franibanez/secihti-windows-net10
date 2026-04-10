using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using SECIHTI.Common;
using SECIHTI.Models.Common;

namespace SECIHTI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    [Authorize]
    [EnableCors("AllowedOrigins")]
    public class HomeController : ControllerBase
    {
        private readonly AuthHelper _authHelper;

        public HomeController(AuthHelper authHelper)
        {
            _authHelper = authHelper;
        }

        [HttpGet]
        public Response<UserClaims> GetUserClaims()
        {
            var userProfile = _authHelper.GetClaims();
            return new Response<UserClaims>(userProfile);
        }
    }
}