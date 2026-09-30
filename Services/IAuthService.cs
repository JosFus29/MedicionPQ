using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Define las operaciones de autenticación disponibles para los controladores.</summary>
public interface IAuthService
{
    /// <summary>Valida las credenciales y devuelve el resultado del acceso.</summary>
    /// <param name="correo">Correo de la cuenta.</param>
    /// <param name="contrasena">Contraseña que se va a verificar.</param>
    /// <returns>Mensaje y, cuando el acceso se autoriza, token y datos de la cuenta.</returns>
    Task<(bool Exito, string Mensaje, string Token, DateTime? ExpiresAtUtc, int ExpiresIn, Usuario? UsuarioInfo)> ValidarLoginAsync(string correo, string contrasena);
}
