using System.Text.RegularExpressions;

namespace MedicionPQ.Modelos;

/// <summary>Reglas comunes para contraseñas nuevas; no se aplican al inicio de sesión.</summary>
public static class PasswordPolicy
{
    /// <summary>De 8 a 16 caracteres, mayúscula, minúscula, número, símbolo y ningún espacio.</summary>
    public const string Pattern = @"^(?=.*\p{Lu})(?=.*\p{Ll})(?=.*[0-9])(?=.*[^\p{L}\p{N}\s])\S{8,16}$";

    /// <summary>Mensaje compartido para validaciones de contraseñas nuevas.</summary>
    public const string ErrorMessage = "La contraseña debe tener de 8 a 16 caracteres, una mayúscula, una minúscula, un número y un carácter especial, sin espacios.";

    /// <summary>Valida una contraseña nueva con las reglas compartidas del proyecto.</summary>
    public static bool IsValid(string? password) =>
        password is not null && Regex.IsMatch(password, Pattern);
}
