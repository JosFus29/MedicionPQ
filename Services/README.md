# Services — Capa de Servicios

Resumen
-------
La carpeta `Services` contiene la lógica de negocio y las operaciones que interactúan con el `AppDbContext` y otros servicios (TokenService, PasswordService). Los controladores deben delegar en estos servicios y no contener lógica de acceso a datos.

Archivos principales
-------------------
- `IAuthService.cs` / `AuthService.cs` — Validación de credenciales, verificación de contraseñas hasheadas y generación de JWT mediante `TokenService`.
- `IPasswordService.cs` / `PasswordService.cs` — Abstracción para hashear y verificar contraseñas. Implementación basada en `PasswordHasher<Usuario>`.
- `IUserService.cs` / `UserService.cs` — Operaciones relacionadas con usuarios: listar administradores y crear usuarios (valida entrada y hashea contrasena).
- `TokenService.cs` — Encargado de la creación del JWT (claims mínimos: NameIdentifier, Email, Name, Role).

Dónde se referencia
-------------------
- `Program.cs` — se registran en el contenedor DI (`AddScoped<IAuthService, AuthService>`, `AddScoped<IUserService, UserService>`, `AddScoped<IPasswordService, PasswordService>`, `AddScoped<TokenService>`).
- `Controllers/*` — los controladores consumen `IAuthService` e `IUserService`.

Buenas prácticas
---------------
- Mantener los métodos pequeños y con una sola responsabilidad.
- No exponer hashes ni contraseñas en las respuestas.
- Añadir pruebas unitarias para cualquier regla de negocio compleja.

Editar/Extender
---------------
- Para añadir una nueva operación sobre usuarios, agregar la definición en `IUserService` y su implementación en `UserService`, escribir tests y exponer un endpoint en `Controllers/UsersController.cs`.
