namespace MedicionPQ.Services.Medidores;

/// <summary>
/// Se lanza cuando otra solicitud mantiene ocupado el puerto del medidor más tiempo del permitido.
/// Los controladores la convierten en 503 para que el frontend pueda reintentar.
/// </summary>
public sealed class PuertoOcupadoException : Exception
{
    /// <summary>Crea la excepción indicando cuánto se esperó el turno.</summary>
    public PuertoOcupadoException(TimeSpan espera)
        : base($"El puerto del medidor está ocupado por otra solicitud; se esperó {espera.TotalSeconds:0} s. Intenta de nuevo.")
    {
    }
}
