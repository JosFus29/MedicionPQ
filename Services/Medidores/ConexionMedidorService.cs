using MedicionPQ.Data;
using MedicionPQ.Modelos.Medidores;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using MedidorComs = ComsPQLibrary.Modelos.MedidorPQ;

namespace MedicionPQ.Services.Medidores;

/// <summary>Busca los datos del medidor y los entrega al cliente serial de ComsPQLibrary.</summary>
public class ConexionMedidorService : IConexionMedidorService
{
    private readonly AppDbContext _db;
    private readonly ComsPQClient _comsClient;
    private readonly ILogger<ConexionMedidorService> _logger;

    /// <summary>Inicializa el servicio con la base de datos y el cliente serial compartido.</summary>
    public ConexionMedidorService(
        AppDbContext db,
        ComsPQClient comsClient,
        ILogger<ConexionMedidorService> logger)
    {
        _db = db;
        _comsClient = comsClient;
        _logger = logger;
    }

    /// <summary>Conecta con el medidor indicado por idMedidor, usando sus direcciones de la BD.</summary>
    public async Task<bool> ConectarAsync(ConexionMedidorRequest request)
    {
        var datosMedidor = await _db.MedidoresQP
            .AsNoTracking()
            .Where(medidor => medidor.idMedidor == request.IdMedidor)
            .Select(medidor => new
            {
                medidor.idMedidor,
                medidor.dirRed,
                medidor.dirRF
            })
            .SingleOrDefaultAsync();

        if (datosMedidor is null)
            throw new KeyNotFoundException($"No se encontró un medidor con idMedidor {request.IdMedidor}.");

        if (datosMedidor.dirRed is < 0 or > ushort.MaxValue)
        {
            throw new InvalidOperationException(
                $"El medidor {datosMedidor.idMedidor} tiene una dirRed fuera del rango permitido por la DLL.");
        }

        _logger.LogInformation(
            "Medidor de BD seleccionado: IdMedidor={IdMedidor}, dirRed={DirRed}, dirRF='{DirRF}', UsarRF={UsarRF}",
            datosMedidor.idMedidor,
            datosMedidor.dirRed,
            datosMedidor.dirRF,
            _comsClient.UsarRF);

        // La API adapta los datos al DTO; la DLL decide si utiliza DirRF según UsarRF.
        var medidorComs = new MedidorComs
        {
            DirRed = (ushort)datosMedidor.dirRed,
            DirRF = datosMedidor.dirRF
        };

        var cronometro = Stopwatch.StartNew();
        _logger.LogInformation(
            "Iniciando conexión: IdMedidor={IdMedidor}, dirRed={DirRed}, UsarRF={UsarRF}",
            datosMedidor.idMedidor,
            medidorComs.DirRed,
            _comsClient.UsarRF);

        var conectado = await _comsClient.ConectarAsync(medidorComs);
        cronometro.Stop();

        _logger.LogInformation(
            "Intento terminado: IdMedidor={IdMedidor}, conectado={Conectado}, duración={DuracionMs} ms",
            datosMedidor.idMedidor,
            conectado,
            cronometro.ElapsedMilliseconds);

        return conectado;
    }
}
