using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>DTO para pedir un código de recuperación de contraseña.</summary>
public class SolicitarRecuperacionRequest
{
    /// <summary>Correo con el que el usuario inicia sesión.</summary>
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;
}
