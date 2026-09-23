using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;
using MedicionPQ.Services;

namespace MedicionPQ.Services;

/// <summary>
/// Service responsable de la lógica de autenticación.
/// Implementa IAuthService y realiza la validación de credenciales contra la base de datos.
/// Referencias principales:
/// - Data/AppDbContext.cs: EF Core DbContext inyectado.
/// - Services/TokenService.cs: servicio para generar JWT.
/// - Controllers/AuthController.cs: consume este servicio para el login.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IPasswordService _passwordService;

    /// <summary>
    /// Constructor que recibe el DbContext y el TokenService por DI.
    /// </summary>
    public AuthService(AppDbContext context, TokenService tokenService, IPasswordService passwordService)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordService = passwordService;
    }

    public async Task<(bool Exito, string Mensaje, string Token, Usuario? UsuarioInfo)> ValidarLoginAsync(string correo, string contrasena)
    {
        // Primero buscamos por correo únicamente (ahora las contraseñas están hasheadas)
        var usuarioEnDb = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.correo == correo);

        if (usuarioEnDb == null) return (false, "Credenciales incorrectas.", string.Empty, null);

        // Verificamos la contraseña usando el PasswordService
        var passwordValida = _passwordService.VerifyPassword(usuarioEnDb.contrasena, contrasena);
        if (!passwordValida) return (false, "Credenciales incorrectas.", string.Empty, null);

        if (!usuarioEnDb.edo) return (false, "El usuario está inactivo.", string.Empty, null);

        // NUEVO: Generamos el token criptográfico con los datos del usuario válido
        string tokenGenerado = _tokenService.GenerarToken(usuarioEnDb);

        return (true, "Inicio de sesión exitoso", tokenGenerado, usuarioEnDb);
    }
}