using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;
using MedicionPQ.Services;

namespace MedicionPQ.Services;

/// <summary>
/// Servicio que encapsula la lógica de trabajo con usuarios (capa de servicios).
/// Aquí se deben poner las reglas de negocio relacionadas con usuarios y el acceso a datos.
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;

    public UserService(AppDbContext context, IPasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    /// <summary>
    /// Recupera todos los usuarios cuyo rol sea Administrador y los proyecta a UsuarioDto.
    /// </summary>
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

    /// <summary>
    /// Devuelve todos los usuarios del sistema (sin contraseñas).
    /// </summary>
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

    /// <summary>
    /// Crea un nuevo usuario validando correo y longitud de contraseña. Hashea la contraseña antes de persistir.
    /// Sólo almacena datos no sensibles en el DTO devuelto.
    /// </summary>
    public async Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> CreateUserAsync(string correo, string contrasena, string nombreUsuario, Usuario.TipoRol rol)
    {
        // Validaciones básicas
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrWhiteSpace(nombreUsuario))
            return (false, "Datos incompletos.", null);

        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailAttr.IsValid(correo)) return (false, "Correo inválido.", null);
        if (contrasena.Length < 8) return (false, "La contraseña debe tener al menos 8 caracteres.", null);

        // Verificar que no exista usuario con el mismo correo
        var existente = await _context.Usuarios.AnyAsync(u => u.correo == correo);
        if (existente) return (false, "Ya existe un usuario con ese correo.", null);

        var hashed = _passwordService.HashPassword(contrasena);

        var nuevo = new Usuario
        {
            correo = correo,
            contrasena = hashed,
            nombreUsuario = nombreUsuario,
            rol = rol,
            edo = true,
            tiempoSesion = 60
        };

        _context.Usuarios.Add(nuevo);
        await _context.SaveChangesAsync();

        var dto = new UsuarioDto
        {
            idUsuario = nuevo.idUsuario,
            correo = nuevo.correo,
            nombreUsuario = nuevo.nombreUsuario,
            rol = nuevo.rol,
            edo = nuevo.edo,
            tiempoSesion = nuevo.tiempoSesion
        };

        return (true, "Usuario creado.", dto);
    }

    /// <summary>
    /// Actualiza un usuario existente por id. No modifica la contraseña.
    /// </summary>
    public async Task<(bool Exito, string Mensaje, UsuarioDto? Usuario)> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.idUsuario == id);
        if (usuario == null) return (false, "Usuario no encontrado.", null);

        // Validaciones básicas
        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailAttr.IsValid(request.Correo)) return (false, "Correo inválido.", null);
        if (string.IsNullOrWhiteSpace(request.NombreUsuario)) return (false, "Nombre de usuario requerido.", null);

        // Verificar que el nuevo correo no pertenezca a otro usuario
        var otro = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == request.Correo && u.idUsuario != id);
        if (otro != null) return (false, "El correo ya está en uso por otro usuario.", null);

        // Actualizar campos permitidos
        usuario.correo = request.Correo;
        usuario.nombreUsuario = request.NombreUsuario;
        usuario.rol = request.Rol;
        usuario.edo = request.Edo;
        usuario.tiempoSesion = request.TiempoSesion;

        await _context.SaveChangesAsync();

        var dto = new UsuarioDto
        {
            idUsuario = usuario.idUsuario,
            correo = usuario.correo,
            nombreUsuario = usuario.nombreUsuario,
            rol = usuario.rol,
            edo = usuario.edo,
            tiempoSesion = usuario.tiempoSesion
        };

        return (true, "Usuario actualizado.", dto);
    }
}
