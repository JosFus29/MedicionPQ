using MedicionPQ.Data;
using MedicionPQ.Modelos.Medidores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using MedidorComs = ComsPQLibrary.Modelos.MedidorPQ;
using PuntoIODtoComs = ComsPQLibrary.Modelos.PuntoIODto;

namespace MedicionPQ.Services.Medidores;

/// <summary>Busca los datos del medidor y los entrega al cliente serial de ComsPQLibrary.</summary>
public class ConexionMedidorService : IConexionMedidorService
{
    private readonly AppDbContext _db;
    private readonly ComsPQClient _comsClient;
    private readonly ILogger<ConexionMedidorService> _logger;
    private readonly PuntoSincronizarOptions _puntoSincronizar;

    /// <summary>Inicializa el servicio con la base de datos y el cliente serial compartido.</summary>
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

    /// <summary>Conecta con el medidor indicado por idMedidor, usando sus direcciones de la BD.</summary>
    public async Task<bool> ConectarAsync(ConexionMedidorRequest request)
    {
        var (idMedidor, medidorComs) = await ObtenerMedidorComsAsync(request.IdMedidor);

        var cronometro = Stopwatch.StartNew();
        _logger.LogInformation(
            "Iniciando conexión: IdMedidor={IdMedidor}, dirRed={DirRed}, UsarRF={UsarRF}",
            idMedidor,
            medidorComs.DirRed,
            _comsClient.UsarRF);

        var conectado = await _comsClient.ConectarAsync(medidorComs);
        cronometro.Stop();

        _logger.LogInformation(
            "Intento terminado: IdMedidor={IdMedidor}, conectado={Conectado}, duración={DuracionMs} ms",
            idMedidor,
            conectado,
            cronometro.ElapsedMilliseconds);

        return conectado;
    }

    /// <summary>
    /// Conecta y luego sincroniza el medidor mediante dos métodos distintos de la DLL.
    /// El punto de sincronización es configuración del servidor; el frontend solo manda idMedidor.
    /// </summary>
    public async Task<SincronizacionMedidorResult> SincronizarAsync(ConexionMedidorRequest request)
    {
        var (idMedidor, medidorComs) = await ObtenerMedidorComsAsync(request.IdMedidor);

        if (_puntoSincronizar.DirComsCEC is null)
        {
            throw new InvalidOperationException(
                "No se configuró Sincronizacion:Punto:DirComsCEC. Define la dirección SimIV confirmada antes de solicitar sincronización.");
        }

        if (_puntoSincronizar.DirComsCEC is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                "Sincronizacion:Punto:DirComsCEC debe estar entre 0 y 65535.");
        }

        if (string.IsNullOrWhiteSpace(_puntoSincronizar.Tag))
        {
            throw new InvalidOperationException(
                "Sincronizacion:Punto:Tag no puede estar vacío.");
        }

        // En esta DLL el punto SimIV usa DirComsCEC. DirModbus y Valor no intervienen aquí.
        var puntoDll = new PuntoIODtoComs
        {
            Tag = _puntoSincronizar.Tag,
            DirComsCEC = (ushort)_puntoSincronizar.DirComsCEC.Value
        };

        _logger.LogInformation(
            "Iniciando operación de sincronización: IdMedidor={IdMedidor}, DirRed={DirRed}, PuntoTag={PuntoTag}, DirComsCEC={DirComsCEC}",
            idMedidor,
            medidorComs.DirRed,
            puntoDll.Tag,
            puntoDll.DirComsCEC);

        var cronometro = Stopwatch.StartNew();
        var resultado = await _comsClient.ConectarYSincronizarAsync(medidorComs, puntoDll);
        cronometro.Stop();

        _logger.LogInformation(
            "Operación de sincronización terminada: IdMedidor={IdMedidor}, Conectado={Conectado}, Sincronizado={Sincronizado}, DuracionMs={DuracionMs}",
            idMedidor,
            resultado.Conectado,
            resultado.Sincronizado,
            cronometro.ElapsedMilliseconds);

        return resultado;
    }

    /// <summary>
    /// Busca una vez los datos del medidor y los adapta al DTO oficial de la DLL.
    /// Este mapeo común evita mantener dos versiones distintas para conectar y sincronizar.
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
                $"El medidor {datos.idMedidor} tiene una dirRed fuera del rango permitido por la DLL.");
        }

        _logger.LogInformation(
            "Medidor de BD seleccionado: IdMedidor={IdMedidor}, dirRed={DirRed}, dirRF='{DirRF}', UsarRF={UsarRF}",
            datos.idMedidor,
            datos.dirRed,
            datos.dirRF,
            _comsClient.UsarRF);

        // La API solo adapta los campos del registro; la DLL decide si usa DirRF según UsarRF.
        var medidorComs = new MedidorComs
        {
            DirRed = (ushort)datos.dirRed,
            DirRF = datos.dirRF
        };

        return (datos.idMedidor, medidorComs);
    }
}
