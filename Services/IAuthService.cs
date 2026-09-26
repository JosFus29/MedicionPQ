using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>
/// Interfaz que define las operaciones de autenticación del sistema.
/// Implementada por Services/AuthService.cs.
/// Consumidores principales:
/// - Controllers/AuthController.cs (login endpoint)
/// - Cualquier servicio que necesite validar credenciales desde la capa de presentación
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Valida las credenciales y devuelve una tupla con el resultado, mensaje, token y la info del usuario.
    /// </summary>
    Task<(bool Exito, string Mensaje, string Token, Usuario? UsuarioInfo)> ValidarLoginAsync(string correo, string contrasena);

    /// <summary>
    /// Genera un nuevo token JWT para un usuario ya autenticado, con el fin de
    /// extender su sesión antes de que el token actual expire.
    /// </summary>
    /// <param name="idUsuario">Id del usuario, extraído del token actual (aún válido).</param>
    /// <returns>Tupla con el resultado, mensaje y el nuevo token generado.</returns>
    Task<(bool Exito, string Mensaje, string Token)> RenovarSesionAsync(int idUsuario);
}
