using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>Consultas de solo lectura a las vistas Medicion5M.</summary>
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
}
