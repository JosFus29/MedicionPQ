using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MedicionPQ.Modelos;
using Microsoft.IdentityModel.Tokens;

namespace MedicionPQ.Services;

/// <summary>Crea tokens JWT firmados para identificar y autorizar a una cuenta autenticada.</summary>
public class TokenService
{
    private readonly IConfiguration _config;

    /// <summary>Inicializa el servicio con la configuración de emisor, audiencia y clave JWT.</summary>
    /// <param name="config">Configuración cargada por ASP.NET Core.</param>
    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>Genera un JWT firmado con la identidad, rol y vigencia de la cuenta indicada.</summary>
    /// <param name="usuario">Cuenta previamente autenticada.</param>
    /// <returns>Token JWT serializado para enviar al cliente.</returns>
    public TokenGenerado GenerarToken(Usuario usuario)
    {
        if (usuario.tiempoSesion is < 1 or > 1440)
            throw new ArgumentOutOfRangeException(nameof(usuario), "El tiempo de sesión debe estar entre 1 y 1440 minutos.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.idUsuario.ToString()),
            new Claim(ClaimTypes.Email, usuario.correo),
            new Claim(ClaimTypes.Name, usuario.nombreUsuario),
            new Claim(ClaimTypes.Role, usuario.rol.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(usuario.tiempoSesion);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new TokenGenerado(new JwtSecurityTokenHandler().WriteToken(token), expiresAt, usuario.tiempoSesion * 60);
    }
}

/// <summary>JWT y vigencia exacta emitidos para una sesión.</summary>
public sealed record TokenGenerado(string Token, DateTime ExpiresAtUtc, int ExpiresIn);
