using System.Diagnostics;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using Azure.Identity;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using Microsoft.AspNetCore.Authentication;

using Microsoft.IdentityModel.Logging;
using SECIHTI.Common;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var services = builder.Services;

// ── Azure Key Vault ──
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUri = configuration["KeyVaultUri"];
    if (!string.IsNullOrEmpty(keyVaultUri))
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"[KeyVault] Conectando a {keyVaultUri}...");

            var credential = new ManagedIdentityCredential();
            configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);

            sw.Stop();
            Console.WriteLine($"[KeyVault] Conectado en {sw.Elapsed.TotalSeconds:F1}s");

            // Log de diagnóstico: muestra qué claves se cargaron (sin valores)
            var connStr = configuration.GetConnectionString("AzureSql");
            Console.WriteLine($"[KeyVault] ConnectionStrings:AzureSql = {(string.IsNullOrEmpty(connStr) ? "❌ NO ENCONTRADA" : $"✅ Cargada ({connStr.Length} chars)")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[KeyVault] ❌ Error conectando a {keyVaultUri}: {ex.Message}");
            // La app continúa con lo que tenga en appsettings.json
        }
    }
    else
    {
        Console.WriteLine("[KeyVault] ⚠️ KeyVaultUri está vacío, saltando Key Vault.");
    }
}

// ── Serilog ──
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
builder.Host.UseSerilog();
Serilog.Debugging.SelfLog.Enable(msg =>
{
    Debug.Print(msg);
    Debugger.Break();
});

// ── Services ──
var bypassSaml = configuration.GetValue<bool>("Auth:BypassSaml");

services.AddHttpClient();

if (!bypassSaml)
{
    services.Configure<Saml2Configuration>(configuration.GetSection("Saml2"));
    services.AddOptions<Saml2Configuration>().Configure<IHttpClientFactory>((saml2Config, httpClientFactory) =>
    {
        try
        {
            saml2Config.Issuer = configuration["Saml2:Issuer"];
            saml2Config.SingleSignOnDestination = new Uri(configuration["Saml2:SingleSignOnDestination"]!);
            saml2Config.SingleLogoutDestination = new Uri(configuration["Saml2:SingleLogoutDestination"]!);
            saml2Config.SignatureAlgorithm = configuration["Saml2:SignatureAlgorithm"];
            saml2Config.SignAuthnRequest = Convert.ToBoolean(configuration["Saml2:SignAuthnRequest"]);
            saml2Config.SigningCertificate = X509CertificateLoader.LoadPkcs12(
                Convert.FromBase64String(configuration["Saml2:SigningCertificate"]!),
                configuration["Saml2:SigningCertificatePassword"],
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet
            );
            saml2Config.CertificateValidationMode = Enum.Parse<X509CertificateValidationMode>(configuration["Saml2:CertificateValidationMode"]!);
            saml2Config.RevocationMode = Enum.Parse<X509RevocationMode>(configuration["Saml2:RevocationMode"]!);
            saml2Config.AllowedAudienceUris.Add(saml2Config.Issuer);

            var entityDescriptor = new EntityDescriptor();
            entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(httpClientFactory, new Uri(configuration["Saml2:IdPMetadata"]!)).GetAwaiter().GetResult();

            if (entityDescriptor.IdPSsoDescriptor != null)
            {
                saml2Config.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
                saml2Config.SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.First().Location;
                saml2Config.SignatureValidationCertificates.AddRange(entityDescriptor.IdPSsoDescriptor.SigningCertificates);
            }
            else
            {
                throw new Exception("No se cargo el IdPSsoDescriptor del metadata");
            }
        }
        catch (Exception e)
        {
            Log.Error(e, e.Message);
            throw;
        }
    });

    services.AddSaml2(slidingExpiration: true);
}
else
{
    services.AddAuthentication("DevAuth")
        .AddScheme<AuthenticationSchemeOptions, DevAuthHandler>("DevAuth", null);
}

services.AddHttpContextAccessor();
services.AddControllersWithViews();

services.AddDistributedMemoryCache();
services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>()!);
        policy.AllowCredentials();
        policy.AllowAnyMethod();
        policy.AllowAnyHeader();
    });
});

// Los archivos estáticos del SPA se sirven desde wwwroot en producción
// y vía proxy al Angular dev server en desarrollo

// ── App ──
var app = builder.Build();

IdentityModelEventSource.ShowPII = true;

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
    app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();

if (!bypassSaml)
{
    app.UseSaml2();
    app.MapWhen(
        context => !(context.User.Identity?.IsAuthenticated ?? false)
                   && (context.Request.Path.Value?.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ?? false),
        config =>
        {
            config.Run(async context =>
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await Task.CompletedTask;
            });
        }
    );
}

app.UseAuthorization();
app.UseCors("AllowedOrigins");

app.MapControllers();

if (!bypassSaml)
{
    app.Use(async (context, next) =>
    {
        // No interceptar rutas del controlador Auth (Login, ACS, Logout)
        // ni el endpoint de Health para evitar loops de redirección
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/Auth/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/Health", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            await context.ChallengeAsync(Saml2Constants.AuthenticationScheme);
        }
        else
        {
            await next();
        }
    });
}


if (app.Environment.IsDevelopment())
{
    // Proxy al Angular dev server (ng serve en puerto 4200)
    app.MapWhen(
        context => !context.Request.Path.StartsWithSegments("/api")
                   && !context.Request.Path.StartsWithSegments("/Auth")
                   && !context.Request.Path.StartsWithSegments("/Health"),
        spaApp =>
        {
            spaApp.Run(async context =>
            {
                var proxyUri = $"http://localhost:4200{context.Request.Path}{context.Request.QueryString}";
                using var httpClient = new HttpClient();
                try
                {
                    var response = await httpClient.GetAsync(proxyUri);
                    context.Response.StatusCode = (int)response.StatusCode;
                    context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/html";
                    await response.Content.CopyToAsync(context.Response.Body);
                }
                catch
                {
                    context.Response.StatusCode = 502;
                    await context.Response.WriteAsync("Angular dev server not available. Run 'ng serve' in ClientApp/");
                }
            });
        }
    );
}
else
{
    app.MapFallbackToFile("index.html");
}

app.Run();
