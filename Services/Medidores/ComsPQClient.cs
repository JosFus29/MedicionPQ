using MedicionPQ.Modelos.Medidores;
using ComsProtocoloConfig = ComsPQLibrary.Modelos.ProtocoloConfig;
using ParametroComs = ComsPQLibrary.Modelos.ParamT;
using MedidorComs = ComsPQLibrary.Modelos.MedidorPQ;
using PuntoIODtoComs = ComsPQLibrary.Modelos.PuntoIODto;
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
            return await ConectarConLaDllAsync(medidor);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>
    /// Ejecuta Conectar y luego Sincronizar en una sola sección exclusiva del puerto.
    /// Esta operación es distinta de ConectarAsync: la sincronización solo se intenta
    /// después de que la DLL confirme la conexión.
    /// </summary>
    public async Task<SincronizacionMedidorResult> ConectarYSincronizarAsync(
        MedidorComs medidor,
        PuntoIODtoComs puntoSincronizar)
    {
        _logger.LogDebug(
            "Esperando turno para conectar y sincronizar. DirRed={DirRed}, PuntoTag={PuntoTag}, DirComsCEC={DirComsCEC}",
            medidor.DirRed,
            puntoSincronizar.Tag,
            puntoSincronizar.DirComsCEC);

        await _semaforo.WaitAsync();
        try
        {
            bool conectado;
            try
            {
                conectado = await ConectarConLaDllAsync(medidor);
            }
            catch (Exception)
            {
                // ConectarConLaDllAsync ya registra la excepción de la DLL.

                return new SincronizacionMedidorResult
                {
                    Conectado = false,
                    Sincronizado = false,
                    Mensaje = "La DLL lanzó una excepción durante la conexión."
                };
            }

            if (!conectado)
            {
                _logger.LogWarning(
                    "Se omite Sincronizar porque ComsPQService.Conectar devolvió false. DirRed={DirRed}",
                    medidor.DirRed);

                return new SincronizacionMedidorResult
                {
                    Conectado = false,
                    Sincronizado = false,
                    Mensaje = "No se pudo conectar con el medidor; no se intentó sincronizar."
                };
            }

            _logger.LogInformation(
                "Llamando a ComsPQService.Sincronizar. DirRed={DirRed}, PuntoTag={PuntoTag}, DirComsCEC={DirComsCEC}",
                medidor.DirRed,
                puntoSincronizar.Tag,
                puntoSincronizar.DirComsCEC);

            try
            {
                var sincronizado = await _servicioComs.Sincronizar(medidor, puntoSincronizar);
                if (!sincronizado)
                {
                    _logger.LogWarning(
                        "ComsPQService.Sincronizar devolvió false. DirRed={DirRed}, PuntoTag={PuntoTag}, DirComsCEC={DirComsCEC}",
                        medidor.DirRed,
                        puntoSincronizar.Tag,
                        puntoSincronizar.DirComsCEC);
                }
                else
                {
                    _logger.LogInformation(
                        "Sincronización confirmada por la DLL. DirRed={DirRed}, PuntoTag={PuntoTag}",
                        medidor.DirRed,
                        puntoSincronizar.Tag);
                }

                return new SincronizacionMedidorResult
                {
                    Conectado = true,
                    Sincronizado = sincronizado,
                    Mensaje = sincronizado
                        ? "Conexión y sincronización establecidas."
                        : "El medidor conectó, pero la DLL no confirmó la sincronización."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ComsPQLibrary lanzó una excepción durante Sincronizar. DirRed={DirRed}, PuntoTag={PuntoTag}, DirComsCEC={DirComsCEC}",
                    medidor.DirRed,
                    puntoSincronizar.Tag,
                    puntoSincronizar.DirComsCEC);

                return new SincronizacionMedidorResult
                {
                    Conectado = true,
                    Sincronizado = false,
                    Mensaje = "El medidor conectó, pero la DLL lanzó una excepción durante la sincronización."
                };
            }
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>
    /// Conecta con el medidor y solicita instantáneos dentro del mismo turno exclusivo del puerto.
    /// </summary>
    public async Task<(bool Conectado, bool LecturaExitosa)> ConectarYSolicitarInstantaneosAsync(
        MedidorComs medidor,
        List<PuntoIODtoComs> puntos)
    {
        _logger.LogInformation(
            "Iniciando conexión y solicitud de instantáneos: DirRed={DirRed}, CantidadPuntos={CantidadPuntos}, UsarRF={UsarRF}",
            medidor.DirRed,
            puntos.Count,
            UsarRF);

        await _semaforo.WaitAsync();
        try
        {
            bool conectado;
            try
            {
                conectado = await ConectarConLaDllAsync(medidor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "La DLL lanzó una excepción al conectar antes de solicitar instantáneos. DirRed={DirRed}",
                    medidor.DirRed);
                return (false, false);
            }

            if (!conectado)
            {
                _logger.LogWarning(
                    "No se solicitarán instantáneos porque Conectar devolvió false. DirRed={DirRed}",
                    medidor.DirRed);
                return (false, false);
            }

            try
            {
                var lecturaExitosa = await _servicioComs.SolInstT(medidor, puntos);
                if (lecturaExitosa)
                {
                    _logger.LogInformation(
                        "La DLL confirmó la solicitud de instantáneos. DirRed={DirRed}, CantidadPuntos={CantidadPuntos}",
                        medidor.DirRed,
                        puntos.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "ComsPQService.SolInstT devolvió false. DirRed={DirRed}, CantidadPuntos={CantidadPuntos}",
                        medidor.DirRed,
                        puntos.Count);
                }

                return (true, lecturaExitosa);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ComsPQLibrary lanzó una excepción durante SolInstT. DirRed={DirRed}, CantidadPuntos={CantidadPuntos}",
                    medidor.DirRed,
                    puntos.Count);
                return (true, false);
            }
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>Invoca únicamente la operación Conectar de la DLL; no sincroniza el medidor.</summary>
    private async Task<bool> ConectarConLaDllAsync(MedidorComs medidor)
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
}
