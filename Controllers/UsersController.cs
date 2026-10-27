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
    /// Devuelve la lista de cuentas con rol Administrador o SuperAdministrador.
    /// Requiere autenticación con cualquiera de esos roles.
    /// </summary>
    [HttpGet("admins")]
    [Authorize(Roles = "SuperAdministrador,Administrador")]
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
    [Authorize(Roles = "SuperAdministrador,Administrador")]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// PUT /api/Users/{id}
    /// Actualiza los datos de un usuario existente (no actualiza la contraseña). Solo administradores.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdministrador,Administrador")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var rolSolicitante = User.IsInRole(nameof(Usuario.TipoRol.SuperAdministrador))
            ? Usuario.TipoRol.SuperAdministrador
            : Usuario.TipoRol.Administrador;

        var (exito, prohibido, mensaje, usuario) = await _userService.UpdateUserAsync(id, request, rolSolicitante);
        if (prohibido) return Forbid();
        if (!exito) return BadRequest(new { mensaje });

        return Ok(usuario);
    }
}
