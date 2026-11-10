using MedicionPQ.Modelos.Medidores;
using ComsProtocoloConfig = ComsPQLibrary.Modelos.ProtocoloConfig;
using ParametroComs = ComsPQLibrary.Modelos.ParamT;
using MedidorComs = ComsPQLibrary.Modelos.MedidorPQ;
using ServicioComs = ComsPQLibrary.ComsPQServicio.ComsPQService;

namespace MedicionPQ.Services.Medidores;

/// <summary>
/// Mantiene una instancia de ComsPQService para reutilizar el transporte serial entre solicitudes.
/// La DLL abre y opera el puerto; esta clase solo le pasa configuración y medidores.
/// </summary>
public sealed class ComsPQClient
{
    private readonly ServicioComs _servicioComs;
    private readonly ILogger<ComsPQClient> _logger;
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    /// <summary>Indica si la configuración JSON activa el envío por radiofrecuencia.</summary>
    public bool UsarRF { get; }

    /// <summary>Construye una sola instancia de comunicación con la configuración de los JSON.</summary>
    public ComsPQClient(JsonStore jsonStore, ILogger<ComsPQClient> logger)
    {
        _logger = logger;

        var protocoloOrigen = jsonStore.CargarProtocolo();
        var transporte = jsonStore.CargarTransporte();

        if (protocoloOrigen.TipoTransporte != 1)
        {
            throw new InvalidOperationException(
                "Por ahora solo está habilitado el puerto serial. Configura TipoTransporte = 1 en ProtocoloConfig.json.");
        }

        UsarRF = protocoloOrigen.UsarRF;

        var protocolo = new ComsProtocoloConfig
        {
            Id = protocoloOrigen.Id,
            TipoTransporte = protocoloOrigen.TipoTransporte,
            DataPacker = protocoloOrigen.DataPacker,
            UsarRF = protocoloOrigen.UsarRF,
            PubTramasRxTx = protocoloOrigen.PubTramasRxTx
        };

        var parametros = new[]
        {
            new ParametroComs { Name = "COM", Valor = transporte.SerialParams.COM },
            new ParametroComs { Name = "BaudRate", Valor = transporte.SerialParams.BaudRate },
            new ParametroComs { Name = "Parity", Valor = transporte.SerialParams.Paridad },
            new ParametroComs { Name = "DataBits", Valor = transporte.SerialParams.BitsDatos },
            new ParametroComs { Name = "StopBits", Valor = transporte.SerialParams.BitsParo },
            new ParametroComs { Name = "Timeout", Valor = transporte.SerialParams.Timeout }
        };

        _logger.LogInformation(
            "Creando cliente serial: COM={COM}, BaudRate={BaudRate}, Parity={Parity}, DataBits={DataBits}, StopBits={StopBits}, Timeout={Timeout}, UsarRF={UsarRF}",
            transporte.SerialParams.COM,
            transporte.SerialParams.BaudRate,
            transporte.SerialParams.Paridad,
            transporte.SerialParams.BitsDatos,
            transporte.SerialParams.BitsParo,
            transporte.SerialParams.Timeout,
            UsarRF);

        // La DLL configura y abre el transporte al construir ComsPQService.
        // Capturamos excepciones que puedan escapar durante esa inicialización.
        try
        {
            _servicioComs = new ServicioComs(protocolo, parametros);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(
                ex,
                "La DLL lanzó una excepción al crear ComsPQService. COM={COM}, UsarRF={UsarRF}",
                transporte.SerialParams.COM,
                UsarRF);
            throw;
        }

        // Los eventos permiten registrar excepciones que la DLL captura internamente.
        _servicioComs.ErrorComsEvent += RegistrarErrorComs;
        _servicioComs.TramasTxRxEvent += trama =>
            _logger.LogInformation("Trama de comunicación: {Trama}", trama);
    }

    /// <summary>Registra las excepciones que ComsPQLibrary publica mediante ErrorComsEvent.</summary>
    private void RegistrarErrorComs(Exception ex, string origen)
    {
        _logger.LogError(
            ex,
            "ComsPQLibrary reportó un error. Origen={Origen}, TipoExcepcion={TipoExcepcion}, HResult={HResult}, Mensaje={Mensaje}",
            origen,
            ex.GetType().FullName,
            ex.HResult,
            ex.Message);
    }

    /// <summary>Solicita a la DLL conectar con un medidor. Evita llamadas concurrentes al puerto serial.</summary>
    public async Task<bool> ConectarAsync(MedidorComs medidor)
    {
        _logger.LogDebug(
            "Esperando turno para llamar a la DLL. DirRed={DirRed}, DirRF={DirRF}",
            medidor.DirRed,
            medidor.DirRF);

        await _semaforo.WaitAsync();
        try
        {
            _logger.LogInformation(
                "Llamando a ComsPQService.Conectar. DirRed={DirRed}, UsarRF={UsarRF}, DirRF={DirRF}",
                medidor.DirRed,
                UsarRF,
                medidor.DirRF);

            try
            {
                var conectado = await _servicioComs.Conectar(medidor);
                if (!conectado)
                {
                    _logger.LogWarning(
                        "ComsPQService.Conectar terminó con false; la DLL no confirmó la conexión. DirRed={DirRed}, UsarRF={UsarRF}, DirRF={DirRF}",
                        medidor.DirRed,
                        UsarRF,
                        medidor.DirRF);
                }

                return conectado;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ComsPQLibrary lanzó una excepción durante Conectar. DirRed={DirRed}, UsarRF={UsarRF}, DirRF={DirRF}",
                    medidor.DirRed,
                    UsarRF,
                    medidor.DirRF);
                throw;
            }
        }
        finally
        {
            _semaforo.Release();
        }
    }
}
