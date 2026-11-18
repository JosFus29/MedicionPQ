namespace MedicionPQ.Modelos.Medidores;

/// <summary>
/// Grupos de PuntosIO que se leen del medidor con una lista de puntos.
/// El valor de cada elemento es el idGF de la tabla IO_GrupoFuncional.
/// </summary>
public enum GrupoLectura
{
    /// <summary>Valores instantáneos (IO_GrupoFuncional "Instantaneos"); la DLL los lee con SolInstT.</summary>
    Instantaneos = 1,

    /// <summary>Variables 5 minutales (IO_GrupoFuncional "UV5M"); la DLL las lee con SolUV5M.</summary>
    Variables5Min = 2,

    /// <summary>Consumos (IO_GrupoFuncional "Consumos"); la DLL los lee con SolConsumos.</summary>
    Consumos = 3,

    /// <summary>Calidad de la potencia (IO_GrupoFuncional "CalDePot"); la DLL la lee con SolCalPot.</summary>
    CalidadPotencia = 5
}

/// <summary>Textos de cada grupo para mensajes y logs.</summary>
public static class GrupoLecturaExtensions
{
    /// <summary>Nombre legible del grupo, en minúsculas, para armar mensajes.</summary>
    public static string Descripcion(this GrupoLectura grupo) => grupo switch
    {
        GrupoLectura.Instantaneos => "instantáneos",
        GrupoLectura.Variables5Min => "variables 5 minutales",
        GrupoLectura.Consumos => "consumos",
        GrupoLectura.CalidadPotencia => "calidad de la potencia",
        _ => grupo.ToString()
    };
}
