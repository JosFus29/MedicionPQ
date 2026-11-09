using MedicionPQ.Data;
using MedicionPQ.Modelos.Medidores;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using ComsProtocoloConfig = ComsPQLibrary.Modelos.ProtocoloConfig;
using ParametroComs = ComsPQLibrary.Modelos.ParamT;
using ServicioComs = ComsPQLibrary.ComsPQServicio.ComsPQService;

namespace MedicionPQ.Services.Medidores;

/// <summary>Implementa la conexión con un medidor usando ComsPQLibrary.</summary>
public class ConexionMedidorService : IConexionMedidorService
{
    private readonly JsonStore _jsonStore;
    private readonly AppDbContext _db;
    private readonly ILogger<ConexionMedidorService> _logger;

    /// <summary>Inicializa el servicio con JSON, base de datos y logger.</summary>
    public ConexionMedidorService(
        JsonStore jsonStore,
        AppDbContext db,
        ILogger<ConexionMedidorService> logger)
    {
        _jsonStore = jsonStore;
        _db = db;
        _logger = logger;
    }

    /// <summary>Conecta con el medidor identificado por su dirRed.</summary>
    public async Task<bool> ConectarAsync(ConexionMedidorRequest request)
    {
        var protocoloOrigen = _jsonStore.CargarProtocolo();
        var transporte = _jsonStore.CargarTransporte();

        // Se consulta la BD por dirRed para obtener datos del medidor, como dirRF.
        var medidoresEncontrados = await _db.MedidoresQP
            .AsNoTracking()
            .Where(medidor => medidor.dirRed == request.DirRed)
            .Select(medidor => new
            {
                medidor.idMedidor,
                medidor.dirRed,
                medidor.dirRF
            })
            .Take(2)
            .ToListAsync();

        if (medidoresEncontrados.Count == 0)
            throw new KeyNotFoundException($"No se encontró un medidor con dirRed {request.DirRed}.");

        if (medidoresEncontrados.Count > 1)
        {
            throw new ArgumentException(
                $"La dirRed {request.DirRed} está asignada a varios medidores; no se puede identificar uno de forma única.");
        }

        var medidorDb = medidoresEncontrados[0];
        var usarRf = request.TipoTransporte == 1;

        if (usarRf && string.IsNullOrWhiteSpace(medidorDb.dirRF))
            throw new InvalidOperationException($"El medidor {medidorDb.idMedidor} no tiene dirRF configurada en la BD.");

        if (request.TipoTransporte == 2 && string.IsNullOrWhiteSpace(transporte.EthernetParams.Ip))
            throw new InvalidOperationException("TransporteConfig.json no tiene configurada EthernetParams.Ip.");

        var protocolo = new ComsProtocoloConfig
        {
            Id = protocoloOrigen.Id,
            // 1 selecciona RF por serial; 2 selecciona Ethernet directo.
            TipoTransporte = request.TipoTransporte,
            DataPacker = protocoloOrigen.DataPacker,
            UsarRF = usarRf,
            PubTramasRxTx = protocoloOrigen.PubTramasRxTx
        };

        _logger.LogInformation(
            "Transporte seleccionado: {TipoTransporte}; medidor {IdMedidor}, dirRed {DirRed}",
            protocolo.TipoTransporte,
            medidorDb.idMedidor,
            medidorDb.dirRed);

        var parametros = new List<ParametroComs>();

        if (protocolo.TipoTransporte == 1)
        {
            // TEMPORAL: registrar dirRF y parámetros seriales para diagnosticar la falta de respuesta.
            _logger.LogInformation(
                "Configuración RF/serial: IdMedidor={IdMedidor}, dirRed={DirRed}, dirRF='{DirRF}', COM={COM}, BaudRate={BaudRate}, Parity={Parity}, DataBits={DataBits}, StopBits={StopBits}, Timeout={Timeout}",
                medidorDb.idMedidor,
                medidorDb.dirRed,
                medidorDb.dirRF,
                transporte.SerialParams.COM,
                transporte.SerialParams.BaudRate,
                transporte.SerialParams.Paridad,
                transporte.SerialParams.BitsDatos,
                transporte.SerialParams.BitsParo,
                transporte.SerialParams.Timeout);

            parametros.Add(new ParametroComs { Name = "COM", Valor = transporte.SerialParams.COM });
            parametros.Add(new ParametroComs { Name = "BaudRate", Valor = transporte.SerialParams.BaudRate });
            parametros.Add(new ParametroComs { Name = "Parity", Valor = transporte.SerialParams.Paridad });
            parametros.Add(new ParametroComs { Name = "DataBits", Valor = transporte.SerialParams.BitsDatos });
            parametros.Add(new ParametroComs { Name = "StopBits", Valor = transporte.SerialParams.BitsParo });
            parametros.Add(new ParametroComs { Name = "Timeout", Valor = transporte.SerialParams.Timeout });
        }
        else if (protocolo.TipoTransporte == 2)
        {
            // La IP, el puerto y el timeout Ethernet vuelven a leerse desde TransporteConfig.json.
            _logger.LogInformation(
                "Configuración Ethernet desde JSON: IdMedidor={IdMedidor}, IP='{Ip}', puerto={Port}, Timeout={Timeout}",
                medidorDb.idMedidor,
                transporte.EthernetParams.Ip,
                transporte.EthernetParams.Port,
                transporte.EthernetParams.Timeout);

            parametros.Add(new ParametroComs { Name = "IP", Valor = transporte.EthernetParams.Ip });
            parametros.Add(new ParametroComs { Name = "Port", Valor = transporte.EthernetParams.Port });
            parametros.Add(new ParametroComs { Name = "Timeout", Valor = transporte.EthernetParams.Timeout });
        }
        else
        {
            throw new InvalidOperationException(
                $"Tipo de transporte no reconocido: {protocolo.TipoTransporte}.");
        }

        var servicioComs = new ServicioComs(protocolo, parametros.ToArray());
        servicioComs.ErrorComsEvent += (ex, origen) =>
            _logger.LogError(ex, "Error de comunicación en {Origen}", origen);
        servicioComs.TramasTxRxEvent += trama =>
            _logger.LogInformation("Trama de comunicación: {Trama}", trama);

        var medidorComs = new ComsPQLibrary.Modelos.MedidorPQ
        {
            DirRed = (ushort)medidorDb.dirRed,
            DirRF = usarRf ? medidorDb.dirRF : null
        };

        var cronometro = Stopwatch.StartNew();
        _logger.LogInformation(
            "Iniciando conexión con IdMedidor={IdMedidor}, dirRed={DirRed}, modo={Modo}",
            medidorDb.idMedidor,
            medidorDb.dirRed,
            usarRf ? "RF/serial" : "Ethernet");

        try
        {
            var conectado = await servicioComs.Conectar(medidorComs);
            cronometro.Stop();

            _logger.LogInformation(
                "Intento de conexión terminado: IdMedidor={IdMedidor}, conectado={Conectado}, duración={DuracionMs} ms",
                medidorDb.idMedidor,
                conectado,
                cronometro.ElapsedMilliseconds);

            return conectado;
        }
        catch (Exception ex)
        {
            cronometro.Stop();
            _logger.LogError(
                ex,
                "Excepción al conectar IdMedidor={IdMedidor}, dirRed={DirRed}, modo={Modo}, duración={DuracionMs} ms",
                medidorDb.idMedidor,
                medidorDb.dirRed,
                usarRf ? "RF/serial" : "Ethernet",
                cronometro.ElapsedMilliseconds);
            throw;
        }
    }
}
