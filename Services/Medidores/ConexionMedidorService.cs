using MedicionPQ.Modelos.Medidores;
using ComsProtocoloConfig = ComsPQLibrary.Modelos.ProtocoloConfig;
using ParametroComs = ComsPQLibrary.Modelos.ParamT;
using ServicioComs = ComsPQLibrary.ComsPQServicio.ComsPQService;
using Microsoft.Extensions.Logging;

namespace MedicionPQ.Services.Medidores;

/// <summary>
/// Implementa la conexion con un medidor, usando la librería ComsPQLibrary.
/// </summary>
public class ConexionMedidorService : IConexionMedidorService
{
    private readonly JsonStore _jsonStore;
    private readonly ILogger<ConexionMedidorService> _logger;

    /// <summary>
    /// Inicializa el servicio con el alamcén de configuración JSON.
    /// </summary>  
    public ConexionMedidorService(
        JsonStore jsonStore,
        ILogger<ConexionMedidorService> logger)
    {
        _jsonStore = jsonStore;
        _logger = logger;
    }

    /// <summary> Intenta conectar con el medidor indicado.</summary>
    public async Task<bool> ConectarAsync(ConexionMedidorRequest request)
    {
        var protocoloOrigen = _jsonStore.CargarProtocolo();
        var transporte = _jsonStore.CargarTransporte();

        // TEMPORAL: confirmar qué IP y puerto leyó JsonStore durante el diagnóstico de conexión.
        _logger.LogInformation(
            "Configuración Ethernet leída: IP='{Ip}', puerto={Port}",
            transporte.EthernetParams.Ip,
            transporte.EthernetParams.Port);

        var protocolo = new ComsProtocoloConfig
        {
            Id = protocoloOrigen.Id,
            TipoTransporte = protocoloOrigen.TipoTransporte,
            DataPacker = protocoloOrigen.DataPacker,
            UsarRF = protocoloOrigen.UsarRF,
            PubTramasRxTx = protocoloOrigen.PubTramasRxTx
        };

        var parametros = new List<ParametroComs>();

        if (protocolo.TipoTransporte == 1)
        {
            parametros.Add(new ParametroComs { Name = "COM", Valor = transporte.SerialParams.COM });
            parametros.Add(new ParametroComs { Name = "BaudRate", Valor = transporte.SerialParams.BaudRate });
            parametros.Add(new ParametroComs { Name = "Parity", Valor = transporte.SerialParams.Paridad });
            parametros.Add(new ParametroComs { Name = "DataBits", Valor = transporte.SerialParams.BitsDatos });
            parametros.Add(new ParametroComs { Name = "StopBits", Valor = transporte.SerialParams.BitsParo });
            parametros.Add(new ParametroComs { Name = "Timeout", Valor = transporte.SerialParams.Timeout });
        }
        else if (protocolo.TipoTransporte == 2)
        {
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
            DirRed = request.DirRed
        };

        return await servicioComs.Conectar(medidorComs);
    }

}

