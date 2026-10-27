using System.Security.Cryptography;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>
/// Recuperación de contraseña con código por correo.
/// Reglas: el código tiene 6 dígitos, vence en 15 minutos, solo se usa una vez,
/// se invalida después de 5 intentos incorrectos y no se puede pedir otro antes de 60 segundos.
/// El código nunca se guarda ni se registra en el log: solo su hash.
/// </summary>
public class RecuperacionContrasenaService : IRecuperacionContrasenaService
{
    /// <summary>Minutos que dura un código antes de vencer.</summary>
    public const int MinutosVigencia = 15;

    /// <summary>Intentos incorrectos permitidos antes de invalidar el código.</summary>
    public const int IntentosMaximos = 5;

    /// <summary>Segundos mínimos entre dos solicitudes de código del mismo usuario.</summary>
    public const int SegundosEntreSolicitudes = 60;

    /// <summary>Mensaje único para cualquier código rechazado, para no dar pistas de qué falló.</summary>
    public const string MensajeCodigoInvalido = "Código inválido o vencido.";

    private readonly AppDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RecuperacionContrasenaService> _logger;

    /// <summary>Recibe el acceso a datos, el hash de contraseñas, el envío de correos y el logger.</summary>
    public RecuperacionContrasenaService(
        AppDbContext context,
        IPasswordService passwordService,
        IEmailService emailService,
        ILogger<RecuperacionContrasenaService> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SolicitarCodigoAsync(string correo)
    {
        var correoNormalizado = correo.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == correoNormalizado);
        if (usuario is null || !usuario.edo)
        {
            _logger.LogWarning("Recuperación de contraseña pedida para un correo no registrado o inactivo: {Correo}", correoNormalizado);
            return;
        }

        var ahora = DateTime.UtcNow;
        var pendientes = await _context.RecuperacionesContrasena
            .Where(r => r.idUsuario == usuario.idUsuario && !r.usado)
            .ToListAsync();

        // Evita que alguien llene el buzón del usuario pidiendo códigos sin parar.
        if (pendientes.Any(r => r.fechaCreacion > ahora.AddSeconds(-SegundosEntreSolicitudes)))
        {
            _logger.LogWarning("Recuperación ignorada para {Correo}: ya se pidió un código hace menos de {Segundos} segundos", correoNormalizado, SegundosEntreSolicitudes);
            return;
        }

        // Solo el código más reciente es válido: los anteriores se invalidan.
        foreach (var anterior in pendientes)
            anterior.usado = true;

        // RandomNumberGenerator es criptográficamente seguro (Random no lo es).
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        _context.RecuperacionesContrasena.Add(new RecuperacionContrasena
        {
            idUsuario = usuario.idUsuario,
            codigoHash = _passwordService.HashPassword(codigo),
            expira = ahora.AddMinutes(MinutosVigencia),
            intentos = 0,
            usado = false,
            fechaCreacion = ahora
        });
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.EnviarAsync(
                usuario.correo,
                "Código para recuperar tu contraseña - MedicionPQ",
                $"Hola {usuario.nombreUsuario}:\n\nTu código para crear una contraseña nueva es: {codigo}\n\n" +
                $"Vence en {MinutosVigencia} minutos y solo se puede usar una vez.\n" +
                "Si no pediste este código, ignora este correo: tu contraseña no ha cambiado.");

            _logger.LogInformation("Código de recuperación de contraseña enviado a {Correo}", correoNormalizado);
        }
        catch (Exception ex)
        {
            // No se lanza la excepción: la respuesta al cliente debe ser la misma exista o no el correo.
            _logger.LogError(ex, "No se pudo enviar el código de recuperación a {Correo}", correoNormalizado);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Exito, string Mensaje)> RestablecerAsync(string correo, string codigo, string nuevaContrasena)
    {
        if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Length < 8)
            return (false, "La contraseña debe tener al menos 8 caracteres.");

        var correoNormalizado = correo.Trim().ToLowerInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == correoNormalizado);
        if (usuario is null || !usuario.edo)
            return (false, MensajeCodigoInvalido);

        var registro = await _context.RecuperacionesContrasena
            .Where(r => r.idUsuario == usuario.idUsuario && !r.usado)
            .OrderByDescending(r => r.fechaCreacion)
            .FirstOrDefaultAsync();

        if (registro is null || registro.expira < DateTime.UtcNow)
            return (false, MensajeCodigoInvalido);

        if (!_passwordService.VerifyPassword(registro.codigoHash, codigo.Trim()))
        {
            registro.intentos++;
            if (registro.intentos >= IntentosMaximos)
                registro.usado = true;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Código de recuperación incorrecto para {Correo} (intento {Intento} de {Maximo})",
                correoNormalizado, registro.intentos, IntentosMaximos);
            return (false, MensajeCodigoInvalido);
        }

        usuario.contrasena = _passwordService.HashPassword(nuevaContrasena);
        registro.usado = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Contraseña restablecida con código de recuperación: {Correo}", correoNormalizado);
        return (true, "Contraseña actualizada. Ya puedes iniciar sesión con la nueva.");
    }
}
