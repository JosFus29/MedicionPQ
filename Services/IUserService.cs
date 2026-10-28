using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Define las operaciones disponibles para administrar cuentas de usuario.</summary>
public interface IUserService
{
    /// <summary>Obtiene todas las cuentas con rol Administrador.</summary>
    /// <returns>Lista de administradores sin hashes de contraseña.</returns>
    Task<List<UsuarioDto>> GetAdministradoresAsync();

    /// <summary>Obtiene todas las cuentas sin datos de contraseña.</summary>
    /// <returns>Lista pública de usuarios.</returns>
    Task<List<UsuarioDto>> GetAllUsersAsync();

    /// <summary>Registra públicamente una cuenta y asigna siempre el rol Usuario.</summary>
    Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> RegisterAsync(string correo, string contrasena, string nombreUsuario);

    /// <summary>Actualiza los datos editables de una cuenta existente.</summary>
    /// <param name="id">Identificador de la cuenta.</param>
    /// <param name="request">Campos que se guardarán; no permite cambiar la contraseña.</param>
    /// <param name="rolSolicitante">Rol obtenido del JWT para aplicar las restricciones sobre cuentas SuperAdministrador.</param>
    /// <returns>Resultado de la operación, mensaje y DTO actualizado si tuvo éxito.</returns>
    Task<(bool Exito, bool Prohibido, string Mensaje, UsuarioDto? Usuario)> UpdateUserAsync(
        int id,
        UpdateUserRequest request,
        Usuario.TipoRol rolSolicitante);

    /// <summary>
    /// Permite que el propio usuario que olvidó su contraseña cree una nueva con solo su correo.
    /// No hay una operación para que un administrador cambie la contraseña de otra cuenta.
    /// </summary>
    /// <param name="correo">Correo con el que el usuario inicia sesión.</param>
    /// <param name="nuevaContrasena">Contraseña nueva en texto plano, que se convertirá en hash.</param>
    /// <returns>Resultado de la operación y mensaje.</returns>
    Task<(bool Exito, string Mensaje)> RestablecerContrasenaAsync(string correo, string nuevaContrasena);
}
