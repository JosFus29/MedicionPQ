using MedicionPQ.Data;
using MedicionPQ.Modelos.Medidores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using MedidorComs = ComsPQLibrary.Modelos.MedidorPQ;
using PuntoIODtoComs = ComsPQLibrary.Modelos.PuntoIODto;

namespace MedicionPQ.Services.Medidores;

/// <summary>Adapta registros de la base de datos a los DTO de ComsPQLibrary y coordina las operaciones del medidor.</summary>
public class ConexionMedidorService : IConexionMedidorService
{
    private readonly AppDbContext _db;
    private readonly ComsPQClient _comsClient;
    private readonly ILogger<ConexionMedidorService> _logger;
    private readonly PuntoSincronizarOptions _puntoSincronizar;

    /// <summary>Inicializa el servicio con la base de datos, el cliente DLL, el logger y la configuración del punto de sincronización.</summary>
    public ConexionMedidorService(
        AppDbContext db,
        ComsPQClient comsClient,
        ILogger<ConexionMedidorService> logger,
        IOptions<PuntoSincronizarOptions> puntoSincronizar)
    {
        _db = db;
        _comsClient = comsClient;
        _logger = logger;
        _puntoSincronizar = puntoSincronizar.Value;
    }

    /// <summary>Conecta con el medidor identificado por su ID en la base de datos. No lo sincroniza.</summary>
    public async Task<bool> ConectarAsync(ConexionMedidorRequest request)
    {
        var (idMedidor, medidorComs) = await ObtenerMedidorComsAsync(request.IdMedidor);

        var cronometro = Stopwatch.StartNew();
        _logger.LogInformation(
            "Iniciando solo conexión: IdMedidor={IdMedidor}, DirRed={DirRed}, UsarRF={UsarRF}",
            idMedidor,
            medidorComs.DirRed,
            _comsClient.UsarRF);

        var conectado = await _comsClient.ConectarAsync(medidorComs);
        cronometro.Stop();

        _logger.LogInformation(
            "Terminó la operación de solo conexión: IdMedidor={IdMedidor}, Conectado={Conectado}, DuracionMs={DuracionMs}",
            idMedidor,
            conectado,
            cronometro.ElapsedMilliseconds);

        return conectado;
    }

    /// <summary>
    /// Primero conecta y después llama al método de sincronización de la DLL con el punto seleccionado por IdPunto.
    /// Es una operación distinta de ConectarAsync, que únicamente establece la conexión.
    /// </summary>
    public async Task<SincronizacionMedidorResult> SincronizarAsync(ConexionMedidorRequest request)
    {
        var (idMedidor, medidorComs) = await ObtenerMedidorComsAsync(request.IdMedidor);

        // IdPunto selecciona el registro PuntosIO; la DLL recibe el Tag y la dirección CEC de ese registro.
        var puntoBd = await _db.PuntosIO
            .AsNoTracking()
            .Where(punto => punto.IdPunto == _puntoSincronizar.IdPunto)
            .Select(punto => new
            {
                punto.IdPunto,
                punto.Tag,
                punto.DirCCEC
            })
            .SingleOrDefaultAsync();

        if (puntoBd is null)
        {
            throw new InvalidOperationException(
                $"No existe un registro PuntosIO con el IdPunto configurado {_puntoSincronizar.IdPunto}.");
        }

        if (puntoBd.DirCCEC is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"El registro PuntosIO {puntoBd.IdPunto} tiene un DirCCEC fuera del rango que admite la DLL.");
        }

        // Para SimIV, ComsPQLibrary usa DirComsCEC. DirModbus y Valor no se utilizan aquí.
        var puntoDll = new PuntoIODtoComs
        {
            Tag = puntoBd.Tag,
            DirComsCEC = (ushort)puntoBd.DirCCEC
        };

        _logger.LogInformation(
            "Iniciando conexión y sincronización: IdMedidor={IdMedidor}, DirRed={DirRed}, IdPunto={IdPunto}, Tag={Tag}, DirCCEC={DirCCEC}, DirComsCEC={DirComsCEC}",
            idMedidor,
            medidorComs.DirRed,
            puntoBd.IdPunto,
            puntoDll.Tag,
            puntoBd.DirCCEC,
            puntoDll.DirComsCEC);

        var cronometro = Stopwatch.StartNew();
        var resultado = await _comsClient.ConectarYSincronizarAsync(medidorComs, puntoDll);
        cronometro.Stop();

        _logger.LogInformation(
            "Terminó la operación de conexión y sincronización: IdMedidor={IdMedidor}, Conectado={Conectado}, Sincronizado={Sincronizado}, DuracionMs={DuracionMs}",
            idMedidor,
            resultado.Conectado,
            resultado.Sincronizado,
            cronometro.ElapsedMilliseconds);

        return resultado;
    }

    /// <summary>
    /// Lee el medidor una vez y adapta sus direcciones al DTO de la DLL.
    /// La conexión y la sincronización reutilizan este mismo mapeo.
    /// </summary>
    private async Task<(int IdMedidor, MedidorComs Medidor)> ObtenerMedidorComsAsync(int idMedidor)
    {
        var datos = await _db.MedidoresQP
            .AsNoTracking()
            .Where(medidor => medidor.idMedidor == idMedidor)
            .Select(medidor => new
            {
                medidor.idMedidor,
                medidor.dirRed,
                medidor.dirRF
            })
            .SingleOrDefaultAsync();

        if (datos is null)
            throw new KeyNotFoundException($"No se encontró un medidor con idMedidor {idMedidor}.");

        if (datos.dirRed is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"El medidor {datos.idMedidor} tiene una dirRed fuera del rango que admite la DLL.");
        }

        _logger.LogInformation(
            "Medidor seleccionado de la base de datos: IdMedidor={IdMedidor}, DirRed={DirRed}, DirRF='{DirRF}', UsarRF={UsarRF}",
            datos.idMedidor,
            datos.dirRed,
            datos.dirRF,
            _comsClient.UsarRF);

        // Se pasan los valores de la base de datos; la DLL decide si utiliza DirRF.
        var medidorComs = new MedidorComs
        {
            DirRed = (ushort)datos.dirRed,
            DirRF = datos.dirRF
        };

        return (datos.idMedidor, medidorComs);
    }
}
