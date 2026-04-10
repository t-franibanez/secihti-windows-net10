using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SECIHTI.Common
{
    /// <summary>
    /// Handler de autenticacion fake para desarrollo.
    /// Se registra cuando Auth:BypassSaml = true para que [Authorize] funcione
    /// sin necesidad de federacion SAML real.
    /// Los claims se leen de appsettings > UserImpersonation.
    /// </summary>
    public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _config;

        public DevAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IConfiguration config)
            : base(options, logger, encoder)
        {
            _config = config;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim("IDPersona", _config["UserImpersonation:PersonID"] ?? ""),
                new Claim("TipoUsuario", _config["UserImpersonation:UserType"] ?? ""),
                new Claim(ClaimTypes.NameIdentifier, _config["UserImpersonation:PayrollID"] ?? ""),
                new Claim("NAM_upn", _config["UserImpersonation:Email"] ?? ""),
                new Claim("perfiles", _config["UserImpersonation:Profiles"] ?? ""),
            };

            var identity = new ClaimsIdentity(claims, "DevAuth");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "DevAuth");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
