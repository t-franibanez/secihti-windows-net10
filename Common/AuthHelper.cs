using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Linq;
using SECIHTI.Models.Common;

namespace SECIHTI.Common
{
    public class AuthHelper
    {
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthHelper(IConfiguration configuration, IWebHostEnvironment environment, IHttpContextAccessor httpContextAccessor)
        {
            _config = configuration;
            _httpContextAccessor = httpContextAccessor;
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
                Log.Error(e, "Error en AuthHelper.GetClaims()");
            }

            return userClaims;
        }
    }
}
