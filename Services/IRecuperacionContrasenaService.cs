namespace MedicionPQ.Services;

/// <summary>
/// Define el cambio de contraseña con código de verificación, para cualquier rol:
/// primero se pide un código por correo y luego se usa ese código para guardar la contraseña nueva.
/// </summary>
public interface IRecuperacionContrasenaService
{
    /// <summary>
    /// Genera un código de 6 dígitos y lo envía al correo indicado, si pertenece a una cuenta activa.
    /// No informa si el correo existe, para no revelar qué cuentas hay registradas.
    /// </summary>
    /// <param name="correo">Correo con el que el usuario inicia sesión.</param>
    Task SolicitarCodigoAsync(string correo);

    /// <summary>Valida el código recibido por correo y, si es correcto, guarda la contraseña nueva.</summary>
    /// <param name="correo">Correo con el que el usuario inicia sesión.</param>
    /// <param name="codigo">Código de 6 dígitos recibido por correo.</param>
    /// <param name="nuevaContrasena">Contraseña nueva; debe cumplir la política y ser distinta de la actual.</param>
    /// <returns>Resultado de la operación y mensaje.</returns>
    Task<(bool Exito, string Mensaje)> RestablecerAsync(string correo, string codigo, string nuevaContrasena);
}
