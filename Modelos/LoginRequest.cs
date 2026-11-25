using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>
/// Datos de <c>POST /api/Auth/login</c>. Se entra con el correo o con el nombre de usuario, más la contraseña.
/// </summary>
/// <remarks>
/// El campo recomendado es <see cref="Usuario"/>, que acepta cualquiera de los dos. <see cref="Correo"/> se conserva
/// para que los clientes que ya mandan <c>{ "correo", "contrasena" }</c> sigan funcionando; también acepta un nombre de usuario.
/// Si llegan los dos, se usa <see cref="Usuario"/>.
/// </remarks>
public class LoginRequest : IValidatableObject
{
    /// <summary>Correo o nombre de usuario de la cuenta.</summary>
    [StringLength(100)]
    public string? Usuario { get; set; }

    /// <summary>Correo de la cuenta (forma anterior); también acepta un nombre de usuario.</summary>
    [StringLength(100)]
    public string? Correo { get; set; }

    /// <summary>Contraseña de la cuenta.</summary>
    [Required]
    public string Contrasena { get; set; } = string.Empty;

    /// <summary>Lo que se escribió para identificar la cuenta: <see cref="Usuario"/> o, si viene vacío, <see cref="Correo"/>.</summary>
    /// <returns>Correo o nombre de usuario, sin espacios al inicio ni al final.</returns>
    public string ObtenerCuenta() =>
        (string.IsNullOrWhiteSpace(Usuario) ? Correo : Usuario)?.Trim() ?? string.Empty;

    /// <summary>Exige que venga el correo o el nombre de usuario.</summary>
    /// <param name="validationContext">Contexto de validación de ASP.NET Core.</param>
    /// <returns>Error si no se indicó ninguno de los dos.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Usuario) && string.IsNullOrWhiteSpace(Correo))
            yield return new ValidationResult("Escribe tu correo o tu nombre de usuario.", new[] { nameof(Usuario) });
    }
}
