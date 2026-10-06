using Microsoft.EntityFrameworkCore;
using MedicionPQ.Modelos;

namespace MedicionPQ.Data;

/// <summary>
/// Contexto de Entity Framework Core para consultar y guardar datos en SQL Server.
/// Se registra mediante inyeccion de dependencias en <c>Program.cs</c> y lo usan los servicios.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Inicializa el contexto con las opciones configuradas para SQL Server.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// Conjunto de registros de usuarios asociado a la tabla <c>Usuario</c>.
    /// </summary>
    public DbSet<Usuario> Usuarios { get; set; }

    /// <summary>Conjunto de controladores RF asociado a la tabla <c>ControladorRF</c>.</summary>
    public DbSet<ControladorRF> ControladoresRF { get; set; }

    /// <summary>Conjunto de medidores QP asociado a la tabla <c>MedidorQP</c>.</summary>
    public DbSet<MedidorQP> MedidoresQP { get; set; }

    /// <summary>Conjunto de eventos configurables asociado a la tabla existente <c>EventosConfig</c>.</summary>
    public DbSet<EventosConfig> EventosConfig { get; set; }

    /// <summary>Conjunto de unidades asociado a la tabla existente <c>Unidades</c>.</summary>
    public DbSet<Unidad> Unidades { get; set; }

    /// <summary>Conjunto de registros asociado a la tabla <c>EventosReg</c>.</summary>
    public DbSet<EventosReg> EventosReg { get; set; }

    /// <summary>Conjunto de grupos eléctricos asociado a <c>IO_GrupoElectrico</c>.</summary>
    public DbSet<IO_GrupoElectrico> GruposElectricos { get; set; }

    /// <summary>Conjunto de grupos funcionales asociado a <c>IO_GrupoFuncional</c>.</summary>
    public DbSet<IO_GrupoFuncional> GruposFuncionales { get; set; }

    /// <summary>Conjunto de puntos de entrada/salida asociado a la tabla <c>PuntosIO</c>.</summary>
    public DbSet<PuntosIO> PuntosIO { get; set; }

    /// <summary>Configura la relación de muchos medidores hacia un único controlador RF.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // La tabla identifica cada registro combinando medidor, controlador, evento y fecha.
        modelBuilder.Entity<EventosReg>()
            .HasKey(registro => new { registro.idMedidor, registro.idCtrlRF, registro.idEvento, registro.fecha });

        modelBuilder.Entity<MedidorQP>()
            .HasOne<ControladorRF>()
            .WithMany(controlador => controlador.Medidores)
            .HasForeignKey(medidor => medidor.idCtrlRF)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
