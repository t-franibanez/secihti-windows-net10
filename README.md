# Template SAML2 + Angular + Bamboo

Plantilla base para aplicaciones web del Tec de Monterrey con autenticacion SAML2,
Angular standalone y el Design System Bamboo (ds-ng). Incluye Docker para desarrollo local,
Azure Key Vault para secretos, y CI/CD con GitHub Actions para tres ambientes.

---

## Tabla de contenido

1. [Requisitos previos](#requisitos-previos)
2. [Inicio rapido](#inicio-rapido)
3. [Estructura del proyecto](#estructura-del-proyecto)
4. [Arquitectura](#arquitectura)
5. [Configuracion del backend (.NET 8)](#configuracion-del-backend-net-8)
6. [Configuracion del frontend (Angular)](#configuracion-del-frontend-angular)
7. [Autenticacion SAML2](#autenticacion-saml2)
8. [Desarrollo local](#desarrollo-local)
9. [Docker](#docker)
10. [Environments y configuracion por ambiente](#environments-y-configuracion-por-ambiente)
11. [CI/CD con GitHub Actions](#cicd-con-github-actions)
12. [Azure Key Vault](#azure-key-vault)
13. [Agregar nuevas funcionalidades](#agregar-nuevas-funcionalidades)
14. [Referencia de archivos clave](#referencia-de-archivos-clave)
15. [Troubleshooting](#troubleshooting)

---

## Requisitos previos

| Herramienta | Version minima | Uso |
|-------------|----------------|-----|
| .NET SDK | 8.0+ | Backend ASP.NET Core |
| Node.js | 20.x+ | Build de Angular |
| npm | 10.x+ | Dependencias frontend |
| Angular CLI | 17+ | Desarrollo frontend |
| Docker | 24+ | Desarrollo local (opcional) |
| Git | 2.40+ | Control de versiones |

---

## Inicio rapido

### 1. Clonar y personalizar

```bash
git clone https://github.com/tu-org/template-saml2.git MiProyecto
cd MiProyecto
chmod +x init-project.sh
./init-project.sh MiProyecto
```

El script `init-project.sh` hace lo siguiente automaticamente:
- Reemplaza todos los nombres de la plantilla (namespaces C#, package.json, docker-compose,
  configuraciones de Serilog, etc.) con el nombre de tu proyecto
- Renombra el archivo `.csproj`
- Actualiza `angular.json` y `index.html`
- Genera variantes PascalCase (para C#) y lowercase (para Docker/npm)

### 2. Configurar variables locales

```bash
cp .env.example .env
# Edita .env con tus valores locales
```

### 3. Instalar dependencias y ejecutar

```bash
# Backend + Frontend juntos
dotnet restore
cd ClientApp && npm install --legacy-peer-deps && cd ..
dotnet run
```

La app se abre en `https://localhost:5001` con claims simulados desde `appsettings.Development.json`.

---

## Estructura del proyecto

```
/
├── Program.cs                          # Entry point (.NET 8 minimal hosting)
├── {Proyecto}.csproj                   # Configuracion del proyecto .NET
├── appsettings.json                    # Config de produccion (SAML, Serilog, CORS)
├── appsettings.Development.json        # Config de desarrollo (bypass SAML, user mock)
├── .env.example                        # Variables de entorno de ejemplo
├── init-project.sh                     # Script de bootstrap para proyectos nuevos
├── docker-compose.yml                  # Docker Compose para desarrollo local
├── Dockerfile.dev                      # Dockerfile de desarrollo
├── Flujo Despliegue.md                 # Documentacion de despliegue (detallada)
│
├── Controllers/
│   ├── HomeController.cs               # Endpoint GetUserClaims (autenticado)
│   └── HealthController.cs             # Health check (anonimo, para CI/CD)
│
├── Common/
│   ├── AuthHelper.cs                   # Mapeo de claims SAML → UserClaims
│   ├── DevAuthMiddleware.cs            # Auth fake para desarrollo + DevAuthHandler
│   └── GlobalExceptionMiddleware.cs    # Manejo global de errores
│
├── Models/
│   └── Common/
│       ├── Response.cs                 # Contrato generico ApiResponse<T>
│       └── UserClaims.cs               # Modelo de claims del usuario
│
├── ClientApp/                          # === ANGULAR FRONTEND ===
│   ├── angular.json                    # Config de Angular (builds, environments)
│   ├── package.json                    # Dependencias npm
│   ├── src/
│   │   ├── main.ts                     # Bootstrap de la app Angular
│   │   ├── index.html                  # HTML principal
│   │   ├── styles.scss                 # Estilos globales (Bamboo)
│   │   ├── polyfills.ts                # Polyfills (zone.js, localize)
│   │   │
│   │   ├── environments/
│   │   │   ├── environment.interface.ts  # Interfaz TypeScript (type-safe)
│   │   │   ├── environment.ts            # Desarrollo
│   │   │   ├── environment.pprd.ts       # Pre-produccion
│   │   │   └── environment.prod.ts       # Produccion
│   │   │
│   │   └── app/
│   │       ├── app.component.ts/html     # Root component (BmbTheme)
│   │       ├── app.config.ts             # Providers (router, http, interceptors)
│   │       ├── app.routes.ts             # Rutas principales
│   │       │
│   │       ├── components/
│   │       │   ├── federation/           # Pantalla de carga post-login SAML
│   │       │   ├── layout/               # Shell con sidebar (BmbContainer)
│   │       │   └── not-found-page/       # Pagina 404
│   │       │
│   │       ├── guard/
│   │       │   ├── user-session.guard.ts  # Protege rutas por sesion activa
│   │       │   └── admin.guard.ts         # roleGuard() configurable por roles
│   │       │
│   │       ├── interceptor/
│   │       │   └── token-api.interceptor.ts  # Cache-Control + refresh en 401/403
│   │       │
│   │       ├── models/
│   │       │   ├── response-api.model.ts  # ApiResponse<T> (alineado con backend)
│   │       │   └── user-claims.model.ts   # UserClaims (alineado con backend)
│   │       │
│   │       └── services/
│   │           ├── session.service.ts           # Sesion del usuario (sessionStorage)
│   │           ├── session-refresh.service.ts   # Refresh automatico cada 4 min
│   │           └── notification-snackbar.service.ts  # Snackbar de Material
│   │
│   └── ...
│
├── Properties/
│   └── launchSettings.json             # Puertos de desarrollo (5001/5000)
│
└── github-workflow-templates/
    ├── deploy-reusable.yml             # Workflow reutilizable (build + deploy)
    ├── ci-cd-devl.yml                  # Deploy a DEVL (branch: develop)
    ├── ci-cd-pprd.yml                  # Deploy a PPRD (branch: release)
    └── ci-cd-prod.yml                  # Deploy a PROD (branch: main)
```

---

## Arquitectura

```
                    ┌──────────────────────────────────────────┐
                    │              Azure Web App               │
                    │                                          │
  Usuario ──HTTPS──►│  ASP.NET Core 8 (.NET)                  │
                    │  ├─ GlobalExceptionMiddleware             │
                    │  ├─ DevAuthMiddleware (solo si bypass)    │
                    │  ├─ Authentication (SAML2 o DevAuth)     │
                    │  ├─ Authorization ([Authorize])          │
                    │  ├─ CORS                                 │
                    │  ├─ Controllers (API)                    │
                    │  └─ SPA (Angular)                        │
                    │                                          │
                    │  Angular Standalone                      │
                    │  ├─ Bamboo Design System (ds-ng)         │
                    │  ├─ FederationComponent (post-login)     │
                    │  ├─ LayoutComponent (shell + sidebar)    │
                    │  ├─ Guards (session + roles)             │
                    │  └─ SessionRefreshService (cada 4 min)   │
                    │                                          │
                    └────────────┬─────────────────────────────┘
                                 │
                    ┌────────────▼─────────────┐
                    │     Azure Key Vault      │
                    │  (secretos SAML, certs)   │
                    └──────────────────────────┘
                                 │
                    ┌────────────▼─────────────┐
                    │     IdP SAML (amfs)       │
                    │  amfs.tec.mx / amfsdevl   │
                    └──────────────────────────┘
```

---

## Configuracion del backend (.NET 8)

### Program.cs (Minimal Hosting)

El archivo `Program.cs` unifica toda la configuracion en un solo archivo con top-level statements.
El flujo es:

1. **Azure Key Vault** (solo en ambientes publicados)
2. **Serilog** para logging estructurado
3. **SAML2 o DevAuth** segun la config `Auth:BypassSaml`
4. **Middleware pipeline**: Exception → Auth → SAML → Authorization → CORS → Controllers → SPA

### appsettings.json — Configuracion principal

```jsonc
{
  // CORS: origenes permitidos
  "AllowedOrigins": ["https://tu-app.tec.mx"],

  // Auth: bypass de SAML para desarrollo
  // (solo en appsettings.Development.json)
  "Auth": { "BypassSaml": true },

  // Mapeo de claims SAML → propiedades de UserClaims
  // Cambia estos valores si tu IdP usa nombres de claims diferentes
  "ClaimMappings": {
    "PersonID": "IDPersona",
    "UserType": "TipoUsuario",
    "PayrollID": "nameidentifier",
    "Email": "NAM_upn",
    "Profiles": "perfiles",
    "ITESMProfFuncionDesc": "ITESMProfFuncionDesc",
    "ITESMProfFuncion": "ITESMProfFuncion"
  },

  // Azure Key Vault
  "keyVaultUri": "https://kv-tu-app.vault.azure.net/",

  // SAML2: configuracion de federacion
  "Saml2": {
    "IdPMetadata": "https://amfsdevl.tec.mx/nidp/saml2/metadata",
    "Issuer": "https://tu-app.tec.mx/Auth/AssertionConsumerService",
    "SingleSignOnDestination": "https://amfsdevl.tec.mx/nidp/saml2/sso",
    "SingleLogoutDestination": "https://amfsdevl.tec.mx/nidp/saml2/slo",
    "SignatureAlgorithm": "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
    "SignAuthnRequest": "true",
    "CertificateValidationMode": "None",
    "RevocationMode": "NoCheck",
    "SigningCertificate": "",       // Base64 del .pfx (en Key Vault)
    "SigningCertificatePassword": "" // Password del .pfx (en Key Vault)
  },

  // Serilog: logging
  "Serilog": { /* ... */ }
}
```

### appsettings.Development.json — Desarrollo local

```json
{
  "Auth": {
    "BypassSaml": true
  },
  "AllowedOrigins": ["https://localhost:5001"],
  "UserImpersonation": {
    "PayrollID": "L00000000",
    "PersonID": "00000000",
    "Email": "tu.nombre@tec.mx",
    "UserType": "Administrador",
    "Profiles": "Colaborador"
  }
}
```

Cuando `Auth:BypassSaml` es `true`:
- No se conecta al IdP SAML ni descarga metadata
- Se registra un esquema de autenticacion fake (`DevAuth`)
- Los claims se inyectan desde `UserImpersonation`
- `[Authorize]` en controllers funciona normalmente

### Contrato de respuesta API

Backend y frontend comparten el mismo contrato:

```csharp
// C# — Models/Common/Response.cs
public class Response<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}
```

```typescript
// TypeScript — models/response-api.model.ts
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}
```

.NET serializa PascalCase como camelCase por defecto, asi que `Success` → `success`, `Data` → `data`.

### Health Check

El endpoint `GET /Health` esta disponible sin autenticacion y reporta:

```json
{
  "status": "healthy",
  "samlConfigured": true,
  "keyVaultConfigured": true
}
```

Usado por los smoke tests de CI/CD y para monitoreo.

---

## Configuracion del frontend (Angular)

### Stack

| Libreria | Uso |
|----------|-----|
| Angular 20+ (standalone) | Framework principal |
| Bamboo ds-ng | Design System del Tec |
| Angular Material | Snackbar, CDK |
| RxJS | Programacion reactiva |

### Environments (Type-Safe)

Todos los environments implementan la interfaz `Environment`:

```typescript
// environments/environment.interface.ts
export interface Environment {
  production: boolean;
  surveyUrl: string;
  homeUrl: string;
  apiBaseUrl: string;
}
```

Si agregas una nueva propiedad a la interfaz, TypeScript te obliga a agregarla en los 3 archivos
de environment. Esto evita errores de configuracion entre ambientes.

### Rutas

```typescript
// app.routes.ts
export const routes: Routes = [
  // Ruta raiz: FederationComponent obtiene claims y redirige a /dashboard
  { path: "", component: FederationComponent, pathMatch: "full" },

  // Rutas protegidas por sesion
  {
    path: "",
    component: LayoutComponent,
    canActivate: [userSessionGuard],
    children: [
      // Agrega tus rutas aqui:
      // { path: "dashboard", component: DashboardComponent },
      // { path: "admin", canActivate: [roleGuard('Administrador')], children: [...] },
    ],
  },

  // 404
  { path: "**", component: NotFoundPageComponent },
];
```

### Guards

**`userSessionGuard`** — Verifica que exista sesion activa en sessionStorage.

**`roleGuard(...roles)`** — Guard configurable por roles:

```typescript
// Una sola role
canActivate: [roleGuard('Administrador')]

// Multiples roles
canActivate: [roleGuard('Administrador', 'Supervisor', 'Coordinador')]
```

### Interceptor HTTP

El interceptor `tokenApiInterceptor` hace dos cosas:
1. Agrega headers `Cache-Control: no-cache` a todas las peticiones
2. Ante errores 401/403, intenta refrescar la sesion automaticamente

### Flujo de sesion

1. Angular arranca → `FederationComponent` llama `Home/GetUserClaims`
2. El backend retorna los claims del usuario autenticado
3. `SessionService.createSessionUser()` guarda en `sessionStorage`
4. Redirige a `/dashboard`
5. `SessionRefreshService` refresca claims cada 4 minutos en background
6. Si el refresh falla, `sessionValid$` emite `false`

---

## Autenticacion SAML2

### Flujo en produccion

```
Usuario → App → [Authorize] → No autenticado?
  → Challenge SAML2 → Redirect a IdP (amfs.tec.mx)
  → Login en IdP → POST AssertionConsumerService
  → Claims disponibles en HttpContext.User
  → FederationComponent obtiene claims via API
  → Sesion Angular creada
```

### Claims disponibles

Los claims se mapean desde `appsettings.json` > `ClaimMappings`:

| Propiedad | Claim SAML (default) | Descripcion |
|-----------|---------------------|-------------|
| `PersonID` | `IDPersona` | ID unico de persona |
| `UserType` | `TipoUsuario` | Tipo de usuario (Administrador, Colaborador, etc.) |
| `PayrollID` | `nameidentifier` | Nomina |
| `Email` | `NAM_upn` | Correo institucional |
| `Profiles` | `perfiles` | Perfiles asignados |
| `ITESMProfFuncionDesc` | `ITESMProfFuncionDesc` | Descripcion de funcion |
| `ITESMProfFuncion` | `ITESMProfFuncion` | Codigo de funcion |

Si un proyecto nuevo necesita claims diferentes, solo cambia los valores en `ClaimMappings`
sin tocar codigo C#.

### Flujo en desarrollo

Cuando `Auth:BypassSaml = true`:
- No se contacta al IdP
- `DevAuthHandler` crea un `ClaimsPrincipal` con claims de `UserImpersonation`
- `[Authorize]` pasa sin problema
- Puedes simular diferentes tipos de usuario cambiando `UserType` en `appsettings.Development.json`

---

## Desarrollo local

### Opcion A: Sin Docker (recomendado para desarrollo rapido)

```bash
# 1. Restaurar dependencias
dotnet restore
cd ClientApp && npm install --legacy-peer-deps && cd ..

# 2. Ejecutar (backend + frontend)
dotnet run

# La app se abre en https://localhost:5001
# Angular se sirve en http://localhost:4200 (proxied por ASP.NET)
```

### Opcion B: Con Docker

```bash
docker compose up --build
# Backend: https://localhost:5011
# Angular: http://localhost:4210
```

### Cambiar el usuario de prueba

Edita `appsettings.Development.json`:

```json
{
  "UserImpersonation": {
    "PayrollID": "L12345678",
    "PersonID": "12345678",
    "Email": "otro.usuario@tec.mx",
    "UserType": "Colaborador",
    "Profiles": "Becario"
  }
}
```

Reinicia el backend para que tome los cambios.

---

## Docker

### docker-compose.yml

```yaml
services:
  {proyecto}_web:
    build:
      context: .
      dockerfile: Dockerfile.dev
    ports:
      - "5011:5001"   # HTTPS backend
      - "5010:5000"   # HTTP backend
      - "4210:4200"   # Angular dev server
    volumes:
      - .:/app                          # Hot reload de codigo
      - /app/ClientApp/node_modules     # Evita conflicto de node_modules
      - /app/bin
      - /app/ClientApp/.angular/cache
```

### Notas

- El `Dockerfile.dev` instala .NET SDK 8, Node.js 20, y Angular CLI
- Los volumenes permiten hot-reload tanto de C# (`dotnet watch`) como de Angular
- La network se crea automaticamente para comunicacion entre servicios futuros

---

## Environments y configuracion por ambiente

| Ambiente | Branch | Build Definition | Environment File | appsettings |
|----------|--------|-----------------|------------------|-------------|
| DEVL | `develop` | `BackOfficeDEVL` | `environment.ts` | `vars.DEVL_JSON` |
| PPRD | `release` | `BackOfficePPRD` | `environment.pprd.ts` | `vars.PPRD_JSON` |
| PROD | `main` | `BackOfficePROD` | `environment.prod.ts` | `vars.PROD_JSON` |

### Variables de GitHub que necesitas configurar

En **Settings > Variables** del repositorio:

| Variable | Contenido |
|----------|-----------|
| `DEVL_JSON` | JSON completo de `appsettings.json` para DEVL |
| `PPRD_JSON` | JSON completo de `appsettings.json` para PPRD |
| `PROD_JSON` | JSON completo de `appsettings.json` para PROD |

En **Settings > Secrets**:

| Secret | Contenido |
|--------|-----------|
| `AZUREAPPSERVICE_PUBLISHPROFILE_DEVL` | Publish profile de la Azure Web App DEVL |
| `AZUREAPPSERVICE_PUBLISHPROFILE_PPRD` | Publish profile de la Azure Web App PPRD |
| `AZUREAPPSERVICE_PUBLISHPROFILE_PROD` | Publish profile de la Azure Web App PROD |

---

## CI/CD con GitHub Actions

### Workflow reutilizable

Los tres ambientes usan un solo workflow base (`deploy-reusable.yml`) que recibe parametros:

```yaml
# ci-cd-devl.yml (ejemplo simplificado)
jobs:
  deploy:
    uses: ./.github/workflows/deploy-reusable.yml
    with:
      environment-name: 'Development'
      app-name: 'TU_APP_DEVL'
      build-definition: 'BackOfficeDEVL'
      settings-var: ${{ vars.DEVL_JSON }}
    secrets:
      publish-profile: ${{ secrets.AZUREAPPSERVICE_PUBLISHPROFILE_DEVL }}
```

### Como configurar CI/CD en un proyecto nuevo

1. Copia `github-workflow-templates/` a `.github/workflows/`
2. En cada archivo `ci-cd-*.yml`, reemplaza `TU_APP_*` con el nombre real de tu Azure Web App
3. Configura las variables y secrets en GitHub (ver tabla arriba)
4. El deploy se ejecuta automaticamente al hacer push a la branch correspondiente

### Smoke test

El workflow de PPRD y PROD incluye un smoke test post-deploy que verifica el endpoint `/Health`.
Si falla, el log del workflow mostrara el error.

---

## Azure Key Vault

En ambientes publicados (no Development), la app lee secretos de Azure Key Vault automaticamente.

### Secretos necesarios en Key Vault

| Nombre del secreto | Descripcion |
|--------------------|-------------|
| `Saml2--SigningCertificate` | Certificado SAML en Base64 (.pfx) |
| `Saml2--SigningCertificatePassword` | Password del certificado |
| `ConnectionStrings--AzureSql` | Connection string de la BD (si aplica) |

> Nota: Azure Key Vault usa `--` como separador, que .NET mapea a `:` en la configuracion.
> Asi `Saml2--SigningCertificate` se lee como `Configuration["Saml2:SigningCertificate"]`.

### Configuracion de acceso

La app usa `DefaultAzureCredential`, que en Azure Web App resuelve via Managed Identity:

1. En la Azure Web App, activa **System-assigned managed identity**
2. En Key Vault > **Access policies**, agrega la identidad con permisos `Get` y `List` en Secrets

---

## Agregar nuevas funcionalidades

### Agregar un nuevo componente/pagina

```bash
cd ClientApp
ng generate component components/mi-pagina --standalone
```

Luego registrala en `app.routes.ts`:

```typescript
children: [
  { path: "mi-pagina", component: MiPaginaComponent },
],
```

### Agregar un nuevo controller API

```bash
# Crear archivo en Controllers/
```

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace TU_NAMESPACE.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    [Authorize]
    [EnableCors("AllowedOrigins")]
    public class MiController : ControllerBase
    {
        [HttpGet]
        public Response<MiModelo> GetDatos()
        {
            // Tu logica aqui
            return new Response<MiModelo>(datos);
        }
    }
}
```

### Agregar un nuevo servicio Angular

```bash
cd ClientApp
ng generate service services/mi-servicio
```

```typescript
@Injectable({ providedIn: 'root' })
export class MiServicioService {
  private http = inject(HttpClient);

  getDatos(): Observable<ApiResponse<MiModelo>> {
    return this.http.get<ApiResponse<MiModelo>>('MiController/GetDatos');
  }
}
```

### Proteger una ruta por rol

```typescript
import { roleGuard } from './guard/admin.guard';

// En app.routes.ts
{
  path: "admin",
  canActivate: [roleGuard('Administrador', 'Supervisor')],
  children: [
    { path: "config", component: ConfigComponent },
  ],
}
```

---

## Referencia de archivos clave

| Archivo | Proposito | Cuando modificar |
|---------|-----------|-----------------|
| `Program.cs` | Entry point, middleware pipeline, servicios | Agregar servicios, cambiar orden de middleware |
| `appsettings.json` | Config de produccion | URLs SAML, CORS, Key Vault, claims |
| `appsettings.Development.json` | Config de desarrollo | Usuario de prueba, bypass auth |
| `Common/AuthHelper.cs` | Mapeo de claims SAML | Si agregas claims nuevos a `ClaimMappings` |
| `Common/DevAuthMiddleware.cs` | Auth fake + DevAuthHandler | Normalmente no se modifica |
| `Models/Common/Response.cs` | Contrato de respuesta API | Si necesitas campos extra en la respuesta |
| `app.routes.ts` | Rutas de Angular | Cada que agregues paginas |
| `app.config.ts` | Providers de Angular | Si agregas interceptors o providers globales |
| `environment.interface.ts` | Interfaz de environment | Si agregas config nueva por ambiente |
| `init-project.sh` | Bootstrap de proyecto nuevo | No necesitas modificar |

---

## Troubleshooting

### "Error al descargar metadata del IdP"

- Verifica que la URL en `Saml2:IdPMetadata` sea accesible desde tu red
- En desarrollo, esto no deberia pasar si `Auth:BypassSaml = true`

### "401 Unauthorized" en desarrollo

- Verifica que `appsettings.Development.json` tenga `"Auth": { "BypassSaml": true }`
- Verifica que `UserImpersonation` tenga todos los campos

### "CORS error" en el navegador

- Verifica que `AllowedOrigins` en `appsettings.json` incluya la URL desde donde accedes
- En desarrollo: `["https://localhost:5001"]`

### Angular no compila

- Ejecuta `npm install --legacy-peer-deps` (Bamboo ds-ng requiere `--legacy-peer-deps`)
- Verifica que tengas Node.js 20+

### Docker no arranca

- Verifica que `.env` exista (copia de `.env.example`)
- Verifica que los puertos 5011, 5010, 4210 no esten en uso

### Warnings de Vite/Rolldown en consola (known issue)

```
optimizeDeps.esbuildOptions is now deprecated. Use optimizeDeps.rolldownOptions instead.
esbuild option is set to false, but oxc option was not set to false.
```

Estos warnings vienen de Angular 20+ con Vite 8 que migro de esbuild a Rolldown/Oxc.
Son inofensivos y se resuelven cuando las dependencias (Bamboo ds-ng, Angular CLI) se actualicen.
No afectan el build ni el runtime.

### El script init-project.sh no funciona en Linux

El script usa `sed -i ''` (macOS). En Linux usa `sed -i` sin comillas vacias.
El script intenta ambas variantes automaticamente.

---

## Licencia

Uso interno - Tecnologico de Monterrey.
