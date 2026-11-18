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
/// Configuración esperada (la contraseña siempre en User Secrets, nunca en appsettings.json):
/// <c>Email:Host</c>, <c>Email:Puerto</c> (587 por defecto), <c>Email:Usuario</c>, <c>Email:Contrasena</c>,
/// <c>Email:Remitente</c> (por defecto el mismo usuario) y <c>Email:UsarSsl</c> (true por defecto).
/// </summary>
public class EmailService : IEmailService
{
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

        var usuario = _config["Email:Usuario"];
        var remitente = _config["Email:Remitente"] ?? usuario;
        if (string.IsNullOrWhiteSpace(remitente))
        {
            _logger.LogError("Falta Email:Remitente o Email:Usuario; no se envió el mensaje para {Destinatario}", destinatario);
            return;
        }

        var puerto = int.TryParse(_config["Email:Puerto"], out var p) ? p : 587;
        var usarSsl = !string.Equals(_config["Email:UsarSsl"], "false", StringComparison.OrdinalIgnoreCase);

        using var mensaje = new MailMessage(remitente, destinatario, asunto, cuerpo);
        using var cliente = new SmtpClient(host, puerto)
        {
            EnableSsl = usarSsl,
            Credentials = new NetworkCredential(usuario, _config["Email:Contrasena"])
        };

        await cliente.SendMailAsync(mensaje);
    }
}
