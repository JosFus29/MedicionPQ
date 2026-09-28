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

    /// <summary>Recupera las cuentas con rol Administrador, sin incluir hashes de contraseña.</summary>
    /// <returns>Lista de administradores como DTO públicos.</returns>
    public async Task<List<UsuarioDto>> GetAdministradoresAsync()
    {
        return await _context.Usuarios
            .Where(u => u.rol == Usuario.TipoRol.Administrador)
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

        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailAttr.IsValid(correo)) return (false, "Correo inválido.", null);
        if (contrasena.Length < 8) return (false, "La contraseña debe tener al menos 8 caracteres.", null);

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
    /// <returns>Resultado de la operación, mensaje y DTO actualizado si tiene éxito.</returns>
    public async Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.idUsuario == id);
        if (usuario == null) return (false, "Usuario no encontrado.", null);

        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailAttr.IsValid(request.Correo)) return (false, "Correo inválido.", null);
        if (string.IsNullOrWhiteSpace(request.NombreUsuario)) return (false, "Nombre de usuario requerido.", null);

        var otro = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == request.Correo && u.idUsuario != id);
        if (otro != null) return (false, "El correo ya está en uso por otro usuario.", null);

        usuario.correo = request.Correo;
        usuario.nombreUsuario = request.NombreUsuario;
        usuario.rol = request.Rol;
        usuario.edo = request.Edo;
        usuario.tiempoSesion = request.TiempoSesion;

        await _context.SaveChangesAsync();

        return (true, "Usuario actualizado.", new UsuarioDto
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
