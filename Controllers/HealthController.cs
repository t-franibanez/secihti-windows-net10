using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace SECIHTI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// GET /Health — Health check básico (para CI/CD smoke tests)
        /// </summary>
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                samlConfigured = !string.IsNullOrEmpty(_configuration["Saml2:Issuer"]),
                keyVaultConfigured = !string.IsNullOrEmpty(_configuration["keyVaultUri"]),
                dbConfigured = !string.IsNullOrEmpty(_configuration.GetConnectionString("AzureSql"))
                               && _configuration.GetConnectionString("AzureSql") != "SERVER_PROVIDED_BY_KEYVAULT",
            });
        }

        /// <summary>
        /// GET /Health/deep — Diagnóstico completo: prueba conexión real a la BD.
        /// Útil para debugging sin acceso al portal de Azure.
        /// 
        /// ⚠️ En producción, protege este endpoint con [Authorize] o elimínalo.
        /// </summary>
        [HttpGet("deep")]
        public async Task<IActionResult> Deep()
        {
            var result = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow,
                ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                ["saml"] = new
                {
                    configured = !string.IsNullOrEmpty(_configuration["Saml2:Issuer"]),
                    bypassEnabled = _configuration.GetValue<bool>("Auth:BypassSaml"),
                },
                ["keyVault"] = new
                {
                    uri = _configuration["keyVaultUri"] ?? "(vacío)",
                    configured = !string.IsNullOrEmpty(_configuration["keyVaultUri"]),
                },
            };

            // ── Test de conexión a Azure SQL ──
            var connStr = _configuration.GetConnectionString("AzureSql");
            var dbResult = new Dictionary<string, object>();

            if (string.IsNullOrEmpty(connStr) || connStr == "SERVER_PROVIDED_BY_KEYVAULT")
            {
                dbResult["status"] = "not_configured";
                dbResult["detail"] = "ConnectionStrings:AzureSql no fue cargada (¿Key Vault no la proporcionó?)";
            }
            else
            {
                // Muestra el servidor sin exponer user/password
                try
                {
                    var builder = new SqlConnectionStringBuilder(connStr);
                    dbResult["server"] = builder.DataSource;
                    dbResult["database"] = builder.InitialCatalog;
                    dbResult["user"] = builder.UserID;
                    // No logueamos el password
                }
                catch
                {
                    dbResult["server"] = "(no se pudo parsear la cadena)";
                }

                // Intenta conectar
                var sw = Stopwatch.StartNew();
                try
                {
                    using var connection = new SqlConnection(connStr);
                    await connection.OpenAsync();

                    // Query simple para verificar que la BD responde
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = "SELECT 1 AS Ok";
                    var queryResult = await cmd.ExecuteScalarAsync();

                    sw.Stop();
                    dbResult["status"] = "connected";
                    dbResult["responseTimeMs"] = sw.ElapsedMilliseconds;
                    dbResult["detail"] = $"Conexión exitosa en {sw.ElapsedMilliseconds}ms";
                }
                catch (SqlException sqlEx)
                {
                    sw.Stop();
                    dbResult["status"] = "error";
                    dbResult["responseTimeMs"] = sw.ElapsedMilliseconds;
                    dbResult["errorNumber"] = sqlEx.Number;
                    dbResult["detail"] = sqlEx.Number switch
                    {
                        18456 => $"Login failed: usuario o contraseña incorrectos (User: {dbResult.GetValueOrDefault("user")})",
                        40615 => "Firewall: la IP del App Service no está en las reglas del servidor SQL",
                        40532 => $"Base de datos no encontrada: {dbResult.GetValueOrDefault("database")}",
                        -2 => "Timeout: el servidor SQL no respondió a tiempo",
                        _ => $"SQL Error {sqlEx.Number}: {sqlEx.Message}",
                    };
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    dbResult["status"] = "error";
                    dbResult["responseTimeMs"] = sw.ElapsedMilliseconds;
                    dbResult["detail"] = ex.Message;
                }
            }

            result["database"] = dbResult;

            // Status code según resultado
            var isHealthy = dbResult.GetValueOrDefault("status")?.ToString() == "connected"
                         || dbResult.GetValueOrDefault("status")?.ToString() == "not_configured";

            return isHealthy ? Ok(result) : StatusCode(503, result);
        }
    }
}