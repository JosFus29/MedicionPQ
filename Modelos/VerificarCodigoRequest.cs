using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// DTO para comprobar el código de verificación antes de pedir la contraseña nueva
/// (endpoint <c>POST /api/Contrasena/verificar-codigo</c>). Aplica a cualquier rol.
/// </summary>
/// <remarks>
/// El formato del código (6 dígitos) no se valida aquí a propósito: cualquier código incorrecto, aunque tenga
/// letras o menos dígitos, recibe el mismo 400 con el mensaje genérico "Código inválido o vencido.".
/// </remarks>
public class VerificarCodigoRequest
{
    /// <summary>Correo con el que el usuario inicia sesión.</summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    /// <summary>Código de verificación de 6 dígitos recibido por correo.</summary>
    [Required]
    public string Codigo { get; set; } = string.Empty;
}
