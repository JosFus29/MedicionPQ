using MedicionPQ.Data;
using MedicionPQ.Modelos.Medidores;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
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

        string? dirRfParaDll = null;
        if (_comsClient.UsarRF)
        {
            if (string.IsNullOrWhiteSpace(datosMedidor.dirRF))
            {
                throw new InvalidOperationException(
                    $"El medidor {datosMedidor.idMedidor} no tiene dirección MAC (dirRF) configurada en la BD.");
            }

            dirRfParaDll = NormalizarDirRf(datosMedidor.dirRF);
        }

        _logger.LogInformation(
            "Medidor de BD seleccionado: IdMedidor={IdMedidor}, dirRed={DirRed}, UsarRF={UsarRF}, dirRF BD='{DirRFBd}', dirRF DLL='{DirRfDll}'",
            datosMedidor.idMedidor,
            datosMedidor.dirRed,
            _comsClient.UsarRF,
            _comsClient.UsarRF ? datosMedidor.dirRF : null,
            dirRfParaDll);

        var medidorComs = new MedidorComs
        {
            DirRed = (ushort)datosMedidor.dirRed,
            DirRF = dirRfParaDll
        };

        var cronometro = Stopwatch.StartNew();
        _logger.LogInformation(
            "Iniciando conexión: IdMedidor={IdMedidor}, dirRed={DirRed}, UsarRF={UsarRF}",
            datosMedidor.idMedidor,
            medidorComs.DirRed,
            _comsClient.UsarRF);

        try
        {
            var conectado = await _comsClient.ConectarAsync(medidorComs);
            cronometro.Stop();

            _logger.LogInformation(
                "Intento terminado: IdMedidor={IdMedidor}, conectado={Conectado}, duración={DuracionMs} ms",
                datosMedidor.idMedidor,
                conectado,
                cronometro.ElapsedMilliseconds);

            return conectado;
        }
        catch (Exception ex)
        {
            cronometro.Stop();
            _logger.LogError(
                ex,
                "Excepción al conectar IdMedidor={IdMedidor}, dirRed={DirRed}, UsarRF={UsarRF}, duración={DuracionMs} ms",
                datosMedidor.idMedidor,
                medidorComs.DirRed,
                _comsClient.UsarRF,
                cronometro.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>Normaliza bytes hexadecimales de dirRF a minúsculas y separados por ':' para la DLL.</summary>
    private static string NormalizarDirRf(string dirRF)
    {
        var segmentos = dirRF
            .Trim()
            .Replace('.', ':')
            .Split(':', StringSplitOptions.None)
            .Select(segmento => segmento.Trim())
            .ToArray();

        if (segmentos.Length == 0
            || segmentos.Any(segmento =>
                segmento.Length != 2
                || !byte.TryParse(
                    segmento,
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out _)))
        {
            throw new InvalidOperationException(
                $"La dirRF '{dirRF}' no tiene formato hexadecimal válido. Usa bytes de dos dígitos separados por ':' (por ejemplo, 62:69:c0), sin agregar el prefijo 30.");
        }

        return string.Join(":", segmentos.Select(segmento => segmento.ToLowerInvariant()));
    }
}
