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

    /// <summary>Valida y registra una cuenta nueva.</summary>
    /// <param name="correo">Correo único para iniciar sesión.</param>
    /// <param name="contrasena">Contraseña en texto plano, que se convertirá en hash.</param>
    /// <param name="nombreUsuario">Nombre visible de la cuenta.</param>
    /// <param name="rol">Rol que se asignará al nuevo usuario.</param>
    /// <returns>Resultado de la operación, mensaje y DTO creado si tuvo éxito.</returns>
    Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> CreateUserAsync(string correo, string contrasena, string nombreUsuario, Usuario.TipoRol rol);

    /// <summary>Actualiza los datos editables de una cuenta existente.</summary>
    /// <param name="id">Identificador de la cuenta.</param>
    /// <param name="request">Campos que se guardarán; no permite cambiar la contraseña.</param>
    /// <returns>Resultado de la operación, mensaje y DTO actualizado si tuvo éxito.</returns>
    Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> UpdateUserAsync(int id, UpdateUserRequest request);
}
