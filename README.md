# MedicionPQ — Documentación del proyecto

Resumen
-------
MedicionPQ es una API Web (.NET 10) para gestionar autenticación y usuarios (ej. para una página de medición de consumo energético). Está organizada con separación de capas: Controllers (API), Services (lógica de negocio), Data (EF Core DbContext) y Models (entidades/DTOs). Incluye hashing de contraseñas, autenticación JWT y autorización por roles.

Estructura principal (archivos clave)
----------------------------------
- Program.cs — Configuración de la aplicación: DI, EF Core, autenticación JWT, Swagger y pipeline HTTP.
- Data/AppDbContext.cs — EF Core DbContext. DbSet<Usuario> Usuarios.
- Modelos/
  - Usuario.cs — Entidad que mapea la tabla Usuario. `contrasena` almacena el hash.
  - UsuarioDto.cs — DTO público para exponer usuarios sin campos sensibles.
  - LoginRequest.cs — DTO de entrada para login ({ Correo, Contrasena }).
  - LoginResponse.cs — DTO de respuesta para login ({ Mensaje, Token, IdUsuario }).
  - CreateUserRequest.cs — DTO para crear usuarios.

- Controllers/
  - AuthController.cs — POST /api/Auth/login. Valida entrada y delega en IAuthService.
  - UsersController.cs — GET /api/Users/admins (solo administradores) y POST /api/Users (crear usuario, Administrador requerido).

- Services/
  - IAuthService.cs / AuthService.cs — Validación de login, verificación de hash y generación de token (usa TokenService).
  - TokenService.cs — Creación de JWT (claims: id, email, name, role). No incluye contraseñas.
  - IPasswordService.cs / PasswordService.cs — Abstracción y uso de PasswordHasher<Usuario> para hash/verify.
  - IUserService.cs / UserService.cs — Lógica relacionada con usuarios: GetAdministradoresAsync y CreateUserAsync (valida input, hashea contraseña y persiste).

- Tools/MigrateAdminPassword/ — Utilidad de consola (temporal) para hashear y actualizar la contraseña de un usuario existente.

- Tests/ — Proyecto de pruebas xUnit con pruebas para AuthService y UserService (in-memory DB).

Endpoints y contratos
---------------------
- POST /api/Auth/login
  - Request: LoginRequest { Correo, Contrasena }
  - Response: LoginResponse { Mensaje, Token, IdUsuario }

- GET /api/Users/admins
  - Authorization: Bearer <token> con rol Administrador
  - Response: List<UsuarioDto>

- POST /api/Users
  - Authorization: Bearer <token> con rol Administrador
  - Request: CreateUserRequest { Correo, Contrasena, NombreUsuario, Rol }
  - Response: 201 Created + UsuarioDto

Inyección de dependencias (Program.cs)
-------------------------------------
- AddDbContext<AppDbContext>(...)
- AddScoped<IAuthService, AuthService>()
- AddScoped<IUserService, UserService>()
- AddScoped<IPasswordService, PasswordService>()
- AddScoped<TokenService>()

Seguridad y buenas prácticas aplicadas
-------------------------------------
- Contraseñas: almacenadas como hash usando PasswordHasher<Usuario> (ASP.NET Core).
- Tokens JWT firmados con clave en configuración (mover a secret manager en producción).
- No se devuelven contraseñas ni hashes en respuestas.
- Endpoints administrativos protegidos con [Authorize(Roles = "Administrador")].
- Validaciones básicas: formato de correo, longitud mínima de contraseña (>=8).

Cómo ejecutar localmente
------------------------
1. Compilar la solución:
   - dotnet build MedicionPQ.slnx
2. Ejecutar la API en desarrollo:
   - dotnet run --project MedicionPQ.csproj
   - Swagger disponible en Development (MapOpenApi + SwaggerUI).
3. Login (ejemplo):
   - POST /api/Auth/login
   - Body: { "Correo": "admin@gmail.com", "Contrasena": "admin12345678" }
4. Listar administradores:
   - GET /api/Users/admins con header Authorization: Bearer <token>

Migración de contraseñas (herramienta creada)
-------------------------------------------
- Tools/MigrateAdminPassword es una utilidad para calcular el hash y actualizar la columna `contrasena` de un usuario.
- Uso: dotnet run --project Tools/MigrateAdminPassword -- <email> <newPassword>
- Precaución: hacer backup antes. Eliminar o excluir esta carpeta del repo público después de su uso.

Pruebas unitarias
-----------------
- Proyecto Tests/MedicionPQ.Tests.csproj con xUnit y EF InMemory.
- Ejecutar tests:
  - dotnet test Tests/MedicionPQ.Tests.csproj

Dónde modificar comportamiento (guía rápida)
-------------------------------------------
- Cambiar duración de token o claims: Services/TokenService.cs
- Política y nombres de roles: Modelos/Usuario.cs (enum TipoRol) y uso de Authorize en controllers
- Reglas de contraseña: Services/UserService.cs (CreateUserAsync) y validaciones en controllers
- Proveedor de BD: Data/AppDbContext.cs y Program.cs (UseSqlServer → otro proveedor)

Notas para nuevos desarrolladores
--------------------------------
- Mantén las capas separadas: los controladores solo validan entrada y delegan a servicios.
- No pongas lógica de acceso a datos en los controladores.
- Si añades campos a Usuario, actualiza DTOs y mapeos en UserService y pruebas.
- Asegura que las modificaciones al esquema de la BD se acompañen con migraciones EF Core.

Lista rápida de archivos a revisar según tarea
-------------------------------------------
- Cambiar login/claims: Services/TokenService.cs, Services/AuthService.cs, Controllers/AuthController.cs
- Crear usuarios o cambiar validación: Services/UserService.cs, Controllers/UsersController.cs, Modelos/CreateUserRequest.cs
- Cambiar modelo Usuario: Modelos/Usuario.cs y Data/AppDbContext.cs
- Tests: Tests/*.cs

Contacto y mantenimiento
------------------------
- El que modifique autenticación o hashing debe entender PasswordHasher y seguridad JWT.
- Se recomienda añadir revisiones de seguridad antes de poner en producción.

Si quieres, puedo:
- Generar documentación Swagger enriquecida con las descripciones de DTOs, o
- Añadir un README más técnico con diagramas y comandos de despliegue.
