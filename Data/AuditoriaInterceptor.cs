using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MedicionPQ.Data;

/// <summary>
/// Registra en el log qué datos cambian en la base de datos y quién los cambió.
/// Se ejecuta automáticamente cada vez que un servicio llama a <c>SaveChangesAsync()</c>,
/// así que cubre todos los controladores sin tener que modificar cada uno.
/// Ejemplos de lo que escribe:
/// <code>
/// admin@test.com modificó Vars5M (idVar5M=14): edo true -> false, fases 1 -> 2
/// admin@test.com creó Unidades (idUnidad=11): nombre="Watt", simbolo="W"
/// admin@test.com eliminó EventosConfig (idEvento=3)
/// </code>
/// Las contraseñas nunca se escriben: solo se indica que cambiaron.
/// </summary>
public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditoriaInterceptor> _logger;

    /// <summary>Modificaciones y eliminaciones detectadas antes de guardar, listas para escribirse.</summary>
    private readonly List<string> _cambiosPendientes = new();

    /// <summary>Registros nuevos; se describen después de guardar para conocer el id que asignó SQL Server.</summary>
    private readonly List<(string Usuario, EntityEntry Entrada)> _altasPendientes = new();

    /// <summary>Recibe el acceso a la solicitud actual (para saber quién hace el cambio) y el logger.</summary>
    /// <param name="httpContextAccessor">Da acceso al usuario del token JWT de la solicitud actual.</param>
    /// <param name="logger">Logger configurado con Serilog.</param>
    public AuditoriaInterceptor(IHttpContextAccessor httpContextAccessor, ILogger<AuditoriaInterceptor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>Antes de guardar (versión asíncrona): revisa qué entidades cambiaron.</summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        PrepararAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>Antes de guardar (versión síncrona): revisa qué entidades cambiaron.</summary>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        PrepararAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>Después de guardar con éxito (versión asíncrona): escribe los cambios en el log.</summary>
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        EscribirAuditoria();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>Después de guardar con éxito (versión síncrona): escribe los cambios en el log.</summary>
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        EscribirAuditoria();
        return base.SavedChanges(eventData, result);
    }

    /// <summary>Si el guardado falla, descarta los cambios pendientes porque no llegaron a la base de datos.</summary>
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Limpiar();
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    /// <summary>Si el guardado falla, descarta los cambios pendientes porque no llegaron a la base de datos.</summary>
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Limpiar();
        base.SaveChangesFailed(eventData);
    }

    /// <summary>
    /// Recorre las entidades que EF Core tiene en seguimiento y anota las que se crearon,
    /// modificaron o eliminaron. En las modificaciones guarda el valor anterior y el nuevo.
    /// </summary>
    private void PrepararAuditoria(DbContext? context)
    {
        if (context is null) return;

        // Asegura que EF Core ya comparó los valores actuales con los originales.
        context.ChangeTracker.DetectChanges();
        var usuario = ObtenerUsuario();

        foreach (var entrada in context.ChangeTracker.Entries())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    _altasPendientes.Add((usuario, entrada));
                    break;

                case EntityState.Modified:
                    var cambios = entrada.Properties
                        .Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue))
                        .Select(p => EsSensible(p.Metadata.Name)
                            ? $"{p.Metadata.Name} (modificada)"
                            : $"{p.Metadata.Name} {Formatear(p.OriginalValue)} -> {Formatear(p.CurrentValue)}")
                        .ToList();

                    // Si se guardó sin que ningún valor cambiara realmente, no hay nada que registrar.
                    if (cambios.Count > 0)
                        _cambiosPendientes.Add($"{usuario} modificó {NombreTabla(entrada)} ({Llave(entrada)}): {string.Join(", ", cambios)}");
                    break;

                case EntityState.Deleted:
                    _cambiosPendientes.Add($"{usuario} eliminó {NombreTabla(entrada)} ({Llave(entrada)})");
                    break;
            }
        }
    }

    /// <summary>Escribe en el log todos los cambios anotados y vacía las listas.</summary>
    private void EscribirAuditoria()
    {
        foreach (var (usuario, entrada) in _altasPendientes)
        {
            var llaves = entrada.Metadata.FindPrimaryKey()?.Properties.Select(p => p.Name).ToHashSet() ?? new HashSet<string>();
            var valores = entrada.Properties
                .Where(p => !llaves.Contains(p.Metadata.Name))
                .Select(p => $"{p.Metadata.Name}={(EsSensible(p.Metadata.Name) ? "(oculta)" : Formatear(p.CurrentValue))}");

            _cambiosPendientes.Add($"{usuario} creó {NombreTabla(entrada)} ({Llave(entrada)}): {string.Join(", ", valores)}");
        }

        foreach (var cambio in _cambiosPendientes)
            _logger.LogInformation("{Auditoria}", cambio);

        Limpiar();
    }

    /// <summary>Vacía las listas de cambios pendientes.</summary>
    private void Limpiar()
    {
        _cambiosPendientes.Clear();
        _altasPendientes.Clear();
    }

    /// <summary>
    /// Correo del usuario del token JWT; "anonimo" si la solicitud no trae token
    /// (por ejemplo, el restablecimiento de contraseña) y "sistema" si el cambio no viene de una solicitud HTTP.
    /// </summary>
    private string ObtenerUsuario()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return "sistema";

        return httpContext.User.FindFirst(ClaimTypes.Email)?.Value ?? "anonimo";
    }

    /// <summary>Nombre de la tabla en SQL Server (o de la clase, si no tiene tabla asignada).</summary>
    private static string NombreTabla(EntityEntry entrada)
        => entrada.Metadata.GetTableName() ?? entrada.Metadata.ClrType.Name;

    /// <summary>Llave primaria del registro, por ejemplo "idVar5M=14" o varias separadas por coma si es compuesta.</summary>
    private static string Llave(EntityEntry entrada)
    {
        var llave = entrada.Metadata.FindPrimaryKey();
        if (llave is null) return "sin llave";

        return string.Join(", ", llave.Properties.Select(p => $"{p.Name}={Formatear(entrada.Property(p.Name).CurrentValue)}"));
    }

    /// <summary>Indica si una columna guarda una contraseña, para no escribir su valor en el log.</summary>
    private static bool EsSensible(string nombrePropiedad)
        => nombrePropiedad.Contains("contrasena", StringComparison.OrdinalIgnoreCase)
        || nombrePropiedad.Contains("password", StringComparison.OrdinalIgnoreCase);

    /// <summary>Convierte un valor a texto legible: textos entre comillas, booleanos en minúscula y fechas con formato fijo.</summary>
    private static string Formatear(object? valor) => valor switch
    {
        null => "null",
        string texto => $"\"{texto}\"",
        bool booleano => booleano ? "true" : "false",
        DateTime fecha => fecha.ToString("yyyy-MM-dd HH:mm:ss"),
        IFormattable formateable => formateable.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString() ?? string.Empty
    };
}
