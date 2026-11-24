namespace MedicionPQ.Services;

/// <summary>
/// Define el cambio de contraseña con código de verificación, para cualquier rol:
/// primero se pide un código por correo, después se puede verificar el código por separado y al final
/// se usa ese mismo código para guardar la contraseña nueva.
/// </summary>
public interface IRecuperacionContrasenaService
{
    /// <summary>
    /// Genera un código de 6 dígitos y lo envía al correo indicado, si pertenece a una cuenta activa.
    /// No informa si el correo existe, para no revelar qué cuentas hay registradas.
    /// </summary>
    /// <param name="correo">Correo con el que el usuario inicia sesión.</param>
    Task SolicitarCodigoAsync(string correo);

    /// <summary>
    /// Comprueba si el código recibido por correo es correcto, sin cambiar la contraseña ni marcar el código como usado
    /// (después se usa el mismo código en <see cref="RestablecerAsync"/>). Un código incorrecto suma un intento
    /// igual que en <see cref="RestablecerAsync"/>, y al quinto el código se invalida.
    /// </summary>
    /// <param name="correo">Correo con el que el usuario inicia sesión.</param>
    /// <param name="codigo">Código de 6 dígitos recibido por correo.</param>
    /// <returns>Resultado y mensaje ("Código válido." o el mensaje genérico de código inválido).</returns>
    Task<(bool Exito, string Mensaje)> VerificarCodigoAsync(string correo, string codigo);

    /// <summary>
    /// Valida el código recibido por correo y, si es correcto, guarda la contraseña nueva y marca el código como usado.
    /// Vuelve a validar el código aunque ya se haya verificado antes con <see cref="VerificarCodigoAsync"/>.
    /// </summary>
    /// <param name="correo">Correo con el que el usuario inicia sesión.</param>
    /// <param name="codigo">Código de 6 dígitos recibido por correo.</param>
    /// <param name="nuevaContrasena">Contraseña nueva; debe cumplir la política y ser distinta de la actual.</param>
    /// <returns>Resultado de la operación y mensaje.</returns>
    Task<(bool Exito, string Mensaje)> RestablecerAsync(string correo, string codigo, string nuevaContrasena);
}
