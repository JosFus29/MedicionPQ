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
| `POST /api/Auth/renew` | Usuario autenticado | Renueva el JWT cuando quedan 60 segundos o menos. |
| `GET /api/Users` | Administrador | Lista usuarios sin exponer hashes de contraseña. |
| `GET /api/Users/admins` | Administrador | Lista solo usuarios con rol Administrador. |
| `POST /api/Users` | Administrador | Crea un usuario con contraseña hasheada. Devuelve `201` y el DTO creado. |
| `PUT /api/Users/{id}` | Administrador | Actualiza correo, nombre, rol, estado y duración de sesión; no cambia contraseña. |
| `GET /api/ControladorRF` | Usuario autenticado | Lista los controladores RF. |
| `GET /api/ControladorRF/{id}` | Usuario autenticado | Consulta el detalle de un controlador RF. |
| `POST /api/ControladorRF` | Administrador | Registra un controlador RF y devuelve `201`. |
| `PUT /api/ControladorRF/{id}` | Administrador | Actualiza los datos de un controlador RF. |
| `POST /api/MedidoresQP` | Administrador | Registra un medidor asociado a un controlador RF. |
| `PUT /api/MedidoresQP/{id}` | Administrador | Actualiza todos los campos editables de un medidor. |
| `GET /api/EventosConfig` | Usuario autenticado | Lista los eventos configurados. |
| `GET /api/EventosConfig/{id}` | Usuario autenticado | Consulta el detalle de un evento. |
| `POST /api/EventosConfig` | Administrador | Agrega un evento y devuelve `201`. |
| `PUT /api/EventosConfig/{id}` | Administrador | Actualiza los datos de un evento. |
| `DELETE /api/EventosConfig/{id}` | Administrador | Elimina el evento indicado. |
| `GET /api/Unidades` | Usuario autenticado | Lista las unidades. |
| `GET /api/Unidades/{id}` | Usuario autenticado | Consulta el detalle de una unidad. |
| `POST /api/Unidades` | Administrador | Agrega una unidad y devuelve `201`. |
| `PUT /api/Unidades/{id}` | Administrador | Actualiza los datos de una unidad. |
| `DELETE /api/Unidades/{id}` | Administrador | Elimina la unidad indicada. |

### Controladores RF

Las operaciones de controladores requieren un JWT válido. Cualquier usuario autenticado puede listar (`GET /api/ControladorRF`) y consultar el detalle por `idCtrlRF` (`GET /api/ControladorRF/{id}`). Ambas consultas incluyen la propiedad `medidores` con los medidores asociados. El alta y la modificación del controlador requieren además el rol `Administrador`; usuarios sin ese rol reciben `403`.

`POST /api/ControladorRF` recibe `numSerie` (máximo 20 caracteres), `dirIP` (máximo 20), `nombre` (máximo 50) y `edo` (entero). El identificador `idCtrlRF` se genera en la base de datos. Ejemplo:

```json
{
  "numSerie": "RF-0001",
  "dirIP": "192.168.1.20",
  "nombre": "Controlador principal",
  "edo": 1
}
```

`PUT /api/ControladorRF/{id}` recibe esos mismos cuatro campos y reemplaza sus valores. El catálogo numérico de `edo` (activo, mantenimiento e inactivo) está pendiente de definición; por ello, la API acepta el entero recibido sin asignar aún significados ni restringir sus valores. Listados y detalles devuelven `idCtrlRF`, `numSerie`, `dirIP`, `nombre` y `edo`; un identificador inexistente produce `404`.

### Medidores QP

Cada registro de `MedidorQP` pertenece a exactamente un controlador, mediante `idCtrlRF`; un controlador puede tener varios medidores. El `idCtrlRF` enviado al crear o actualizar debe existir. No se permite borrar un controlador que tenga medidores asociados desde la relación configurada en Entity Framework.

`POST /api/MedidoresQP` y `PUT /api/MedidoresQP/{id}` requieren JWT con rol `Administrador`. Ambos reciben `idCtrlRF`, `dirRed` (entero), `dirRF` (máximo 20 caracteres), `descripcion` (máximo 80), `edo` y `dirIP` (máximo 20). Los estados válidos son `10` en operación, `11` en uso, `0` fuera de operación y `20` en mantenimiento. El `idMedidor` se genera en la base de datos. El alta devuelve `201`; una referencia a controlador inexistente o estado inválido devuelve `400`, y un ID de medidor inexistente al actualizar devuelve `404`.

Ejemplo del cuerpo para alta y actualización:

```json
{
  "idCtrlRF": 1,
  "dirRed": 25,
  "dirRF": "RF-A25",
  "descripcion": "Medidor de entrada",
  "edo": 10,
  "dirIP": "192.168.1.25"
}
```

### Eventos configurables

La API se conecta a la tabla existente `EventosConfig`; las operaciones de lectura no modifican los datos almacenados. Cualquier usuario autenticado puede listar (`GET /api/EventosConfig`) y consultar por `idEvento` (`GET /api/EventosConfig/{id}`). Solo el rol `Administrador` puede agregar, editar o eliminar. Las modificaciones y eliminaciones buscan primero el registro por el ID de la ruta, y devuelven `404` si no existe.

El alta (`POST /api/EventosConfig`) y la actualización (`PUT /api/EventosConfig/{id}`) reciben `descripcion` (máximo 100 caracteres), `numEvento` (entero), `edo` (booleano JSON `true`/`false`) e `idUnidad` (entero). Como el significado de los valores de `edo` no está definido, se conserva el booleano sin asignar semántica a `true` o `false`. `idEvento` se trata como clave primaria generada por la base de datos y no se incluye en el cuerpo. No se configura una relación para `idUnidad` porque aún no se ha especificado su tabla relacionada.

Ejemplo de solicitud:

```json
{
  "descripcion": "Evento de ejemplo",
  "numEvento": 5,
  "edo": true,
  "idUnidad": 2
}
```

La eliminación (`DELETE /api/EventosConfig/{id}`) responde `204` cuando borra el registro y `404` si el ID no existe. Las rutas de escritura requieren JWT con rol `Administrador`; los demás usuarios autenticados reciben `403`.

### Unidades

La API consulta la tabla existente `Unidades`. Cualquier usuario autenticado puede listar (`GET /api/Unidades`) y consultar una unidad por `idUnidad` (`GET /api/Unidades/{id}`). Solo el rol `Administrador` puede agregar, editar o eliminar; las operaciones por ID devuelven `404` si la unidad no existe.

El alta (`POST /api/Unidades`) y la actualización (`PUT /api/Unidades/{id}`) reciben `simbolo` (máximo 10 caracteres), `nombre` (máximo 50) y `edo` (booleano JSON `true`/`false`). Como el significado del estado aún no está definido, se conserva el booleano sin interpretar sus valores. `idUnidad` se trata como clave primaria generada por la base de datos y no se incluye en el cuerpo.

Ejemplo de solicitud:

```json
{
  "simbolo": "kW",
  "nombre": "Kilowatt",
  "edo": true
}
```

La eliminación (`DELETE /api/Unidades/{id}`) devuelve `204` al borrar y `404` cuando el ID no existe. Si existen restricciones de clave foránea desde otras tablas, SQL Server impedirá eliminar unidades referenciadas.

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

### Renovar el token

El cliente puede llamar a `POST /api/Auth/renew` cuando llegue a `expiresAtUtc - 60 segundos`, enviando el token todavía vigente en `Authorization: Bearer <jwt>`. La API solo acepta la renovación cuando resta un minuto o menos; si se solicita antes devuelve `400`. Si el token ya venció, la autenticación responde `401` y el usuario debe iniciar sesión de nuevo. La cuenta se consulta nuevamente y debe seguir activa. La respuesta `200` usa el mismo formato que el login y contiene un JWT nuevo con vigencia basada en el `tiempoSesion` actual. El frontend debe reemplazar el token guardado por el nuevo.

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
- `Data/AppDbContext.cs`: contexto de Entity Framework y entidades `Usuario`, `ControladorRF`, `MedidorQP`, `EventosConfig` y `Unidad`.
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
dotnet user-secrets set "Jwt:Audience" "MedicionPQUsuarios" --project .\MedicionPQ.csproj
```

3. Inicia la API con el perfil HTTPS: `dotnet run --launch-profile https --project .\MedicionPQ.csproj`. La API publica `https://localhost:7090` y `http://localhost:5201`; Swagger queda en `https://localhost:7090/swagger`. Si el certificado local no estÃ¡ confiado, ejecuta `dotnet dev-certs https --trust` y acepta el diÃ¡logo del sistema.
4. Configura el front para llamar a la URL de la API. Si usa Vite, puede crear un archivo local `.env.local` (no subirlo si contiene valores privados) con, por ejemplo:

```env
VITE_API_BASE_URL=https://localhost:7090
```

El nombre `VITE_API_BASE_URL` es una convención de ejemplo: el frontend debe leer el nombre que use su código. En desarrollo, la API permite por defecto el origen `http://localhost:5173`; si el front corre en otro origen, actualiza `Frontend:AllowedOrigins` en `appsettings.Development.json` con el origen exacto (esquema, host y puerto, sin barra final).

5. El front inicia sesión con `POST /api/Auth/login`, conserva el `token` de la respuesta y lo manda en solicitudes protegidas como `Authorization: Bearer <token>`. El front nunca recibe ni configura `Jwt:Key`.

Cada integrante configura sus propios User Secrets en su equipo. Para desplegar, configura `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` y la cadena de conexión como secretos/variables del entorno del backend; permite en CORS solo el origen HTTPS publicado del frontend. No reutilices el secreto local y no guardes secretos en Git.
