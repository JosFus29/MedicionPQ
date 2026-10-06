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
| `GET /api/Unidades` | Usuario autenticado | Lista las unidades. |
| `GET /api/Unidades/{id}` | Usuario autenticado | Consulta el detalle de una unidad. |
| `POST /api/Unidades` | Administrador | Agrega una unidad y devuelve `201`. |
| `PUT /api/Unidades/{id}` | Administrador | Actualiza los datos de una unidad. |
| `GET /api/IO_GrupoElectrico` | Usuario autenticado | Lista grupos eléctricos. |
| `GET /api/IO_GrupoElectrico/{id}` | Usuario autenticado | Consulta un grupo eléctrico. |
| `POST /api/IO_GrupoElectrico` | Administrador | Agrega un grupo eléctrico. |
| `PUT /api/IO_GrupoElectrico/{id}` | Administrador | Actualiza un grupo eléctrico. |
| `GET /api/IO_GrupoFuncional` | Usuario autenticado | Lista grupos funcionales. |
| `GET /api/IO_GrupoFuncional/{id}` | Usuario autenticado | Consulta un grupo funcional. |
| `POST /api/IO_GrupoFuncional` | Administrador | Agrega un grupo funcional. |
| `PUT /api/IO_GrupoFuncional/{id}` | Administrador | Actualiza un grupo funcional. |
| `GET /api/PuntosIO` | Usuario autenticado | Lista puntos IO. |
| `GET /api/PuntosIO/{id}` | Usuario autenticado | Consulta un punto IO. |
| `PUT /api/PuntosIO/{id}` | Administrador | Edita un punto después de verificar nuevamente su contraseña. |
| `GET /api/EventosReg` | Usuario autenticado | Lista los registros de eventos. |
| `GET /api/EventosReg/{idMedidor}/{idCtrlRF}/{idEvento}/{fecha}` | Usuario autenticado | Consulta un registro por su clave compuesta. |
| `POST /api/EventosReg` | Administrador | Agrega un registro de evento. |
| `PUT /api/EventosReg/{idMedidor}/{idCtrlRF}/{idEvento}/{fecha}` | Administrador | Actualiza el consumo del registro. |

### Controladores RF

Las operaciones de controladores requieren un JWT válido. Cualquier usuario autenticado puede listar (`GET /api/ControladorRF`) y consultar el detalle por `idCtrlRF` (`GET /api/ControladorRF/{id}`). Ambas consultas incluyen la propiedad `medidores` con los medidores asociados. El alta y la modificación del controlador requieren además el rol `Administrador`; usuarios sin ese rol reciben `403`.

`POST /api/ControladorRF` recibe `numSerie` (máximo 20 caracteres), `dirIP` (máximo 20), `nombre` (máximo 50) y `edo` (entero). El identificador `idCtrlRF` se genera en la base de datos. Ejemplo:

```json
{
  "numSerie": "RF-0001",
  "dirIP": "192.168.1.20",
  "nombre": "Controlador principal",
  "edo": 10
}
```

`PUT /api/ControladorRF/{id}` recibe esos mismos cuatro campos y reemplaza sus valores. `edo` es entero: `10` activo, `11` en uso o `20` inactivo. Para inactivar un controlador, el administrador lo actualiza con `edo: 20`; para activarlo, usa `edo: 10`. Listados y detalles devuelven `idCtrlRF`, `numSerie`, `dirIP`, `nombre` y `edo`; un identificador inexistente produce `404`.

### Medidores QP

Cada registro de `MedidorQP` pertenece a exactamente un controlador, mediante `idCtrlRF`; un controlador puede tener varios medidores. El `idCtrlRF` enviado al crear o actualizar debe existir. No se permite borrar un controlador que tenga medidores asociados desde la relación configurada en Entity Framework.

`POST /api/MedidoresQP` y `PUT /api/MedidoresQP/{id}` requieren JWT con rol `Administrador`. Ambos reciben `idCtrlRF`, `dirRed` (entero), `dirRF` (máximo 20 caracteres), `descripcion` (máximo 80), `edo` (`10` activo, `11` en uso o `20` inactivo) y `dirIP` (máximo 20). Para inactivar un medidor sin borrar el registro, el administrador lo actualiza con `edo: 20`; para activarlo, usa `edo: 10`. El `idMedidor` se genera en la base de datos. El alta devuelve `201`; una referencia a controlador inexistente o estado inválido devuelve `400`, y un ID de medidor inexistente al actualizar devuelve `404`.

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

La API se conecta a la tabla existente `EventosConfig`; las operaciones de lectura no modifican los datos almacenados. Cualquier usuario autenticado puede listar (`GET /api/EventosConfig`) y consultar por `idEvento` (`GET /api/EventosConfig/{id}`). Solo el rol `Administrador` puede agregar, editar o dar de baja/reactivar eventos. Las actualizaciones buscan primero el registro por el ID de la ruta y devuelven `404` si no existe.

El alta (`POST /api/EventosConfig`) y la actualización (`PUT /api/EventosConfig/{id}`) reciben `descripcion` (máximo 100 caracteres), `numEvento` (entero), `edo` (booleano JSON) e `idUnidad` (entero). En `edo`, `true` representa activo (`1`) y `false` inactivo (`0`). Para quitar el evento de uso se actualiza con `edo: false`; el registro permanece guardado y puede reactivarse con `edo: true`. `idEvento` se trata como clave primaria generada por la base de datos y no se incluye en el cuerpo. No se configura una relación para `idUnidad` porque aún no se ha especificado su tabla relacionada.

Ejemplo de solicitud:

```json
{
  "descripcion": "Evento de ejemplo",
  "numEvento": 5,
  "edo": true,
  "idUnidad": 2
}
```

Las rutas de escritura requieren JWT con rol `Administrador`; los demás usuarios autenticados reciben `403`. No existe eliminación física para esta tabla.

### Unidades

La API consulta la tabla existente `Unidades`. Cualquier usuario autenticado puede listar (`GET /api/Unidades`) y consultar una unidad por `idUnidad` (`GET /api/Unidades/{id}`). Solo el rol `Administrador` puede agregar, editar o dar de baja/reactivar; las actualizaciones por ID devuelven `404` si la unidad no existe.

El alta (`POST /api/Unidades`) y la actualización (`PUT /api/Unidades/{id}`) reciben `simbolo` (máximo 10 caracteres), `nombre` (máximo 50) y `edo` (booleano JSON). `simbolo` puede ser `null` en registros ya guardados; para altas y actualizaciones se requiere un texto. `true` representa activo (`1`) y `false` inactivo (`0`). Para quitar una unidad de uso se actualiza con `edo: false`; el registro permanece guardado y puede reactivarse con `edo: true`. `idUnidad` se trata como clave primaria generada por la base de datos y no se incluye en el cuerpo.

Ejemplo de solicitud:

```json
{
  "simbolo": "kW",
  "nombre": "Kilowatt",
  "edo": true
}
```

No existe eliminación física de unidades. El administrador las da de baja usando `edo: false` en `PUT /api/Unidades/{id}`; si la unidad está referenciada, se conserva junto con los datos relacionados.

### Grupos IO

Ambos catálogos permiten consulta a cualquier usuario autenticado y alta/edición solo a administradores. No tienen eliminación física; el administrador los inactiva actualizando `edo` a `false` y los reactiva con `true` (`bit` 0/1 en la base). `nombre` es obligatorio y acepta hasta 50 caracteres.

- `IO_GrupoElectrico`: `GET /api/IO_GrupoElectrico`, `GET /api/IO_GrupoElectrico/{idGE}`, `POST /api/IO_GrupoElectrico` y `PUT /api/IO_GrupoElectrico/{idGE}`. `idGE` es autoincremental y no se manda en el alta.
- `IO_GrupoFuncional`: `GET /api/IO_GrupoFuncional`, `GET /api/IO_GrupoFuncional/{idGF}`, `POST /api/IO_GrupoFuncional` y `PUT /api/IO_GrupoFuncional/{idGF}`. `idGF` es autoincremental y no se manda en el alta.

Ambos `POST` reciben este formato:

```json
{
  "nombre": "Grupo principal",
  "edo": true
}
```

El `PUT` recibe los mismos campos. El detalle de un ID inexistente devuelve `404`.

### Puntos IO

Cualquier usuario autenticado puede consultar `GET /api/PuntosIO` y `GET /api/PuntosIO/{id}`. Solo un administrador puede editar con `PUT /api/PuntosIO/{id}`. Antes de guardar, la API vuelve a verificar `contrasenaActual` contra el hash de la cuenta autenticada; una contraseña incorrecta devuelve `401`. La contraseña se usa solo para la verificación y nunca forma parte de la respuesta ni se guarda en `PuntosIO`.

El cuerpo del `PUT` contiene `idGF`, `idGE`, `idUnidad`, `tag` (máximo 30 caracteres), `descripcion` (máximo 150), `tipoDato` (máximo 20), `tipoReg` (máximo 5), `dirCCEC`, `dirMB`, `edo`, `limMin`, `limMax` y `contrasenaActual`. Los IDs de grupo y unidad deben existir; si alguna referencia no existe, responde `400`. Un punto inexistente devuelve `404`.

Ejemplo:

```json
{
  "idGF": 1,
  "idGE": 2,
  "idUnidad": 3,
  "tag": "PRESION_01",
  "descripcion": "Sensor de presión principal",
  "tipoDato": "int",
  "tipoReg": "HR",
  "dirCCEC": 10,
  "dirMB": 100,
  "edo": true,
  "limMin": 0,
  "limMax": 500,
  "contrasenaActual": "contraseña-del-admin"
}
```

### Registros de eventos

La API consulta la tabla existente `EventosReg`. Cualquier usuario autenticado puede listar (`GET /api/EventosReg`) y consultar el detalle mediante los cuatro componentes de la clave: `idMedidor`, `idCtrlRF`, `idEvento` y `fecha`. Solo el rol `Administrador` puede agregar o editar; un registro no encontrado devuelve `404`.

El alta (`POST /api/EventosReg`) recibe los tres IDs y `fecha` como fecha ISO (`yyyy-MM-dd`), que juntos forman la clave, además de `consumo` como número. El tipo CLR `double` representa el tipo SQL Server `float` de doble precisión. Para consultar se usa `GET /api/EventosReg/{idMedidor}/{idCtrlRF}/{idEvento}/{fecha}`. En la edición (`PUT` con esa misma ruta), la clave permanece inmutable y el cuerpo contiene solamente `consumo`. Esta tabla no tiene campo `edo`, por lo que no ofrece baja lógica; tampoco expone eliminación física.

Ejemplo de alta:

```json
{
  "idMedidor": 4,
  "idCtrlRF": 2,
  "idEvento": 8,
  "fecha": "2026-10-05",
  "consumo": 125.75
}
```

Ejemplo de cuerpo para edición:

```json
{
  "fecha": "2026-10-06",
  "consumo": 128.5
}
```

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
- `Data/AppDbContext.cs`: contexto de Entity Framework y entidades `Usuario`, `ControladorRF`, `MedidorQP`, `EventosConfig`, `Unidad`, `EventosReg`, catálogos de grupos IO y `PuntosIO`.
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
