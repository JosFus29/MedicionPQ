using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>
/// Interfaz para operaciones relacionadas con usuarios.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Devuelve todos los usuarios con rol Administrador.
    /// </summary>
    Task<List<UsuarioDto>> GetAdministradoresAsync();

    /// <summary>
    /// Devuelve todos los usuarios (sin incluir contraseñas). Requiere privilegios administrativos.
    /// </summary>
    Task<List<UsuarioDto>> GetAllUsersAsync();

    /// <summary>
    /// Crea un nuevo usuario. Devuelve true si se creó correctamente y un mensaje.
    /// </summary>
    Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> CreateUserAsync(string correo, string contrasena, string nombreUsuario, Usuario.TipoRol rol);

    /// <summary>
    /// Actualiza un usuario existente por id. No modifica la contraseña.
    /// </summary>
    Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> UpdateUserAsync(int id, UpdateUserRequest request);
}
