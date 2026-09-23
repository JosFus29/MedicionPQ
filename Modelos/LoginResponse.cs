namespace MedicionPQ.Modelos;

/// <summary>
/// DTO devuelto por el endpoint de login. Contiene token y datos públicos del usuario.
/// </summary>
public class LoginResponse
{
    public string Mensaje { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public int IdUsuario { get; set; }
}
