using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Consultas de las vistas Medicion5M y edición protegida de una lectura.</summary>
public interface IMedicion5MService
{
    /// <summary>Consulta lecturas crudas de cinco minutos.</summary>
    Task<List<Medicion5MView>> GetCrudasAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase);
    /// <summary>Consulta promedios horarios.</summary>
    Task<List<Medicion5MPromedioHoraView>> GetPromedioHoraAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase);
    /// <summary>Consulta promedios diarios.</summary>
    Task<List<Medicion5MPromedioDiaView>> GetPromedioDiaAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase);
    /// <summary>Consulta los promedios diarios de semanas incluidas en el rango.</summary>
    Task<List<Medicion5MPromedioSemanaView>> GetPromedioSemanaAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase);
    /// <summary>Consulta los promedios diarios de meses incluidos en el rango.</summary>
    Task<List<Medicion5MPromedioMesView>> GetPromedioMesAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase);
    /// <summary>Consulta promedios mensuales de los meses que intersectan el rango.</summary>
    Task<List<Medicion5MPromedioAnioView>> GetPromedioAnioAsync(int idMedidor, int idVar5M, DateOnly inicio, DateOnly fin, short fase);
    /// <summary>Verifica la contraseña del administrador y actualiza una lectura por su clave completa.</summary>
    Task<Medicion5MUpdateResult> UpdateAsync(int idMedidor, int idVar5M, DateOnly fecha, short fase, short intervalo, int idUsuario, Medicion5MUpdateRequest request);
}

/// <summary>Resultado discriminado de la edición protegida de una medición.</summary>
public sealed record Medicion5MUpdateResult(Medicion5M? Medicion, Medicion5MUpdateError Error);

/// <summary>Motivos de rechazo para actualizar una medición.</summary>
public enum Medicion5MUpdateError
{
    /// <summary>Actualización completada.</summary>
    None,
    /// <summary>No se encontró la combinación de clave indicada.</summary>
    NoEncontrada,
    /// <summary>Cuenta inactiva, no administradora o contraseña incorrecta.</summary>
    CredencialesInvalidas
}
