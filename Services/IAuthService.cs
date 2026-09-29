using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Define las operaciones de autenticación disponibles para los controladores.</summary>
public interface IAuthService
{
    /// <summary>Valida las credenciales y devuelve el resultado del acceso.</summary>
    /// <param name="correo">Correo de la cuenta.</param>
    /// <param name="contrasena">Contraseña que se va a verificar.</param>
    /// <returns>Mensaje y, cuando el acceso se autoriza, token y datos de la cuenta.</returns>
    Task<(bool Exito, string Mensaje, string Token, Usuario? UsuarioInfo)> ValidarLoginAsync(string correo, string contrasena);

    /// <summary>
    /// Genera un nuevo token JWT para un usuario ya autenticado, con el fin de
    /// extender su sesión antes de que el token actual expire.
    /// </summary>
    /// <param name="idUsuario">Id del usuario, extraído del token actual (aún válido).</param>
    /// <returns>Tupla con el resultado, mensaje y el nuevo token generado.</returns>
    Task<(bool Exito, string Mensaje, string Token)> RenovarSesionAsync(int idUsuario);
}
