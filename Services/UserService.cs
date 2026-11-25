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

    /// <summary>
    /// Crea una cuenta pública con rol Usuario sin aceptar un rol desde la solicitud.
    /// La cuenta queda inactiva (<c>edo = false</c>): no puede iniciar sesión hasta que un
    /// Administrador o SuperAdministrador la apruebe activándola.
    /// </summary>
    public async Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> RegisterAsync(
        string correo,
        string contrasena,
        string nombreUsuario)
    {
        var (exito, mensaje, usuario) = await CreateUserAsync(correo, contrasena, nombreUsuario, Usuario.TipoRol.Usuario, activo: false);
        return exito
            ? (true, MensajeRegistroPendiente, usuario)
            : (false, mensaje, null);
    }

    /// <summary>Mensaje que recibe quien se registra: su cuenta espera la aprobación de un administrador.</summary>
    public const string MensajeRegistroPendiente =
        "Cuenta creada. Un administrador debe aprobarla antes de que puedas iniciar sesión.";

    /// <summary>Valida los datos, evita correos duplicados, aplica hash y guarda una nueva cuenta.</summary>
    /// <param name="correo">Correo único de la cuenta.</param>
    /// <param name="contrasena">Contraseña en texto plano de al menos ocho caracteres.</param>
    /// <param name="nombreUsuario">Nombre visible de la cuenta.</param>
    /// <param name="rol">Rol asignado al usuario.</param>
    /// <param name="activo">Estado inicial de la cuenta; <c>false</c> la deja pendiente de aprobación.</param>
    /// <returns>Resultado de la operación, mensaje y DTO creado si tiene éxito.</returns>
    public async Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> CreateUserAsync(string correo, string contrasena, string nombreUsuario, Usuario.TipoRol rol, bool activo = true)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrWhiteSpace(nombreUsuario))
            return (false, "Datos incompletos.", null);

        correo = correo.Trim().ToLowerInvariant();
        nombreUsuario = NombreUsuarioPolicy.Normalizar(nombreUsuario);
        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailAttr.IsValid(correo)) return (false, "Correo inválido.", null);
        if (NombreUsuarioPolicy.Validar(nombreUsuario) is { } errorNombre) return (false, errorNombre, null);
        if (!PasswordPolicy.IsValid(contrasena)) return (false, PasswordPolicy.ErrorMessage, null);
        if (!Enum.IsDefined(rol)) return (false, "Rol inválido.", null);

        var existente = await _context.Usuarios.AnyAsync(u => u.correo == correo);
        if (existente) return (false, "Ya existe un usuario con ese correo.", null);

        // El nombre de usuario también sirve para iniciar sesión: no se puede repetir (sin importar mayúsculas).
        var claveNombre = NombreUsuarioPolicy.Clave(nombreUsuario);
        if (await _context.Usuarios.AnyAsync(u => u.nombreUsuario.ToLower() == claveNombre))
            return (false, NombreUsuarioPolicy.MensajeDuplicadoRegistro, null);

        var nuevo = new Usuario
        {
            correo = correo,
            contrasena = _passwordService.HashPassword(contrasena),
            nombreUsuario = nombreUsuario,
            rol = rol,
            edo = activo,
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
        var nombreNormalizado = NombreUsuarioPolicy.Normalizar(request.NombreUsuario);
        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (string.IsNullOrWhiteSpace(correoNormalizado) || !emailAttr.IsValid(correoNormalizado))
            return (false, false, "Correo inválido.", null);
        // Las reglas del nombre solo se aplican si cambia, para poder editar otros datos de cuentas
        // antiguas cuyo nombre no las cumpla.
        var claveNombre = NombreUsuarioPolicy.Clave(nombreNormalizado);
        var cambiaNombre = claveNombre != NombreUsuarioPolicy.Clave(usuario.nombreUsuario);
        if (string.IsNullOrEmpty(nombreNormalizado)) return (false, false, "Nombre de usuario requerido.", null);
        if (cambiaNombre && NombreUsuarioPolicy.Validar(nombreNormalizado) is { } errorNombre) return (false, false, errorNombre, null);
        if (!Enum.IsDefined(request.Rol)) return (false, false, "Rol inválido.", null);
        if (request.TiempoSesion is < 1 or > 1440) return (false, false, "El tiempo de sesión debe estar entre 1 y 1440 minutos.", null);

        var otro = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == correoNormalizado && u.idUsuario != id);
        if (otro != null) return (false, false, "El correo ya está en uso por otro usuario.", null);

        if (cambiaNombre && await _context.Usuarios.AnyAsync(u => u.nombreUsuario.ToLower() == claveNombre && u.idUsuario != id))
            return (false, false, NombreUsuarioPolicy.MensajeDuplicadoEdicion, null);

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
}
