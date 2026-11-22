using System.Net;
using System.Net.Mail;

namespace MedicionPQ.Services;

/// <summary>
/// Envía correos por SMTP usando la sección <c>Email</c> de la configuración.
/// Mientras no haya un servidor de correo configurado:
/// <list type="bullet">
/// <item>En desarrollo (modo prueba) escribe el correo solo en la consola, para poder probar sin enviar nada.
/// Se usa <c>Console.WriteLine</c> a propósito, para que el contenido (por ejemplo un código) no quede en el archivo de log.</item>
/// <item>En cualquier otro entorno no envía nada y registra un error.</item>
/// </list>
/// Configuración esperada (la contraseña siempre en User Secrets o variables de entorno, nunca en appsettings.json):
/// <c>Email:Host</c>, <c>Email:Puerto</c> (587 por defecto), <c>Email:Usuario</c>, <c>Email:Contrasena</c>,
/// <c>Email:Remitente</c> (por defecto el mismo usuario), <c>Email:NombreRemitente</c> (por defecto "MedicionPQ")
/// y <c>Email:UsarSsl</c> (true por defecto).
/// Con Gmail: <c>Email:Host = smtp.gmail.com</c>, puerto 587 y una contraseña de aplicación
/// (https://myaccount.google.com/apppasswords), no la contraseña normal de la cuenta.
/// </summary>
public class EmailService : IEmailService
{
    /// <summary>Milisegundos máximos de espera al servidor de correo, para no dejar colgada la petición.</summary>
    public const int TimeoutMilisegundos = 15_000;

    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EmailService> _logger;

    /// <summary>Recibe la configuración, el entorno de ejecución y el logger.</summary>
    public EmailService(IConfiguration config, IWebHostEnvironment environment, ILogger<EmailService> logger)
    {
        _config = config;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Quita los espacios de la contraseña. Google muestra las contraseñas de aplicación en grupos
    /// ("abcd efgh ijkl mnop"); si se guardan con espacios, Gmail rechaza la autenticación.
    /// </summary>
    /// <param name="contrasena">Contraseña tal como está en la configuración.</param>
    /// <returns>La contraseña sin espacios, o cadena vacía si no hay.</returns>
    public static string LimpiarContrasena(string? contrasena) =>
        string.Concat((contrasena ?? string.Empty).Where(c => !char.IsWhiteSpace(c)));

    /// <inheritdoc/>
    public async Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        var host = _config["Email:Host"];

        if (string.IsNullOrWhiteSpace(host))
        {
            if (_environment.IsDevelopment())
            {
                // MODO PRUEBA: el correo se muestra solo en la consola y no llega a ningún buzón.
                Console.WriteLine();
                Console.WriteLine("===== CORREO (MODO PRUEBA, NO SE ENVIÓ) =====");
                Console.WriteLine($"Para:   {destinatario}");
                Console.WriteLine($"Asunto: {asunto}");
                Console.WriteLine(cuerpo);
                Console.WriteLine("=============================================");
                _logger.LogWarning("Modo prueba: no hay correo configurado; el mensaje para {Destinatario} se mostró solo en la consola", destinatario);
                return;
            }

            _logger.LogError("No hay servidor de correo configurado (Email:Host); no se envió el mensaje para {Destinatario}", destinatario);
            return;
        }

        var usuario = _config["Email:Usuario"]?.Trim();
        var contrasena = LimpiarContrasena(_config["Email:Contrasena"]);
        var remitente = _config["Email:Remitente"]?.Trim();
        if (string.IsNullOrWhiteSpace(remitente)) remitente = usuario;

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrWhiteSpace(remitente))
        {
            _logger.LogError("Configuración de correo incompleta: falta Email:Usuario o Email:Contrasena; no se envió el mensaje para {Destinatario}", destinatario);
            throw new InvalidOperationException("La configuración de correo está incompleta (Email:Usuario o Email:Contrasena).");
        }

        var puerto = int.TryParse(_config["Email:Puerto"], out var p) ? p : 587;
        var usarSsl = !string.Equals(_config["Email:UsarSsl"], "false", StringComparison.OrdinalIgnoreCase);
        var nombreRemitente = _config["Email:NombreRemitente"];
        if (string.IsNullOrWhiteSpace(nombreRemitente)) nombreRemitente = "MedicionPQ";

        using var mensaje = new MailMessage(new MailAddress(remitente, nombreRemitente), new MailAddress(destinatario))
        {
            Subject = asunto,
            Body = cuerpo,
            IsBodyHtml = false
        };
        using var cliente = new SmtpClient(host, puerto)
        {
            EnableSsl = usarSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(usuario, contrasena),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = TimeoutMilisegundos
        };

        try
        {
            await cliente.SendMailAsync(mensaje);
            _logger.LogInformation("Correo enviado a {Destinatario} por {Host}:{Puerto} desde {Remitente}", destinatario, host, puerto, remitente);
        }
        catch (SmtpException ex)
        {
            // Gmail suele cerrar la conexión sin explicar el motivo cuando el usuario o la contraseña no son válidos.
            _logger.LogError(ex,
                "El servidor de correo {Host}:{Puerto} rechazó el envío para {Destinatario} (código {Codigo}). " +
                "Revisa Email:Usuario y Email:Contrasena: con Gmail debe ser una contraseña de aplicación de 16 letras " +
                "de la misma cuenta que Email:Usuario, con la verificación en dos pasos activa.",
                host, puerto, destinatario, ex.StatusCode);
            throw;
        }
    }
}
