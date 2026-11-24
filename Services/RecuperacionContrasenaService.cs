using System.Security.Cryptography;
using MedicionPQ.Data;
using MedicionPQ.Modelos;
using Microsoft.EntityFrameworkCore;

namespace MedicionPQ.Services;

/// <summary>
/// Cambio de contraseña con código de verificación enviado por correo. Es el único camino para
/// cambiar una contraseña y aplica igual a Usuario, Administrador y SuperAdministrador.
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

    /// <summary>Mensaje cuando el código es correcto en <c>POST /api/Contrasena/verificar-codigo</c>.</summary>
    public const string MensajeCodigoValido = "Código válido.";

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
            _logger.LogWarning("Código de contraseña pedido para un correo no registrado o inactivo: {Correo}", correoNormalizado);
            return;
        }

        var ahora = DateTime.UtcNow;
        var pendientes = await _context.RecuperacionesContrasena
            .Where(r => r.idUsuario == usuario.idUsuario && !r.usado)
            .ToListAsync();

        // Evita que alguien llene el buzón del usuario pidiendo códigos sin parar.
        if (pendientes.Any(r => r.fechaCreacion > ahora.AddSeconds(-SegundosEntreSolicitudes)))
        {
            _logger.LogWarning("Código de contraseña ignorado para {Correo}: ya se pidió uno hace menos de {Segundos} segundos", correoNormalizado, SegundosEntreSolicitudes);
            return;
        }

        // Solo el código más reciente es válido: los anteriores se invalidan.
        foreach (var anterior in pendientes)
            anterior.usado = true;

        // RandomNumberGenerator es criptográficamente seguro (Random no lo es).
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        var registro = new RecuperacionContrasena
        {
            idUsuario = usuario.idUsuario,
            codigoHash = _passwordService.HashPassword(codigo),
            expira = ahora.AddMinutes(MinutosVigencia),
            intentos = 0,
            usado = false,
            fechaCreacion = ahora
        };
        _context.RecuperacionesContrasena.Add(registro);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.EnviarAsync(
                usuario.correo,
                "Código de verificación para cambiar tu contraseña - MedicionPQ",
                $"Hola {usuario.nombreUsuario}:\n\nTu código de verificación para cambiar tu contraseña es: {codigo}\n\n" +
                $"Vence en {MinutosVigencia} minutos y solo se puede usar una vez.\n" +
                "Si no pediste este código, ignora este correo: tu contraseña no ha cambiado.");

            _logger.LogInformation("Código de verificación de contraseña enviado a {Correo}", correoNormalizado);
        }
        catch (Exception ex)
        {
            // No se lanza la excepción: la respuesta al cliente debe ser la misma exista o no el correo.
            // El detalle técnico ya lo registró EmailService; aquí solo se deja el motivo.
            _logger.LogError("No se pudo enviar el código de verificación a {Correo}: {Motivo}", correoNormalizado, ex.Message);

            // El código nunca llegó: se invalida para que el usuario pueda pedir otro sin esperar.
            registro.usado = true;
            await _context.SaveChangesAsync();
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Exito, string Mensaje)> VerificarCodigoAsync(string correo, string codigo)
    {
        var (_, registro) = await ValidarCodigoAsync(correo, codigo);
        if (registro is null)
            return (false, MensajeCodigoInvalido);

        // No se marca como usado: el mismo código se vuelve a enviar y validar en RestablecerAsync.
        _logger.LogInformation("Código de verificación válido (aún no usado) para {Correo}", correo.Trim().ToLowerInvariant());
        return (true, MensajeCodigoValido);
    }

    /// <inheritdoc/>
    public async Task<(bool Exito, string Mensaje)> RestablecerAsync(string correo, string codigo, string nuevaContrasena)
    {
        if (!PasswordPolicy.IsValid(nuevaContrasena))
            return (false, PasswordPolicy.ErrorMessage);

        // Se vuelve a validar el código aunque ya se haya verificado antes: así nadie puede cambiar
        // la contraseña llamando directo a la API sin el código.
        var (usuario, registro) = await ValidarCodigoAsync(correo, codigo);
        if (usuario is null || registro is null)
            return (false, MensajeCodigoInvalido);

        // El código fue correcto; se revisa hasta aquí para no revelar la contraseña actual a quien no tiene el código.
        if (_passwordService.VerifyPassword(usuario.contrasena, nuevaContrasena))
            return (false, "La contraseña nueva debe ser distinta de la actual.");

        usuario.contrasena = _passwordService.HashPassword(nuevaContrasena);
        registro.usado = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Contraseña cambiada con código de verificación: {Correo}", usuario.correo);
        return (true, "Contraseña actualizada. Ya puedes iniciar sesión con la nueva.");
    }

    /// <summary>
    /// Validación común de <see cref="VerificarCodigoAsync"/> y <see cref="RestablecerAsync"/>: la cuenta debe estar activa
    /// y el código debe ser el más reciente, no usado, vigente y coincidir con el hash guardado.
    /// Si no coincide suma un intento, y al llegar a <see cref="IntentosMaximos"/> invalida el código.
    /// Un código correcto NO se marca como usado aquí.
    /// </summary>
    /// <param name="correo">Correo de la cuenta.</param>
    /// <param name="codigo">Código de 6 dígitos escrito por el usuario.</param>
    /// <returns>La cuenta y el registro del código si es válido; <c>(null, null)</c> si no.</returns>
    private async Task<(Usuario? Usuario, RecuperacionContrasena? Registro)> ValidarCodigoAsync(string? correo, string? codigo)
    {
        var correoNormalizado = correo?.Trim().ToLowerInvariant();
        var codigoLimpio = codigo?.Trim();
        if (string.IsNullOrWhiteSpace(correoNormalizado) || string.IsNullOrWhiteSpace(codigoLimpio))
            return (null, null);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.correo == correoNormalizado);
        if (usuario is null || !usuario.edo)
            return (null, null);

        var registro = await _context.RecuperacionesContrasena
            .Where(r => r.idUsuario == usuario.idUsuario && !r.usado)
            .OrderByDescending(r => r.fechaCreacion)
            .FirstOrDefaultAsync();

        if (registro is null || registro.expira < DateTime.UtcNow)
            return (null, null);

        if (!_passwordService.VerifyPassword(registro.codigoHash, codigoLimpio))
        {
            registro.intentos++;
            if (registro.intentos >= IntentosMaximos)
                registro.usado = true;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Código de verificación incorrecto para {Correo} (intento {Intento} de {Maximo})",
                correoNormalizado, registro.intentos, IntentosMaximos);
            return (null, null);
        }

        return (usuario, registro);
    }
}
