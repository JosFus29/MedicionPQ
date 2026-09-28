# MedicionPQ API

Backend ASP.NET Core para iniciar sesión y administrar usuarios. La API usa SQL Server con Entity Framework Core, JWT Bearer para autenticación y roles para autorizar las operaciones administrativas.

## Requisitos y configuración

- .NET SDK 10.
- SQL Server o LocalDB.
- Configura `ConnectionStrings:DefaultConnection` en `appsettings.json` o mediante variables de entorno.
- Configura el secreto JWT fuera del repositorio. En desarrollo:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "<secreto-aleatorio-de-al-menos-32-bytes>"
```

La clave puede generarse con un gestor de secretos. En despliegues configura `Jwt__Key` como variable de entorno, y define también `Jwt__Issuer` y `Jwt__Audience`. La aplicación falla al arrancar si falta una clave de 32 bytes o más, el emisor o la audiencia.

Para permitir que el navegador del frontend consuma la API, agrega su origen exacto en `Frontend:AllowedOrigins` (esquema, host y puerto, sin barra final), por ejemplo en `appsettings.Development.json`:

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
  "idUsuario": 1
}
```

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
