using System.ComponentModel.DataAnnotations;

namespace MedicionPQ.Modelos;

/// <summary>Valor nuevo y contraseña actual para autorizar la edición de una lectura.</summary>
public class Medicion5MUpdateRequest
{
    /// <summary>Nuevo valor medido.</summary>
    public double Valor { get; set; }

    /// <summary>Contraseña actual del administrador autenticado, usada solo para revalidar la edición.</summary>
    [Required]
    public string ContrasenaActual { get; set; } = string.Empty;
}
