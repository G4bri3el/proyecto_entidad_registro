# Document Manager

Plataforma documental segura para subir, visualizar, descargar, organizar y eliminar PDF e imagenes dentro de un arbol logico de carpetas.

## Arquitectura

- Frontend: React, TypeScript, Vite, React Router, Axios, TanStack Query, React Hook Form, Zod, react-pdf.
- Backend: ASP.NET Core Web API en .NET 10 LTS, EF Core, ASP.NET Core Identity, JWT, refresh tokens, 2FA TOTP, Serilog, Swagger en Development.
- Persistencia: SQL Server para metadatos, usuarios, roles, refresh tokens y auditoria.
- Storage: `IFileStorage` con implementacion inicial `LocalFileStorage`.
- Scanner: `IFileScanner` con implementacion inicial `DevelopmentFileScanner`.

La base de datos guarda solo metadatos; los binarios se guardan fuera de `wwwroot` y siempre se sirven a traves de endpoints autenticados/autorizados.

## Estructura

```text
backend/
  DocumentManager.sln
  src/
    DocumentManager.Api
    DocumentManager.Application
    DocumentManager.Domain
    DocumentManager.Infrastructure
  tests/
    DocumentManager.UnitTests
    DocumentManager.IntegrationTests
frontend/
storage/
```

## Requisitos

- .NET 10 SDK.
- Node.js 24+ y npm.
- SQL Server LocalDB o SQL Server Developer.

En esta maquina se instalo .NET SDK `10.0.400` para poder compilar la solucion.

## Configuracion Backend

Los secretos no estan en el repositorio. Configurelos con user-secrets:

```powershell
cd backend/src/DocumentManager.Api
dotnet user-secrets set "Jwt:SigningKey" "reemplace-con-un-secreto-local-de-al-menos-32-caracteres"
dotnet user-secrets set "DevelopmentSeed:AdminEmail" "admin@example.local"
dotnet user-secrets set "DevelopmentSeed:AdminPassword" "UnaContrasenaTemporal123"
```

La connection string local por defecto usa LocalDB:

```json
"Server=(localdb)\\mssqllocaldb;Database=DocumentManagerDev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Para otra instancia SQL Server, configure `ConnectionStrings:DefaultConnection` con user-secrets o variable de entorno.

## Base de Datos

La aplicacion no usa `EnsureDeleted()` ni destruye la BD al iniciar.

```powershell
cd backend
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/DocumentManager.Infrastructure/DocumentManager.Infrastructure.csproj --startup-project src/DocumentManager.Api/DocumentManager.Api.csproj --context ApplicationDbContext
```

El seed de Development crea roles `ADMINISTRATOR`, `EDITOR`, `VIEWER` y crea admin solo si `DevelopmentSeed` tiene email y password configurados.

## Ejecutar

Backend:

```powershell
cd backend/src/DocumentManager.Api
dotnet run
```

Frontend:

```powershell
cd frontend
npm install
npm run dev
```

Por defecto el frontend espera la API en `https://localhost:7043/api`. Puede cambiarse con:

```text
VITE_API_BASE_URL=https://localhost:7043/api
```

## Seguridad Implementada

- Identity para usuarios, contrasenas, lockout y security stamps.
- JWT de corta duracion y refresh tokens hasheados con rotacion.
- Refresh token en cookie HttpOnly; CSRF por cookie/header `XSRF-TOKEN` y `X-CSRF-TOKEN`.
- 2FA TOTP compatible con Microsoft Authenticator, Google Authenticator y Authy.
- Policies backend por rol; el frontend solo adapta la experiencia.
- Validacion backend de extension, MIME, tamano y magic bytes.
- SHA-256 calculado por stream en cada upload.
- StorageKey generado exclusivamente por servidor.
- Prevencion de path traversal en storage local.
- Soft delete para documentos y carpetas; eliminacion definitiva solo `ADMINISTRATOR`.
- Auditoria append-only sin endpoints publicos PUT/DELETE.
- Correlation ID por request en respuesta, logs y auditoria.
- Middleware global con ProblemDetails.
- Headers de seguridad y CORS estricto para `http://localhost:5173`.
- Swagger UI solo en Development.

## Flujos

Login:

```text
credenciales -> Identity/lockout/IsActive -> 2FA si aplica -> JWT + refresh cookie
```

Refresh:

```text
cookie refresh + CSRF -> hash lookup -> invalidar token usado -> emitir nuevo refresh -> revocar familia si hay reutilizacion
```

Upload:

```text
autorizacion -> tamano -> extension/MIME -> magic bytes -> scanner -> SHA-256 -> storage -> metadata -> auditoria
```

Eliminacion:

```text
confirmacion exacta ELIMINAR -> soft delete -> papelera -> restaurar o eliminar definitivamente con confirmacion estricta
```

## Huawei OBS Futuro

Para cambiar storage local por Huawei OBS, agregue una clase `HuaweiObsFileStorage : IFileStorage` en Infrastructure y cambie el registro DI segun `Storage:Provider`. No deben cambiar Controllers ni servicios de Documents.

## Tests

```powershell
cd backend
dotnet test DocumentManager.sln
```

Cobertura inicial:

- Validacion de firmas magicas, MIME, extension y tamano.
- StorageKey seguro y rechazo de path traversal.
- SHA-256 por stream.
- Constantes de confirmacion destructiva.
- Login, acceso anonimo, bloqueo de usuario desactivado, restricciones de rol y auditoria sin endpoints de mutacion.

## Fuera de Alcance

No se implementa Docker, Kubernetes, Nginx, cloud, Huawei OBS real, CI/CD ni permisos granulares por carpeta en esta etapa.
