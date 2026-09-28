using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Valida credenciales y solicita la creación de un token para el usuario activo.</summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IPasswordService _passwordService;

    /// <summary>Inicializa el servicio con acceso a datos, tokens y verificación de contraseñas.</summary>
    public AuthService(AppDbContext context, TokenService tokenService, IPasswordService passwordService)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordService = passwordService;
    }

    /// <summary>Valida las credenciales y crea un JWT únicamente para una cuenta activa.</summary>
    /// <param name="correo">Correo que identifica la cuenta.</param>
    /// <param name="contrasena">Contraseña enviada por el cliente; solo se verifica contra el hash almacenado.</param>
    /// <returns>Resultado, mensaje, token y cuenta autenticada. En caso de error, el token queda vacío.</returns>
    public async Task<(bool Exito, string Mensaje, string Token, Usuario? UsuarioInfo)> ValidarLoginAsync(string correo, string contrasena)
    {
        var usuarioEnDb = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == correo);
        if (usuarioEnDb == null) return (false, "Credenciales incorrectas.", string.Empty, null);

        var passwordValida = _passwordService.VerifyPassword(usuarioEnDb.contrasena, contrasena);
        if (!passwordValida) return (false, "Credenciales incorrectas.", string.Empty, null);
        if (!usuarioEnDb.edo) return (false, "El usuario está inactivo.", string.Empty, null);

        var tokenGenerado = _tokenService.GenerarToken(usuarioEnDb);
        return (true, "Inicio de sesión exitoso", tokenGenerado, usuarioEnDb);
    }
}
