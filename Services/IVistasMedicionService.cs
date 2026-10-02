using MedicionPQ.Modelos;

namespace MedicionPQ.Services;

/// <summary>
/// Interfaz para consultar las vistas de mediciones en sus distintos niveles
/// de agregación (cruda, hora, día, semana, mes, año).
/// </summary>
public interface IVistasMedicionService
{
    Task<List<Medicion5M>> GetCrudoAsync();
    Task<List<Medicion5MPromedioHora>> GetPromedioHoraAsync();
    Task<List<Medicion5MPromedioDia>> GetPromedioDiaAsync();
    Task<List<Medicion5MPromedioSemana>> GetPromedioSemanaAsync();
    Task<List<Medicion5MPromedioMes>> GetPromedioMesAsync();
    Task<List<Medicion5MPromedioAnio>> GetPromedioAnioAsync();
}