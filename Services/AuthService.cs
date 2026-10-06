using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Valida credenciales y gestiona los JWT de usuarios activos.</summary>
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
    /// <param name="correo">Correo electrónico de la cuenta.</param>
    /// <param name="contrasena">Contraseña que se verifica contra el hash guardado.</param>
    /// <returns>Resultado de autenticación y token emitido cuando las credenciales son válidas.</returns>
    public async Task<(bool Exito, string Mensaje, string Token, DateTime? ExpiresAtUtc, int ExpiresIn, Usuario? UsuarioInfo)> ValidarLoginAsync(string correo, string contrasena)
    {
        var correoNormalizado = correo.Trim().ToLowerInvariant();
        var usuarioEnDb = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.correo == correoNormalizado);
        if (usuarioEnDb is null) return (false, "Credenciales incorrectas.", string.Empty, null, 0, null);

        var passwordValida = _passwordService.VerifyPassword(usuarioEnDb.contrasena, contrasena);
        if (!passwordValida) return (false, "Credenciales incorrectas.", string.Empty, null, 0, null);
        if (!usuarioEnDb.edo) return (false, "El usuario está inactivo.", string.Empty, null, 0, null);
        if (usuarioEnDb.tiempoSesion is < 1 or > 1440)
            return (false, "El tiempo de sesión configurado para el usuario no es válido.", string.Empty, null, 0, null);

        var token = _tokenService.GenerarToken(usuarioEnDb);
        return (true, "Inicio de sesión exitoso", token.Token, token.ExpiresAtUtc, token.ExpiresIn, usuarioEnDb);
    }

    /// <summary>Vuelve a leer la cuenta para impedir renovar sesiones de usuarios inactivos o eliminados.</summary>
    public async Task<(bool Exito, string Mensaje, TokenGenerado? Token, Usuario? UsuarioInfo)> RenovarTokenAsync(int idUsuario)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.idUsuario == idUsuario);
        if (usuario is null || !usuario.edo)
            return (false, "La cuenta no existe o está inactiva.", null, null);
        if (usuario.tiempoSesion is < 1 or > 1440)
            return (false, "El tiempo de sesión configurado para el usuario no es válido.", null, null);

        var token = _tokenService.GenerarToken(usuario);
        return (true, "Token renovado exitosamente.", token, usuario);
    }
}
