namespace MedicionPQ.Services;

/// <summary>
/// Define la recuperación de contraseña para usuarios que la olvidaron:
/// primero se pide un código por correo y luego se usa ese código para crear una contraseña nueva.
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
    /// <param name="nuevaContrasena">Contraseña nueva en texto plano, que se convertirá en hash.</param>
    /// <returns>Resultado de la operación y mensaje.</returns>
    Task<(bool Exito, string Mensaje)> RestablecerAsync(string correo, string codigo, string nuevaContrasena);
}
