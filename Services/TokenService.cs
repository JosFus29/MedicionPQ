using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>
/// Servicio encargado de la generación de JSON Web Tokens (JWT) para la autenticación y autorización en la API.
/// Este servicio sólo se encarga de crear tokens firmados. La validación de credenciales y la lógica
/// de negocio deben residir en AuthService (Services/AuthService.cs) que consume este servicio.
/// </summary>
public class TokenService
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Inicializa una nueva instancia inyectando la configuración de la aplicación.
    /// </summary>
    /// <param name="config">Interfaz para acceder a appsettings.json y leer las credenciales del servidor.</param>
    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Genera un token JWT firmado digitalmente para un usuario validado.
    /// </summary>
    /// <param name="usuario">El objeto de usuario que contiene los datos para el payload (ID, correo, tiempo de sesión).</param>
    /// <returns>Una cadena de texto (string) que representa el token JWT codificado.</returns>
    public string GenerarToken(Usuario usuario)
    {
        // 1. Claims (Carga útil / Payload)
        // Son los datos públicos (pero no modificables) que viajarán dentro del token.
        // Aquí incrustamos el ID del usuario y su correo para identificarlos en cada petición HTTP futura.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.idUsuario.ToString()),
            new Claim(ClaimTypes.Email, usuario.correo),
            new Claim(ClaimTypes.Name, usuario.nombreUsuario),
            new Claim(ClaimTypes.Role, usuario.rol.ToString())
            
        };

        // 2. Firma del Token (Signature)
        // Se extrae la clave secreta desde la configuración (Jwt:Key) y se encripta usando HMAC SHA-256.
        // Esto garantiza que si alguien altera el token en el navegador, la firma no coincidirá y el servidor lo rechazará.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 3. Tiempo de Expiración
        // Calcula exactamente en qué momento caducará el token sumando el tiempo de sesión del usuario a la hora actual UTC.
        var expira = DateTime.UtcNow.AddMinutes(usuario.tiempoSesion);

        // 4. Construcción del Token
        // Ensambla todas las piezas necesarias: quién lo emite (Issuer), quién lo consume (Audience),
        // los datos (Claims), expiración y la firma de seguridad.
        // var identity = new ClaimsIdentity(claims);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: creds
        );

        // 5. Serialización
        // Convierte el objeto JwtSecurityToken en el formato estándar de texto (header.payload.signature) para enviarlo al cliente.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}