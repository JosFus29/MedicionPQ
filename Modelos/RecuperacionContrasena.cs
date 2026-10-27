using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicionPQ.Modelos;

/// <summary>
/// Código de recuperación de contraseña enviado por correo (tabla RecuperacionContrasena).
/// El código nunca se guarda en texto plano, solo su hash; vence y solo se puede usar una vez.
/// La tabla se crea con el script <c>Scripts/RecuperacionContrasena.sql</c>.
/// </summary>
[Table("RecuperacionContrasena")]
public class RecuperacionContrasena
{
    /// <summary>Identificador único del código.</summary>
    [Key]
    public int idRecuperacion { get; set; }

    /// <summary>Usuario que pidió recuperar su contraseña.</summary>
    public int idUsuario { get; set; }

    /// <summary>Hash del código de 6 dígitos (nunca el código original).</summary>
    public string codigoHash { get; set; } = string.Empty;

    /// <summary>Fecha y hora (UTC) en que el código deja de ser válido.</summary>
    public DateTime expira { get; set; }

    /// <summary>Cantidad de veces que se escribió un código incorrecto.</summary>
    public int intentos { get; set; }

    /// <summary>Indica si el código ya se usó o se invalidó.</summary>
    public bool usado { get; set; }

    /// <summary>Fecha y hora (UTC) en que se generó el código.</summary>
    public DateTime fechaCreacion { get; set; }
}
