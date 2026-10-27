namespace MedicionPQ.Services;

/// <summary>Define el envío de correos electrónicos desde la API.</summary>
public interface IEmailService
{
    /// <summary>Envía un correo de texto plano.</summary>
    /// <param name="destinatario">Dirección de correo de quien lo recibe.</param>
    /// <param name="asunto">Asunto del correo.</param>
    /// <param name="cuerpo">Contenido del correo en texto plano.</param>
    Task EnviarAsync(string destinatario, string asunto, string cuerpo);
}
