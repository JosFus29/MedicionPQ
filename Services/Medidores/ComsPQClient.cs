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

    /// <summary>
    /// Tiempo máximo que una solicitud espera a que se libere el puerto. Si otra operación lo ocupa más tiempo,
    /// se lanza <see cref="PuertoOcupadoException"/> en lugar de acumular peticiones indefinidamente.
    /// </summary>
    private static readonly TimeSpan EsperaMaximaPuerto = TimeSpan.FromSeconds(30);

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

    /// <summary>
    /// Espera el turno exclusivo del puerto como máximo <see cref="EsperaMaximaPuerto"/>.
    /// Si no lo obtiene, lanza <see cref="PuertoOcupadoException"/>; quien lo obtiene debe liberar el semáforo.
    /// </summary>
    private async Task EsperarTurnoAsync(string operacion, MedidorComs medidor)
    {
        _logger.LogDebug(
            "Esperando turno del puerto para {Operacion}. DirRed={DirRed}, DirRF={DirRF}",
            operacion,
            medidor.DirRed,
            medidor.DirRF);

        if (!await _semaforo.WaitAsync(EsperaMaximaPuerto))
        {
            _logger.LogWarning(
                "No se obtuvo el turno del puerto para {Operacion} después de {EsperaSegundos} s. DirRed={DirRed}",
                operacion,
                EsperaMaximaPuerto.TotalSeconds,
                medidor.DirRed);
            throw new PuertoOcupadoException(EsperaMaximaPuerto);
        }
    }

    /// <summary>
    /// Solicita a la DLL conectar con un medidor. Evita llamadas concurrentes al puerto serial.
    /// Devuelve false si la DLL no conectó o lanzó una excepción (ya registrada en el log).
    /// </summary>
    public async Task<bool> ConectarAsync(MedidorComs medidor)
    {
        await EsperarTurnoAsync("Conectar", medidor);
        try
        {
            return await ConectarConLaDllAsync(medidor);
        }
        catch (Exception)
        {
            // ConectarConLaDllAsync ya registra la excepción de la DLL.
            return false;
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
        await EsperarTurnoAsync("Conectar y sincronizar", medidor);
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
    /// Conecta con el medidor y solicita los puntos de un grupo dentro del mismo turno exclusivo del puerto.
    /// El método de la DLL depende del grupo (ver <see cref="SolicitarALaDll"/>).
    /// </summary>
    public async Task<(bool Conectado, bool LecturaExitosa)> ConectarYSolicitarGrupoAsync(
        MedidorComs medidor,
        List<PuntoIODtoComs> puntos,
        GrupoLectura grupo)
    {
        var metodoDll = NombreMetodoDll(grupo);

        _logger.LogInformation(
            "Iniciando conexión y solicitud de {Grupo}: DirRed={DirRed}, CantidadPuntos={CantidadPuntos}, MetodoDll={MetodoDll}, UsarRF={UsarRF}",
            grupo.Descripcion(),
            medidor.DirRed,
            puntos.Count,
            metodoDll,
            UsarRF);

        await EsperarTurnoAsync($"Conectar y {metodoDll}", medidor);
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
                return (false, false);
            }

            if (!conectado)
            {
                _logger.LogWarning(
                    "No se llamará {MetodoDll} porque Conectar devolvió false. DirRed={DirRed}",
                    metodoDll,
                    medidor.DirRed);
                return (false, false);
            }

            try
            {
                var lecturaExitosa = await SolicitarALaDll(grupo, medidor, puntos);
                if (lecturaExitosa)
                {
                    _logger.LogInformation(
                        "La DLL confirmó {MetodoDll}. DirRed={DirRed}, CantidadPuntos={CantidadPuntos}",
                        metodoDll,
                        medidor.DirRed,
                        puntos.Count);
                }
                else
                {
                    _logger.LogWarning(
                        "ComsPQService.{MetodoDll} devolvió false. DirRed={DirRed}, CantidadPuntos={CantidadPuntos}",
                        metodoDll,
                        medidor.DirRed,
                        puntos.Count);
                }

                return (true, lecturaExitosa);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ComsPQLibrary lanzó una excepción durante {MetodoDll}. DirRed={DirRed}, CantidadPuntos={CantidadPuntos}",
                    metodoDll,
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

    /// <summary>
    /// Conecta con el medidor y, en el mismo turno exclusivo del puerto, llama a SolArmonicos una vez por fase.
    /// A diferencia de los grupos, SolArmonicos recibe un solo punto y la fase, y devuelve los valores como float[].
    /// Si una fase falla se registra y se continúa con las demás; su posición en el resultado queda en null.
    /// </summary>
    /// <returns>Si conectó y, en el mismo orden que <paramref name="fases"/>, los valores de cada fase (null si falló).</returns>
    public async Task<(bool Conectado, IReadOnlyList<float[]?> ValoresPorFase)> ConectarYSolicitarArmonicosAsync(
        MedidorComs medidor,
        IReadOnlyList<(ushort Fase, PuntoIODtoComs Punto)> fases)
    {
        var sinLecturas = fases.Select(_ => (float[]?)null).ToList();

        _logger.LogInformation(
            "Iniciando conexión y solicitud de armónicos: DirRed={DirRed}, CantidadFases={CantidadFases}, UsarRF={UsarRF}",
            medidor.DirRed,
            fases.Count,
            UsarRF);

        await EsperarTurnoAsync($"Conectar y {nameof(ServicioComs.SolArmonicos)}", medidor);
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
                return (false, sinLecturas);
            }

            if (!conectado)
            {
                _logger.LogWarning(
                    "No se llamará SolArmonicos porque Conectar devolvió false. DirRed={DirRed}",
                    medidor.DirRed);
                return (false, sinLecturas);
            }

            var valoresPorFase = new List<float[]?>(fases.Count);
            foreach (var (fase, punto) in fases)
            {
                _logger.LogInformation(
                    "Llamando a ComsPQService.SolArmonicos. DirRed={DirRed}, Fase={Fase}, PuntoTag={PuntoTag}, DirComsCEC={DirComsCEC}",
                    medidor.DirRed,
                    fase,
                    punto.Tag,
                    punto.DirComsCEC);

                try
                {
                    var valores = await _servicioComs.SolArmonicos(medidor, fase, punto);
                    if (valores is null || valores.Length == 0)
                    {
                        _logger.LogWarning(
                            "ComsPQService.SolArmonicos no devolvió valores. DirRed={DirRed}, Fase={Fase}, PuntoTag={PuntoTag}",
                            medidor.DirRed,
                            fase,
                            punto.Tag);
                        valoresPorFase.Add(null);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "La DLL devolvió armónicos. DirRed={DirRed}, Fase={Fase}, PuntoTag={PuntoTag}, CantidadValores={CantidadValores}",
                            medidor.DirRed,
                            fase,
                            punto.Tag,
                            valores.Length);
                        valoresPorFase.Add(valores);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "ComsPQLibrary lanzó una excepción durante SolArmonicos. DirRed={DirRed}, Fase={Fase}, PuntoTag={PuntoTag}, DirComsCEC={DirComsCEC}",
                        medidor.DirRed,
                        fase,
                        punto.Tag,
                        punto.DirComsCEC);
                    valoresPorFase.Add(null);
                }
            }

            return (true, valoresPorFase);
        }
        finally
        {
            _semaforo.Release();
        }
    }

    /// <summary>Llama al método de la DLL que lee el grupo; la DLL escribe cada lectura en PuntoIODto.Valor.</summary>
    private Task<bool> SolicitarALaDll(GrupoLectura grupo, MedidorComs medidor, List<PuntoIODtoComs> puntos) => grupo switch
    {
        GrupoLectura.Instantaneos => _servicioComs.SolInstT(medidor, puntos),
        GrupoLectura.Variables5Min => _servicioComs.SolUV5M(medidor, puntos),
        GrupoLectura.Consumos => _servicioComs.SolConsumos(medidor, puntos),
        GrupoLectura.CalidadPotencia => _servicioComs.SolCalPot(medidor, puntos),
        _ => throw new ArgumentOutOfRangeException(nameof(grupo), grupo, "Grupo sin método de lectura en la DLL.")
    };

    /// <summary>Nombre del método de la DLL que corresponde al grupo, para los logs.</summary>
    private static string NombreMetodoDll(GrupoLectura grupo) => grupo switch
    {
        GrupoLectura.Instantaneos => nameof(ServicioComs.SolInstT),
        GrupoLectura.Variables5Min => nameof(ServicioComs.SolUV5M),
        GrupoLectura.Consumos => nameof(ServicioComs.SolConsumos),
        GrupoLectura.CalidadPotencia => nameof(ServicioComs.SolCalPot),
        _ => grupo.ToString()
    };

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
