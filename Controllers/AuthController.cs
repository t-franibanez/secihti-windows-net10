using System.Security.Claims;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Serilog;

namespace SECIHTI.Controllers
{
    /// <summary>
    /// Controlador que implementa los endpoints requeridos por ITfoxtec SAML2:
    /// Login, AssertionConsumerService, Logout y SingleLogout.
    /// </summary>
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    public class AuthController : Controller
    {
        private readonly Saml2Configuration _saml2Config;

        public AuthController(IOptions<Saml2Configuration> saml2Config)
        {
            _saml2Config = saml2Config.Value;
        }

        /// <summary>
        /// Inicia el flujo SAML: construye un AuthnRequest y redirige al IdP (Okta/NAM).
        /// GET /Auth/Login
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            try
            {
                var binding = new Saml2RedirectBinding();

                binding.SetRelayStateQuery(new Dictionary<string, string>
                {
                    { "ReturnUrl", returnUrl ?? Url.Content("~/") }
                });

                return binding.Bind(new Saml2AuthnRequest(_saml2Config)).ToActionResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al iniciar el flujo SAML Login");
                return StatusCode(500, "Error al iniciar la autenticación SAML.");
            }
        }

        /// <summary>
        /// Recibe y procesa la respuesta SAML del IdP (Assertion Consumer Service).
        /// POST /Auth/AssertionConsumerService
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AssertionConsumerService()
        {
            try
            {
                var binding = new Saml2PostBinding();
                var saml2AuthnResponse = new Saml2AuthnResponse(_saml2Config);

                binding.ReadSamlResponse(Request.ToGenericHttpRequest(), saml2AuthnResponse);

                if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
                {
                    Log.Warning("SAML Response con status no exitoso: {Status}", saml2AuthnResponse.Status);
                    return Unauthorized($"SAML Response status: {saml2AuthnResponse.Status}");
                }

                binding.Unbind(Request.ToGenericHttpRequest(), saml2AuthnResponse);

                await saml2AuthnResponse.CreateSession(HttpContext,
                    claimsTransform: (claimsPrincipal) => ClaimsTransform.Transform(claimsPrincipal));

                var relayStateQuery = binding.GetRelayStateQuery();
                var returnUrl = relayStateQuery.ContainsKey("ReturnUrl")
                    ? relayStateQuery["ReturnUrl"]
                    : Url.Content("~/");

                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error procesando la respuesta SAML en AssertionConsumerService");
                return StatusCode(500, "Error al procesar la respuesta de autenticación.");
            }
        }

        /// <summary>
        /// Cierra la sesión del usuario y envía LogoutRequest al IdP.
        /// POST /Auth/Logout
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated ?? true)
                    return Redirect(Url.Content("~/"));

                var binding = new Saml2PostBinding();

                var logoutRequest = await new Saml2LogoutRequest(_saml2Config, User)
                    .DeleteSession(HttpContext);

                return binding.Bind(logoutRequest).ToActionResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al cerrar sesión SAML");
                return Redirect(Url.Content("~/"));
            }
        }

        /// <summary>
        /// Recibe la respuesta de Single Logout del IdP.
        /// POST /Auth/SingleLogout
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SingleLogout()
        {
            try
            {
                var binding = new Saml2PostBinding();
                var logoutRequest = new Saml2LogoutRequest(_saml2Config, User);

                binding.Unbind(Request.ToGenericHttpRequest(), logoutRequest);

                await logoutRequest.DeleteSession(HttpContext);

                return Redirect(Url.Content("~/"));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error procesando Single Logout");
                return Redirect(Url.Content("~/"));
            }
        }
    }

    /// <summary>
    /// Transforma los claims recibidos del IdP SAML antes de crear la sesión.
    /// Útil para mapear claims, agregar roles personalizados, etc.
    /// </summary>
    public static class ClaimsTransform
    {
        public static ClaimsPrincipal Transform(ClaimsPrincipal claimsPrincipal)
        {
            // Los claims llegan tal cual del IdP.
            // Agrega lógica de transformación aquí si es necesario.
            return claimsPrincipal;
        }
    }
}
