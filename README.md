# MedicionPQ API

Backend ASP.NET Core para iniciar sesión y administrar usuarios. La API usa SQL Server con Entity Framework Core, JWT Bearer para autenticación y roles para autorizar las operaciones administrativas.

## Requisitos y configuración

- .NET SDK 10.
- SQL Server o LocalDB.
- Configura `ConnectionStrings:DefaultConnection` en `appsettings.json` o mediante variables de entorno.
- El proyecto ya incluye un `UserSecretsId`. En este entorno de desarrollo la clave JWT aleatoria ya está guardada fuera del repositorio. Para generar una en otra máquina, desde la carpeta del proyecto:

```powershell
$bytes = New-Object byte[] 48
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Key" $key
```

La clave puede generarse con un gestor de secretos. En despliegues configura `Jwt__Key` como variable de entorno, y define también `Jwt__Issuer` y `Jwt__Audience`. La aplicación falla al arrancar si falta una clave de 32 bytes o más, el emisor o la audiencia.

En desarrollo, `appsettings.Development.json` permite el origen Vite `http://localhost:5173`. Si el front usa otra dirección, reemplázala por su origen exacto (esquema, host y puerto, sin barra final):

```json
{
  "Frontend": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

No uses `*` con credenciales. La API no habilita CORS para ningún origen mientras la lista esté vacía. CORS aplica a navegadores; no reemplaza la autenticación.

## Ejecutar

```powershell
dotnet run
```

En Development, Swagger UI está en `/swagger` y OpenAPI en `/openapi/v1.json`. La API usa redirección HTTPS. La base de datos debe existir y tener la tabla `Usuario` antes de llamar a los endpoints; el proyecto no aplica migraciones automáticamente al arrancar.

## Endpoints

La serialización JSON usa camel case para propiedades PascalCase (por ejemplo `IdUsuario` se devuelve como `idUsuario`). Los nombres ya escritos en camel case, como `correo` e `idUsuario`, conservan ese formato. Las solicitudes JSON deben enviarse con `Content-Type: application/json`.

| Método y ruta | Acceso | Descripción |
| --- | --- | --- |
| `POST /api/Auth/login` | Público | Valida correo y contraseña; devuelve JWT e ID de usuario. |
| `GET /api/Users` | Administrador | Lista usuarios sin exponer hashes de contraseña. |
| `GET /api/Users/admins` | Administrador | Lista solo usuarios con rol Administrador. |
| `POST /api/Users` | Administrador | Crea un usuario con contraseña hasheada. Devuelve `201` y el DTO creado. |
| `PUT /api/Users/{id}` | Administrador | Actualiza correo, nombre, rol, estado y duración de sesión; no cambia contraseña. |

### Iniciar sesión

Solicitud:

```json
{
  "correo": "admin@ejemplo.com",
  "contrasena": "contraseña"
}
```

Respuesta `200`:

```json
{
  "mensaje": "Inicio de sesión exitoso",
  "token": "<jwt>",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "expiresAtUtc": "2026-09-29T20:00:00Z",
  "idUsuario": 1
}
```

`expiresIn` se expresa en segundos y `expiresAtUtc` es la fecha de expiración UTC. Ambos valores y el claim `exp` del JWT se calculan con `Usuario.tiempoSesion` (en minutos) leído de la base al iniciar sesión. El frontend debe guardar `token` y enviarlo en las rutas protegidas como `Authorization: Bearer <token>`; puede usar `expiresAtUtc` para cerrar la sesión visualmente, pero el servidor siempre valida la expiración firmada del JWT. Si se cambia `tiempoSesion`, el cambio aplica al siguiente inicio de sesión; los tokens ya emitidos conservan su vencimiento original.

Las credenciales inválidas y las cuentas inactivas devuelven `401`. Los campos requeridos, formato de correo y mínimo de 8 caracteres se validan automáticamente y producen `400` si no son válidos.

### Usar rutas protegidas

Envía el token obtenido en cada solicitud administrativa:

```http
Authorization: Bearer <jwt>
```

El token incluye identificador, correo, nombre y rol. Expira según `tiempoSesion` del usuario, expresado en minutos. Las rutas administrativas requieren el rol `Administrador`; un usuario sin sesión válida recibe `401` y uno sin el rol recibe `403`.

### Crear y actualizar usuarios

`POST /api/Users` recibe `correo`, `contrasena`, `nombreUsuario` y `rol` (opcional; por defecto `Usuario`). El enum se envía como número: `1` para Administrador y `2` para Usuario. La contraseña requiere al menos 8 caracteres.

`PUT /api/Users/{id}` recibe `correo`, `nombreUsuario`, `rol` (1 Administrador, 2 Usuario), `edo` (`true` activo) y `tiempoSesion` (1 a 1440 minutos). Todos los campos se envían como un objeto JSON. La contraseña no se modifica desde esta ruta.

Los usuarios se devuelven como DTO con `idUsuario`, `correo`, `nombreUsuario`, `rol`, `edo` y `tiempoSesion`; el hash nunca se devuelve.

## Organización del código

- `Program.cs`: registra controladores, EF Core, servicios, JWT, CORS, Swagger y el orden de middlewares.
- `Controllers/`: define rutas HTTP, códigos de respuesta y autorización; delega reglas a servicios.
- `Services/`: implementa autenticación, administración de usuarios, generación de JWT y hash de contraseñas.
- `Data/AppDbContext.cs`: contexto de Entity Framework y acceso a la entidad `Usuario`.
- `Modelos/`: entidad persistida, solicitudes de entrada y DTOs de respuesta.
- `Tests/`: pruebas unitarias existentes para autenticación y servicios de usuarios.
- `Tools/MigrateAdminPassword/`: herramienta auxiliar para migrar la contraseña de administrador.

## Notas de integración

- El frontend debe enviar el token como `Authorization: Bearer ...` en las rutas protegidas.
- El estado inactivo impide iniciar sesión; usuarios inactivos tampoco deben conservar un token válido en el cliente.
- `GET /api/Users` y `GET /api/Users/admins` devuelven arreglos, incluso cuando no hay resultados.
- Errores de validación del modelo usan la respuesta estándar `400` de ASP.NET Core; otros errores de negocio devuelven `{ "mensaje": "..." }`.

## Inicio local para integrar el frontend

Estos pasos se ejecutan desde la carpeta del repositorio. Requieren el SDK de .NET 10 y una instancia de SQL Server/LocalDB con la base de datos y la tabla `Usuario` listas.

1. Configura la conexión `ConnectionStrings:DefaultConnection` para tu SQL Server en User Secrets (recomendado) o en una variable de entorno. No subas credenciales de base de datos al repositorio.
2. Cada desarrollador genera y guarda su propia clave JWT localmente. No copies una clave de otra persona ni la envíes al frontend:

```powershell
$bytes = New-Object byte[] 48
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Key" $key --project .\MedicionPQ.csproj
dotnet user-secrets set "Jwt:Issuer" "MedicionPQ" --project .\MedicionPQ.csproj
dotnet user-secrets set "Jwt:Audience" "MedicionPQ.Client" --project .\MedicionPQ.csproj
```

3. Inicia la API con el perfil HTTPS: `dotnet run --launch-profile https --project .\MedicionPQ.csproj`. La API publica `https://localhost:7090` y `http://localhost:5201`; Swagger queda en `https://localhost:7090/swagger`. Si el certificado local no estÃ¡ confiado, ejecuta `dotnet dev-certs https --trust` y acepta el diÃ¡logo del sistema.
4. Configura el front para llamar a la URL de la API. Si usa Vite, puede crear un archivo local `.env.local` (no subirlo si contiene valores privados) con, por ejemplo:

```env
VITE_API_BASE_URL=https://localhost:7090
```

El nombre `VITE_API_BASE_URL` es una convención de ejemplo: el frontend debe leer el nombre que use su código. En desarrollo, la API permite por defecto el origen `http://localhost:5173`; si el front corre en otro origen, actualiza `Frontend:AllowedOrigins` en `appsettings.Development.json` con el origen exacto (esquema, host y puerto, sin barra final).

5. El front inicia sesión con `POST /api/Auth/login`, conserva el `token` de la respuesta y lo manda en solicitudes protegidas como `Authorization: Bearer <token>`. El front nunca recibe ni configura `Jwt:Key`.

Cada integrante configura sus propios User Secrets en su equipo. Para desplegar, configura `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` y la cadena de conexión como secretos/variables del entorno del backend; permite en CORS solo el origen HTTPS publicado del frontend. No reutilices el secreto local y no guardes secretos en Git.
