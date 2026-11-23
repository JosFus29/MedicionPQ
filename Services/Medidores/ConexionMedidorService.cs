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
    private sealed record PuntoSincronizacionDb(int IdPunto, string Tag, int DirCCEC);

    /// <summary>idGF de los armónicos en IO_GrupoFuncional; la fase de cada punto se toma de su IdGE.</summary>
    private const int IdGFArmonicos = 6;

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
        _logger.LogInformation(
            "Buscando punto de sincronización en PuntosIO: IdMedidor={IdMedidor}, IdPunto={IdPunto}",
            idMedidor,
            _puntoSincronizar.IdPunto);

        var cronometroBusqueda = Stopwatch.StartNew();
        PuntoSincronizacionDb? puntoBd;
        try
        {
            puntoBd = await _db.PuntosIO
                .AsNoTracking()
                .Where(punto => punto.IdPunto == _puntoSincronizar.IdPunto)
                .Select(punto => new PuntoSincronizacionDb(
                    punto.IdPunto,
                    punto.Tag,
                    punto.DirCCEC))
                .SingleOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falló la consulta del punto de sincronización en PuntosIO: IdMedidor={IdMedidor}, IdPunto={IdPunto}",
                idMedidor,
                _puntoSincronizar.IdPunto);
            throw;
        }
        finally
        {
            cronometroBusqueda.Stop();
        }

        if (puntoBd is null)
        {
            _logger.LogWarning(
                "No se encontró el punto configurado para sincronización: IdMedidor={IdMedidor}, IdPunto={IdPunto}, DuracionConsultaMs={DuracionConsultaMs}",
                idMedidor,
                _puntoSincronizar.IdPunto,
                cronometroBusqueda.ElapsedMilliseconds);
            throw new InvalidOperationException(
                $"No existe un registro PuntosIO con el IdPunto configurado {_puntoSincronizar.IdPunto}.");
        }

        _logger.LogInformation(
            "Punto de sincronización encontrado: IdMedidor={IdMedidor}, IdPunto={IdPunto}, Tag={Tag}, DirCCEC={DirCCEC}, DuracionConsultaMs={DuracionConsultaMs}",
            idMedidor,
            puntoBd.IdPunto,
            puntoBd.Tag,
            puntoBd.DirCCEC,
            cronometroBusqueda.ElapsedMilliseconds);

        if (puntoBd.DirCCEC is < 0 or > ushort.MaxValue)
        {
            _logger.LogError(
                "DirCCEC fuera del rango ushort requerido por la DLL: IdMedidor={IdMedidor}, IdPunto={IdPunto}, DirCCEC={DirCCEC}",
                idMedidor,
                puntoBd.IdPunto,
                puntoBd.DirCCEC);
            throw new InvalidOperationException(
                $"El registro PuntosIO {puntoBd.IdPunto} tiene un DirCCEC fuera del rango que admite la DLL.");
        }

        // Para SimIV, ComsPQLibrary usa DirComsCEC. DirModbus y Valor no se utilizan aquí.
        var puntoDll = new PuntoIODtoComs
        {
            Tag = puntoBd.Tag,
            DirComsCEC = (ushort)puntoBd.DirCCEC
        };

        _logger.LogDebug(
            "Punto PuntosIO adaptado al DTO de ComsPQLibrary: IdPunto={IdPunto}, Tag={Tag}, DirCCEC={DirCCEC}, DirComsCEC={DirComsCEC}",
            puntoBd.IdPunto,
            puntoDll.Tag,
            puntoBd.DirCCEC,
            puntoDll.DirComsCEC);

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
    /// Obtiene de PuntosIO los puntos activos (Edo = 1) del grupo indicado y solicita sus valores a la DLL.
    /// El valor del enum <see cref="GrupoLectura"/> es el IdGF de IO_GrupoFuncional.
    /// </summary>
    public async Task<LecturaGrupoMedidorResult> SolicitarGrupoAsync(ConexionMedidorRequest request, GrupoLectura grupo)
    {
        var idGF = (int)grupo;
        var descripcion = grupo.Descripcion();
        var (idMedidor, medidorComs) = await ObtenerMedidorComsAsync(request.IdMedidor);

        _logger.LogInformation(
            "Buscando puntos de {Grupo} en PuntosIO: IdMedidor={IdMedidor}, IdGF={IdGF}",
            descripcion,
            idMedidor,
            idGF);

        // Solo se piden los puntos activos (Edo = 1) del grupo funcional.
        var puntosBd = await _db.PuntosIO
            .AsNoTracking()
            .Where(punto => punto.IdGF == idGF && punto.Edo)
            .OrderBy(punto => punto.DirCCEC)
            .Select(punto => new
            {
                punto.IdPunto,
                punto.Tag,
                punto.DirCCEC,
                punto.DirMB
            })
            .ToListAsync();

        if (puntosBd.Count == 0)
        {
            _logger.LogWarning(
                "No hay puntos PuntosIO activos con IdGF={IdGF} ({Grupo}); no se llamará a la DLL. IdMedidor={IdMedidor}",
                idGF,
                descripcion,
                idMedidor);
            throw new InvalidOperationException($"No hay puntos activos configurados con IdGF = {idGF} para solicitar {descripcion}.");
        }

        var puntosInvalidos = puntosBd
            .Where(punto => punto.DirCCEC is < 0 or > ushort.MaxValue
                || punto.DirMB is < 0 or > ushort.MaxValue)
            .ToList();

        if (puntosInvalidos.Count > 0)
        {
            _logger.LogError(
                "Hay puntos de {Grupo} con direcciones fuera del rango ushort de la DLL. IdMedidor={IdMedidor}, IdGF={IdGF}, IdPuntos={IdPuntos}",
                descripcion,
                idMedidor,
                idGF,
                string.Join(",", puntosInvalidos.Select(punto => punto.IdPunto)));
            throw new InvalidOperationException($"Uno o más puntos con IdGF = {idGF} tienen direcciones fuera del rango que admite la DLL.");
        }

        var puntosDll = puntosBd
            .Select(punto => new PuntoIODtoComs
            {
                Tag = punto.Tag,
                DirComsCEC = (ushort)punto.DirCCEC,
                DirModbus = (ushort)punto.DirMB
            })
            .ToList();

        _logger.LogInformation(
            "Se cargarán puntos de {Grupo} desde PuntosIO: IdMedidor={IdMedidor}, CantidadPuntos={CantidadPuntos}, IdGF={IdGF}, UsarRF={UsarRF}",
            descripcion,
            idMedidor,
            puntosDll.Count,
            idGF,
            _comsClient.UsarRF);

        var cronometro = Stopwatch.StartNew();
        var (conectado, lecturaExitosa) = await _comsClient.ConectarYSolicitarGrupoAsync(medidorComs, puntosDll, grupo);
        cronometro.Stop();

        var puntosResultado = puntosBd
            .Select((punto, indice) => new LecturaPuntoResult
            {
                IdPunto = punto.IdPunto,
                Tag = punto.Tag,
                DirCCEC = punto.DirCCEC,
                DirModbus = punto.DirMB,
                Valor = puntosDll[indice].Valor
            })
            .ToList();

        _logger.LogInformation(
            "Terminó la solicitud de {Grupo}: IdMedidor={IdMedidor}, IdGF={IdGF}, Conectado={Conectado}, LecturaExitosa={LecturaExitosa}, CantidadPuntos={CantidadPuntos}, DuracionMs={DuracionMs}",
            descripcion,
            idMedidor,
            idGF,
            conectado,
            lecturaExitosa,
            puntosResultado.Count,
            cronometro.ElapsedMilliseconds);

        return new LecturaGrupoMedidorResult
        {
            IdMedidor = idMedidor,
            IdGF = idGF,
            Conectado = conectado,
            LecturaExitosa = lecturaExitosa,
            Mensaje = !conectado
                ? "No se pudo conectar con el medidor."
                : lecturaExitosa
                    ? $"Se solicitaron los puntos de {descripcion}."
                    : $"El medidor conectó, pero no se confirmó la lectura de {descripcion}.",
            Puntos = puntosResultado
        };
    }

    /// <summary>
    /// Obtiene de PuntosIO los puntos activos de armónicos (IdGF = 6) y envía cada uno a la DLL como PuntoIODto;
    /// la fase es el IdGE del punto (1 = A, 2 = B, 3 = C). Puede haber varios puntos por fase (ARM_V* y ARM_I*).
    /// Todos se solicitan en una sola conexión.
    /// </summary>
    public async Task<LecturaArmonicosResult> SolicitarArmonicosAsync(ConexionMedidorRequest request)
    {
        var (idMedidor, medidorComs) = await ObtenerMedidorComsAsync(request.IdMedidor);

        _logger.LogInformation(
            "Buscando puntos de armónicos en PuntosIO: IdMedidor={IdMedidor}, IdGF={IdGF}",
            idMedidor,
            IdGFArmonicos);

        // Orden: fase A, B, C; dentro de cada fase, ARM_V* antes que ARM_I*.
        var puntosBd = await _db.PuntosIO
            .AsNoTracking()
            .Where(punto => punto.IdGF == IdGFArmonicos && punto.Edo)
            .OrderBy(punto => punto.IdGE)
            .ThenByDescending(punto => punto.Tag)
            .Select(punto => new
            {
                punto.IdPunto,
                punto.Tag,
                punto.IdGE,
                punto.DirCCEC,
                punto.DirMB
            })
            .ToListAsync();

        if (puntosBd.Count == 0)
        {
            _logger.LogWarning(
                "No hay puntos PuntosIO activos con IdGF={IdGF} (armónicos); no se llamará a la DLL. IdMedidor={IdMedidor}",
                IdGFArmonicos,
                idMedidor);
            throw new InvalidOperationException($"No hay puntos activos configurados con IdGF = {IdGFArmonicos} para solicitar armónicos.");
        }

        // El IdGE es la fase que se envía a la DLL: solo se admiten 1, 2 y 3.
        var fasesInvalidas = puntosBd.Where(punto => punto.IdGE is < 1 or > 3).ToList();
        if (fasesInvalidas.Count > 0)
        {
            _logger.LogError(
                "Hay puntos de armónicos con IdGE fuera de 1-3. IdMedidor={IdMedidor}, IdPuntos={IdPuntos}",
                idMedidor,
                string.Join(",", fasesInvalidas.Select(punto => punto.IdPunto)));
            throw new InvalidOperationException(
                $"Uno o más puntos con IdGF = {IdGFArmonicos} tienen un IdGE distinto de 1, 2 o 3 (fases A, B y C).");
        }

        var puntosInvalidos = puntosBd
            .Where(punto => punto.DirCCEC is < 0 or > ushort.MaxValue
                || punto.DirMB is < 0 or > ushort.MaxValue)
            .ToList();
        if (puntosInvalidos.Count > 0)
        {
            _logger.LogError(
                "Hay puntos de armónicos con direcciones fuera del rango ushort de la DLL. IdMedidor={IdMedidor}, IdPuntos={IdPuntos}",
                idMedidor,
                string.Join(",", puntosInvalidos.Select(punto => punto.IdPunto)));
            throw new InvalidOperationException($"Uno o más puntos con IdGF = {IdGFArmonicos} tienen direcciones fuera del rango que admite la DLL.");
        }

        _logger.LogInformation(
            "Se solicitarán armónicos: IdMedidor={IdMedidor}, CantidadPuntos={CantidadPuntos}, Puntos={Puntos}",
            idMedidor,
            puntosBd.Count,
            string.Join(",", puntosBd.Select(punto => $"{punto.Tag}(IdGE={punto.IdGE})")));

        var puntosDll = puntosBd
            .Select(punto => (
                Fase: (ushort)punto.IdGE,
                Punto: new PuntoIODtoComs
                {
                    Tag = punto.Tag,
                    DirComsCEC = (ushort)punto.DirCCEC,
                    DirModbus = (ushort)punto.DirMB
                }))
            .ToList();

        var cronometro = Stopwatch.StartNew();
        var (conectado, valoresPorPunto) = await _comsClient.ConectarYSolicitarArmonicosAsync(medidorComs, puntosDll);
        cronometro.Stop();

        var puntosResultado = puntosBd
            .Select((punto, indice) => new ArmonicosPuntoResult
            {
                Fase = punto.IdGE,
                NombreFase = ArmonicosPuntoResult.LetraFase(punto.IdGE),
                Tipo = ArmonicosPuntoResult.TipoDesdeTag(punto.Tag),
                IdPunto = punto.IdPunto,
                Tag = punto.Tag,
                DirCCEC = punto.DirCCEC,
                DirModbus = punto.DirMB,
                Exitosa = valoresPorPunto[indice] is not null,
                Valores = valoresPorPunto[indice] ?? Array.Empty<float>()
            })
            .ToList();

        var puntosExitosos = puntosResultado.Count(punto => punto.Exitosa);
        var lecturaExitosa = conectado && puntosExitosos == puntosResultado.Count;

        _logger.LogInformation(
            "Terminó la solicitud de armónicos: IdMedidor={IdMedidor}, Conectado={Conectado}, PuntosExitosos={PuntosExitosos}/{CantidadPuntos}, DuracionMs={DuracionMs}",
            idMedidor,
            conectado,
            puntosExitosos,
            puntosResultado.Count,
            cronometro.ElapsedMilliseconds);

        var puntosFallidos = string.Join(", ", puntosResultado.Where(punto => !punto.Exitosa).Select(punto => punto.Tag));

        return new LecturaArmonicosResult
        {
            IdMedidor = idMedidor,
            IdGF = IdGFArmonicos,
            Conectado = conectado,
            LecturaExitosa = lecturaExitosa,
            Mensaje = !conectado
                ? "No se pudo conectar con el medidor."
                : lecturaExitosa
                    ? $"Se solicitaron los armónicos de {puntosResultado.Count} punto(s)."
                    : $"El medidor conectó, pero no se obtuvieron los armónicos de: {puntosFallidos}.",
            Puntos = puntosResultado
        };
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
