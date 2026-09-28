namespace MedicionPQ.Modelos;

/// <summary>
/// DTO devuelto por el endpoint de login. Contiene token y datos públicos del usuario.
/// </summary>
public class LoginResponse
{
    /// <summary>Mensaje legible que describe el resultado del inicio de sesión.</summary>
    public string Mensaje { get; set; } = string.Empty;
    /// <summary>JWT que el cliente envía como token Bearer en las rutas protegidas.</summary>
    public string Token { get; set; } = string.Empty;
    /// <summary>Identificador de la cuenta autenticada.</summary>
    public int IdUsuario { get; set; }
}
