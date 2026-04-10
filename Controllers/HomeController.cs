using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly AuthHelper AuthHelper;
        private readonly IConfiguration Configuration;

        public HomeController(IConfiguration configuration, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            AuthHelper = new AuthHelper(configuration, environment, httpContextAccessor);
            Configuration = configuration;
        }

        [HttpGet]
        public Response<UserClaims> GetUserClaims()
        {
            UserClaims userProfile = AuthHelper.GetClaims();
            return new Response<UserClaims>(userProfile);
        }
    }
}