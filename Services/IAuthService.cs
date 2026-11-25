using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Define las operaciones de autenticación disponibles para los controladores.</summary>
public interface IAuthService
{
    /// <summary>Valida las credenciales y devuelve el resultado del acceso.</summary>
    /// <param name="correoOUsuario">Correo de la cuenta o su nombre de usuario.</param>
    /// <param name="contrasena">Contraseña que se va a verificar.</param>
    /// <returns>Mensaje y, cuando el acceso se autoriza, token y datos de la cuenta.</returns>
    Task<(bool Exito, string Mensaje, string Token, DateTime? ExpiresAtUtc, int ExpiresIn, Usuario? UsuarioInfo)> ValidarLoginAsync(string correoOUsuario, string contrasena);

    /// <summary>Emite un JWT nuevo para una cuenta activa identificada por el token vigente.</summary>
    Task<(bool Exito, string Mensaje, TokenGenerado? Token, Usuario? UsuarioInfo)> RenovarTokenAsync(int idUsuario);
}
