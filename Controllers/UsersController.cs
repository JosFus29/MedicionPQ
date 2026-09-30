using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MedicionPQ.Services;
using MedicionPQ.Modelos;

namespace MedicionPQ.Controllers;

/// <summary>
/// Controlador para operaciones relacionadas con usuarios.
/// Separa la lógica HTTP del servicio UserService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>Inicializa el controlador con el servicio de usuarios.</summary>
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// GET /api/Users/admins
    /// Devuelve la lista de usuarios que tienen rol Administrador.
    /// Requiere autenticación y que el token contenga el rol 'Administrador'.
    /// </summary>
    [HttpGet("admins")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GetAdministradores()
    {
        var admins = await _userService.GetAdministradoresAsync();
        return Ok(admins);
    }

    /// <summary>
    /// GET /api/Users
    /// Devuelve todos los usuarios. Solo administradores pueden acceder.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// POST /api/Users
    /// Crea un nuevo usuario. Solo administradores pueden acceder.
    /// </summary>
    /// <param name="request">Datos necesarios para crear el usuario (correo, contrasena, nombre y rol).</param>
    /// <returns>201 Created con el UsuarioDto del usuario creado o 400 BadRequest con mensaje en caso de error.</returns>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var (exito, mensaje, usuario) = await _userService.CreateUserAsync(request.Correo, request.Contrasena, request.NombreUsuario, request.Rol);
        if (!exito) return BadRequest(new { mensaje });

        return CreatedAtAction(nameof(GetAll), new { }, usuario);
    }

    /// <summary>
    /// PUT /api/Users/{id}
    /// Actualiza los datos de un usuario existente (no actualiza la contraseña). Solo administradores.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var (exito, mensaje, usuario) = await _userService.UpdateUserAsync(id, request);
        if (!exito) return BadRequest(new { mensaje });

        return Ok(usuario);
    }
}
