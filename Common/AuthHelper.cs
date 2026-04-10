using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SECIHTI.Models.Common;

namespace SECIHTI.Common
{
    public class AuthHelper
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthHelper> _logger;

        public AuthHelper(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<AuthHelper> logger)
        {
            _config = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public UserClaims GetClaims()
        {
            var userClaims = new UserClaims();

            try
            {
                if (_config.GetValue<bool>("Auth:BypassSaml"))
                {
                    userClaims.PersonID = _config["UserImpersonation:PersonID"];
                    userClaims.UserType = _config["UserImpersonation:UserType"];
                    userClaims.PayrollID = _config["UserImpersonation:PayrollID"];
                    userClaims.Email = _config["UserImpersonation:Email"];
                    userClaims.Profiles = _config["UserImpersonation:Profiles"];
                }
                else if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
                {
                    var claims = _httpContextAccessor.HttpContext.User.Identities.First().Claims.ToList();
                    var mappings = _config.GetSection("ClaimMappings");

                    string? GetClaim(string key, string fallback) =>
                        claims.FirstOrDefault(x => x.Type.EndsWith(mappings[key] ?? fallback, StringComparison.OrdinalIgnoreCase))?.Value;

                    userClaims.PersonID = GetClaim("PersonID", "IDPersona");
                    userClaims.UserType = GetClaim("UserType", "TipoUsuario");
                    userClaims.PayrollID = GetClaim("PayrollID", "nameidentifier");
                    userClaims.Email = GetClaim("Email", "NAM_upn");
                    userClaims.Profiles = GetClaim("Profiles", "perfiles");
                    userClaims.ITESMProfFuncionDesc = GetClaim("ITESMProfFuncionDesc", "ITESMProfFuncionDesc");
                    userClaims.ITESMProfFuncion = GetClaim("ITESMProfFuncion", "ITESMProfFuncion");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error en AuthHelper.GetClaims()");
            }

            return userClaims;
        }
    }
}
