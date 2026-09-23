# Modelos — Entidades y DTOs

Resumen
-------
La carpeta `Modelos` contiene las entidades que mapean la base de datos (`Usuario`) y los DTOs que usa la API para entrada/salida (LoginRequest, LoginResponse, CreateUserRequest, UsuarioDto).

Archivos principales
-------------------
- `Usuario.cs` — Entidad principal que representa la tabla `Usuario`. La propiedad `contrasena` almacena el hash.
- `UsuarioDto.cs` — DTO público que se devuelve al cliente (sin `contrasena`).
- `LoginRequest.cs` — DTO de entrada para autenticación.
- `LoginResponse.cs` — DTO de salida del login (token y datos públicos).
- `CreateUserRequest.cs` — DTO de entrada para creación de usuarios.

Dónde se referencia
-------------------
- `Data/AppDbContext.cs` — usa `Usuario` como `DbSet<Usuario>`.
- `Services/*` — `AuthService`, `UserService`, `PasswordService` usan estas clases.
- `Controllers/*` — consumen y exponen DTOs para la API.

Buenas prácticas
---------------
- Separar entidades (persistencia) de DTOs (contratos API) para evitar exponer datos sensibles.
- Mantener los DTOs simples y versionables (si cambias un DTO, añade una versión nueva antes de romper clientes).

Editar/Extender
---------------
- Si añades un campo en `Usuario`, actualiza `AppDbContext`, migraciones EF Core y los DTOs/mappers en `UserService`.
