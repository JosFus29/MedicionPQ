using System.Text.RegularExpressions;

namespace MedicionPQ.Modelos;

/// <summary>
/// Reglas del nombre de usuario (<c>nombreUsuario</c>), que sirve como nombre visible y también para iniciar sesión
/// en lugar del correo.
/// <list type="bullet">
/// <item>Es único sin importar mayúsculas ni espacios de más: "Ana López", "ana  lópez" y " ANA LÓPEZ " son el mismo.</item>
/// <item>Puede llevar espacios; se guardan sin espacios al inicio o al final y con un solo espacio entre palabras.</item>
/// <item>Tiene de 3 a 50 caracteres (la columna de la base es de 50) y no puede llevar "@", para que el login
/// distinga un nombre de usuario de un correo.</item>
/// </list>
/// </summary>
public static partial class NombreUsuarioPolicy
{
    /// <summary>Largo mínimo del nombre de usuario.</summary>
    public const int Minimo = 3;

    /// <summary>Largo máximo del nombre de usuario (tamaño de la columna nombreUsuario).</summary>
    public const int Maximo = 50;

    /// <summary>
    /// Mensaje al registrar un nombre que ya existe. Empieza igual que el del correo repetido para que
    /// <c>POST /api/Auth/register</c> responda 409 Conflict en ambos casos.
    /// </summary>
    public const string MensajeDuplicadoRegistro = "Ya existe un usuario con ese nombre de usuario.";

    /// <summary>Mensaje al editar una cuenta con el nombre de otra.</summary>
    public const string MensajeDuplicadoEdicion = "El nombre de usuario ya está en uso por otro usuario.";

    /// <summary>Quita los espacios al inicio y al final y deja un solo espacio entre palabras.</summary>
    /// <param name="nombre">Nombre tal como lo escribió el usuario.</param>
    /// <returns>El nombre listo para guardarse; cadena vacía si viene nulo.</returns>
    public static string Normalizar(string? nombre) => EspaciosSeguidos().Replace((nombre ?? string.Empty).Trim(), " ");

    /// <summary>Forma de comparar nombres: normalizado y en minúsculas.</summary>
    /// <param name="nombre">Nombre a comparar.</param>
    /// <returns>Clave para buscar o detectar repetidos.</returns>
    public static string Clave(string? nombre) => Normalizar(nombre).ToLowerInvariant();

    /// <summary>Revisa largo y caracteres de un nombre ya normalizado.</summary>
    /// <param name="nombreNormalizado">Resultado de <see cref="Normalizar"/>.</param>
    /// <returns>Mensaje de error, o null si el nombre es válido.</returns>
    public static string? Validar(string nombreNormalizado)
    {
        if (string.IsNullOrEmpty(nombreNormalizado))
            return "El nombre de usuario es obligatorio.";
        if (nombreNormalizado.Length is < Minimo or > Maximo)
            return $"El nombre de usuario debe tener entre {Minimo} y {Maximo} caracteres.";
        if (nombreNormalizado.Contains('@'))
            return "El nombre de usuario no puede llevar @ (así se distingue del correo al iniciar sesión).";
        return null;
    }

    /// <summary>Uno o más espacios, tabuladores o saltos de línea seguidos.</summary>
    [GeneratedRegex(@"\s+")]
    private static partial Regex EspaciosSeguidos();
}
