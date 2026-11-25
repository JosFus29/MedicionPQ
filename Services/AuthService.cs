using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Valida credenciales y gestiona los JWT de usuarios activos.</summary>
public class AuthService : IAuthService
{
    /// <summary>
    /// Mensaje para una cuenta con <c>edo = false</c>: puede ser una cuenta recién registrada que todavía
    /// no aprueba un administrador, o una que un administrador desactivó. Solo se muestra si la contraseña es correcta.
    /// </summary>
    public const string MensajeCuentaInactiva =
        "Tu cuenta está pendiente de aprobación o fue desactivada. Un administrador debe activarla para que puedas iniciar sesión.";

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
    public async Task<(bool Exito, string Mensaje, string Token, DateTime? ExpiresAtUtc, int ExpiresIn, Usuario? UsuarioInfo)> ValidarLoginAsync(string correoOUsuario, string contrasena)
    {
        var usuarioEnDb = await BuscarCuentaAsync(correoOUsuario);
        if (usuarioEnDb is null) return (false, "Credenciales incorrectas.", string.Empty, null, 0, null);

        var passwordValida = _passwordService.VerifyPassword(usuarioEnDb.contrasena, contrasena);
        if (!passwordValida) return (false, "Credenciales incorrectas.", string.Empty, null, 0, null);
        if (!usuarioEnDb.edo) return (false, MensajeCuentaInactiva, string.Empty, null, 0, null);
        if (usuarioEnDb.tiempoSesion is < 1 or > 1440)
            return (false, "El tiempo de sesión configurado para el usuario no es válido.", string.Empty, null, 0, null);

        var token = _tokenService.GenerarToken(usuarioEnDb);
        return (true, "Inicio de sesión exitoso", token.Token, token.ExpiresAtUtc, token.ExpiresIn, usuarioEnDb);
    }

    /// <summary>
    /// Busca la cuenta con la que se quiere iniciar sesión: por correo si el texto lleva "@" y, si no, por nombre de usuario
    /// (sin importar mayúsculas ni espacios de más). Si el nombre estuviera repetido en una base sin el índice único,
    /// no se elige ninguna cuenta, para no entrar a la equivocada.
    /// </summary>
    /// <param name="correoOUsuario">Correo o nombre de usuario escrito en el login.</param>
    /// <returns>La cuenta encontrada o null.</returns>
    private async Task<Usuario?> BuscarCuentaAsync(string? correoOUsuario)
    {
        var entrada = correoOUsuario?.Trim() ?? string.Empty;
        if (entrada.Length == 0) return null;

        if (entrada.Contains('@'))
        {
            var correoNormalizado = entrada.ToLowerInvariant();
            return await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.correo == correoNormalizado);
        }

        var clave = NombreUsuarioPolicy.Clave(entrada);
        var coincidencias = await _context.Usuarios.AsNoTracking()
            .Where(u => u.nombreUsuario.ToLower() == clave)
            .Take(2)
            .ToListAsync();
        return coincidencias.Count == 1 ? coincidencias[0] : null;
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
