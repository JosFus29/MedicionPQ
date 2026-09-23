# Controllers — API endpoints

Resumen
-------
La carpeta `Controllers` expone la API HTTP. Cada controlador debe ser delgado: validar entrada, aplicar políticas de autorización y delegar la lógica al servicio correspondiente en `Services`.

Controladores actuales
---------------------
- `AuthController.cs` — POST `/api/Auth/login`. Valida formato de correo y longitud mínima de contraseña, llama a `IAuthService.ValidarLoginAsync` y devuelve `LoginResponse`.
- `UsersController.cs` — GET `/api/Users/admins` para listar administradores y POST `/api/Users` para crear usuarios. Ambos endpoints requieren rol `Administrador`.

Dónde se referencia
-------------------
- Los controladores consumen servicios registrados en `Program.cs` mediante inyección de dependencias (`IAuthService`, `IUserService`).
- DTOs importados de `Modelos/` (LoginRequest, LoginResponse, CreateUserRequest, UsuarioDto).

Buenas prácticas
---------------
- Mantener controladores sin lógica de acceso a datos.
- Validar la entrada en los controladores y realizar reglas de negocio en la capa de servicios.
- Proteger endpoints sensibles con `[Authorize]` y, de ser necesario, políticas más finas.

Editar/Extender
---------------
- Para exponer una nueva operación HTTP:
  1. Añadir DTOs en `Modelos/` si es necesario.
  2. Añadir método en la interfaz del servicio (`IUserService`, `IAuthService`, etc.) y su implementación.
  3. Añadir acción en el controlador que llame al servicio y devuelva el DTO apropiado.
  4. Escribir pruebas unitarias para el servicio y pruebas de integración para el endpoint si procede.
