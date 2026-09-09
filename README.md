# Document Manager

Plataforma documental segura para subir, visualizar, descargar, organizar y eliminar PDF e imagenes dentro de un arbol logico de carpetas.

## Arquitectura

- Frontend: React, TypeScript, Vite, React Router, Axios, TanStack Query, React Hook Form, Zod, react-pdf.
- Backend: ASP.NET Core Web API en .NET 10 LTS, EF Core, ASP.NET Core Identity, JWT, refresh tokens, 2FA TOTP, Serilog, Swagger en Development.
- Persistencia: PostgreSQL para metadatos, usuarios, roles, refresh tokens y auditoria.
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
- PostgreSQL local con la base de datos `entidad_registro` creada.

En esta maquina se instalo .NET SDK `10.0.400` para poder compilar la solucion.

## Configuracion Local

Los secretos de desarrollo van en este archivo local ignorado por Git:

```text
C:\SAETA\Proyecto_entidad_registro\.env
```

Edita la connection string y pon tu password real de PostgreSQL:

```dotenv
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=entidad_registro;Username=postgres;Password=TU_PASSWORD_DE_POSTGRES
```

El archivo completo debe quedar con este formato:

```dotenv
ASPNETCORE_ENVIRONMENT=Development
DOTNET_ENVIRONMENT=Development

Database__Provider=PostgreSQL
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=entidad_registro;Username=postgres;Password=TU_PASSWORD_DE_POSTGRES

Jwt__SigningKey=local-dev-only-change-this-signing-key-before-sharing-1234567890
DevelopmentSeed__AdminUserName=Administrador
DevelopmentSeed__AdminEmail=admin@example.local
DevelopmentSeed__AdminPassword=TU_PASSWORD_ADMIN_LOCAL_12_CHARS
DevelopmentSeed__ResetAdminPassword=true
DevelopmentSeed__AdminFirstName=Administrador
DevelopmentSeed__AdminLastName=Local

VITE_API_BASE_URL=http://localhost:5285/api
```

El usuario inicial para entrar a la app es `Administrador` y la contrasena es el valor de `DevelopmentSeed__AdminPassword`. Debe tener minimo 12 caracteres porque Identity valida la politica de contrasenas. Si tu usuario de PostgreSQL no es `postgres`, cambia `Username` tambien.

En Development el frontend usa HTTP local (`http://localhost:5285/api`) para evitar errores de certificado como `ERR_CERT_AUTHORITY_INVALID`. En Production el backend sigue aplicando HTTPS.

## Base de Datos

La aplicacion no usa `EnsureDeleted()` ni destruye la BD al iniciar. Como ya creaste `entidad_registro` en pgAdmin 4, este comando solo aplica las tablas e indices de la migracion:

```powershell
cd C:\SAETA\Proyecto_entidad_registro\backend
dotnet tool restore
dotnet restore
dotnet tool run dotnet-ef database update --project src\DocumentManager.Infrastructure\DocumentManager.Infrastructure.csproj --startup-project src\DocumentManager.Api\DocumentManager.Api.csproj --context ApplicationDbContext
```

El seed de Development crea roles `ADMINISTRATOR`, `EDITOR`, `VIEWER` y sincroniza el admin si `DevelopmentSeed` tiene email y password configurados en `.env`.

## Ejecutar

Backend:

```powershell
cd C:\SAETA\Proyecto_entidad_registro\backend\src\DocumentManager.Api
dotnet run --launch-profile http
```

Frontend:

```powershell
cd C:\SAETA\Proyecto_entidad_registro\frontend
npm install
npm run dev
```

Abre la app en `http://localhost:5173`. Swagger queda disponible en `http://localhost:5285/swagger` con el perfil `http`.

## Visual Studio

1. Abre `C:\SAETA\Proyecto_entidad_registro\backend\DocumentManager.sln`.
2. Edita `C:\SAETA\Proyecto_entidad_registro\.env` y pon tu password real de PostgreSQL.
3. En la Consola del Administrador de paquetes o terminal, ejecuta la migracion desde `C:\SAETA\Proyecto_entidad_registro\backend`.
4. Inicia el perfil `http` de `DocumentManager.Api` para desarrollo local.
5. En otra terminal ejecuta el frontend con `npm run dev` desde `C:\SAETA\Proyecto_entidad_registro\frontend`.
6. Entra con usuario `Administrador` y la contrasena configurada en `DevelopmentSeed__AdminPassword`.

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
cd C:\SAETA\Proyecto_entidad_registro\backend
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