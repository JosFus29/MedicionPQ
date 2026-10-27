using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>Aplica las reglas para consultar, crear y actualizar cuentas de usuario.</summary>
public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    /// <summary>Crea el servicio con acceso a datos y al servicio de hash de contraseñas.</summary>
    public UserService(AppDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>Recupera las cuentas con rol Administrador o SuperAdministrador, sin hashes.</summary>
    /// <returns>Lista de cuentas administrativas como DTO públicos.</returns>
    public async Task<List<UsuarioDto>> GetAdministradoresAsync()
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.rol == Usuario.TipoRol.Administrador || u.rol == Usuario.TipoRol.SuperAdministrador)
            .Select(u => new UsuarioDto
            {
                idUsuario = u.idUsuario,
                correo = u.correo,
                nombreUsuario = u.nombreUsuario,
                rol = u.rol,
                edo = u.edo,
                tiempoSesion = u.tiempoSesion
            })
            .ToListAsync();
    }

    /// <summary>Devuelve todas las cuentas como DTO públicos, sin incluir sus hashes.</summary>
    /// <returns>Lista de usuarios.</returns>
    public async Task<List<UsuarioDto>> GetAllUsersAsync()
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Select(u => new UsuarioDto
            {
                idUsuario = u.idUsuario,
                correo = u.correo,
                nombreUsuario = u.nombreUsuario,
                rol = u.rol,
                edo = u.edo,
                tiempoSesion = u.tiempoSesion
            })
            .ToListAsync();
    }

    /// <summary>Crea una cuenta pública con rol Usuario sin aceptar un rol desde la solicitud.</summary>
    public Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> RegisterAsync(
        string correo,
        string contrasena,
        string nombreUsuario) =>
        CreateUserAsync(correo, contrasena, nombreUsuario, Usuario.TipoRol.Usuario);

    /// <summary>Valida los datos, evita correos duplicados, aplica hash y guarda una nueva cuenta.</summary>
    /// <param name="correo">Correo único de la cuenta.</param>
    /// <param name="contrasena">Contraseña en texto plano de al menos ocho caracteres.</param>
    /// <param name="nombreUsuario">Nombre visible de la cuenta.</param>
    /// <param name="rol">Rol asignado al usuario.</param>
    /// <returns>Resultado de la operación, mensaje y DTO creado si tiene éxito.</returns>
    public async Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> CreateUserAsync(string correo, string contrasena, string nombreUsuario, Usuario.TipoRol rol)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrWhiteSpace(nombreUsuario))
            return (false, "Datos incompletos.", null);

        correo = correo.Trim().ToLowerInvariant();
        nombreUsuario = nombreUsuario.Trim();
        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailAttr.IsValid(correo)) return (false, "Correo inválido.", null);
        if (!PasswordPolicy.IsValid(contrasena)) return (false, PasswordPolicy.ErrorMessage, null);
        if (!Enum.IsDefined(rol)) return (false, "Rol inválido.", null);

        var existente = await _context.Usuarios.AnyAsync(u => u.correo == correo);
        if (existente) return (false, "Ya existe un usuario con ese correo.", null);

        var nuevo = new Usuario
        {
            correo = correo,
            contrasena = _passwordService.HashPassword(contrasena),
            nombreUsuario = nombreUsuario,
            rol = rol,
            edo = true,
            tiempoSesion = 60
        };

        _context.Usuarios.Add(nuevo);
        await _context.SaveChangesAsync();

        return (true, "Usuario creado.", new UsuarioDto
        {
            idUsuario = nuevo.idUsuario,
            correo = nuevo.correo,
            nombreUsuario = nuevo.nombreUsuario,
            rol = nuevo.rol,
            edo = nuevo.edo,
            tiempoSesion = nuevo.tiempoSesion
        });
    }

    /// <summary>Actualiza los campos editables de una cuenta existente; no cambia su contraseña.</summary>
    /// <param name="id">Identificador de la cuenta.</param>
    /// <param name="request">Valores nuevos de correo, nombre, rol, estado y duración de sesión.</param>
    /// <param name="rolSolicitante">Rol obtenido del JWT para aplicar las restricciones sobre cuentas SuperAdministrador.</param>
    /// <returns>Resultado de la operación, mensaje y DTO actualizado si tiene éxito.</returns>
    public async Task<(bool Exito, bool Prohibido, string Mensaje, UsuarioDto? Usuario)> UpdateUserAsync(
        int id,
        UpdateUserRequest request,
        Usuario.TipoRol rolSolicitante)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.idUsuario == id);
        if (usuario == null) return (false, false, "Usuario no encontrado.", null);

        if (rolSolicitante != Usuario.TipoRol.SuperAdministrador &&
            (usuario.rol == Usuario.TipoRol.SuperAdministrador || request.Rol == Usuario.TipoRol.SuperAdministrador))
            return (false, true, "Solo un SuperAdministrador puede editar cuentas de SuperAdministrador o asignar ese rol.", null);

        var correoNormalizado = request.Correo?.Trim().ToLowerInvariant();
        var nombreNormalizado = request.NombreUsuario?.Trim();
        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (string.IsNullOrWhiteSpace(correoNormalizado) || !emailAttr.IsValid(correoNormalizado))
            return (false, false, "Correo inválido.", null);
        if (string.IsNullOrWhiteSpace(nombreNormalizado)) return (false, false, "Nombre de usuario requerido.", null);
        if (!Enum.IsDefined(request.Rol)) return (false, false, "Rol inválido.", null);
        if (request.TiempoSesion is < 1 or > 1440) return (false, false, "El tiempo de sesión debe estar entre 1 y 1440 minutos.", null);

        var otro = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == correoNormalizado && u.idUsuario != id);
        if (otro != null) return (false, false, "El correo ya está en uso por otro usuario.", null);

        usuario.correo = correoNormalizado;
        usuario.nombreUsuario = nombreNormalizado;
        usuario.rol = request.Rol;
        usuario.edo = request.Edo;
        usuario.tiempoSesion = request.TiempoSesion;

        await _context.SaveChangesAsync();

        return (true, false, "Usuario actualizado.", new UsuarioDto
        {
            idUsuario = usuario.idUsuario,
            correo = usuario.correo,
            nombreUsuario = usuario.nombreUsuario,
            rol = usuario.rol,
            edo = usuario.edo,
            tiempoSesion = usuario.tiempoSesion
        });
    }

    /// <summary>
    /// Reemplaza el hash de contraseña de una cuenta por el de una contraseña nueva.
    /// La contraseña anterior no se puede consultar porque solo se guarda su hash.
    /// </summary>
    /// <param name="id">Identificador de la cuenta.</param>
    /// <param name="nuevaContrasena">Contraseña nueva de al menos ocho caracteres.</param>
    /// <returns>Resultado de la operación y mensaje.</returns>
    public async Task<(bool Exito, string Mensaje)> RestablecerContrasenaAsync(int id, string nuevaContrasena)
    {
        if (!PasswordPolicy.IsValid(nuevaContrasena))
            return (false, PasswordPolicy.ErrorMessage);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.idUsuario == id);
        if (usuario == null) return (false, "Usuario no encontrado.");

        usuario.contrasena = _passwordService.HashPassword(nuevaContrasena);
        await _context.SaveChangesAsync();

        return (true, "Contraseña restablecida.");
    }
}
