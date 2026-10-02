using Microsoft.EntityFrameworkCore;
using MedicionPQ.Modelos;

namespace MedicionPQ.Data;

/// <summary>
/// Application database context.
/// This class represents the EF Core DbContext used across the project.
/// Renamed from AppDBContex to AppDbContext to follow .NET naming conventions
/// and avoid confusion. References to this type appear in:
/// - Program.cs: AddDbContext<MedicionPQ.Data.AppDbContext>
/// - Services/AuthService.cs: constructor injection and field _context
/// - Any other service or controller that injects the application's DbContext
/// 
/// If you rename this class again, update all references accordingly.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// DbSet for users (tabla Usuarios).
    /// </summary>
    public DbSet<Usuario> Usuarios { get; set; }

    /// <summary>
    /// DbSet para los medidores (tabla Medidor).
    /// </summary>
    public DbSet<Medidor> Medidores { get; set; }

    // --- Vistas de Medicion5M (solo lectura) ---
    public DbSet<Medicion5M> VistaMedicion5M => Set<Medicion5M>();
    public DbSet<Medicion5MPromedioHora> VistaPromedioHora => Set<Medicion5MPromedioHora>();
    public DbSet<Medicion5MPromedioDia> VistaPromedioDia => Set<Medicion5MPromedioDia>();
    public DbSet<Medicion5MPromedioSemana> VistaPromedioSemana => Set<Medicion5MPromedioSemana>();
    public DbSet<Medicion5MPromedioMes> VistaPromedioMes => Set<Medicion5MPromedioMes>();
    public DbSet<Medicion5MPromedioAnio> VistaPromedioAnio => Set<Medicion5MPromedioAnio>();

    /// <summary>
    /// Mapea las entidades Keyless a sus vistas correspondientes en SQL Server.
    /// Estas vistas ya existen en la base de datos (creadas por script aparte,
    /// no por migración de EF Core).
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Medicion5M>().ToView("vw_Medicion5M");
        modelBuilder.Entity<Medicion5MPromedioHora>().ToView("vw_Medicion5M_PromedioHora");
        modelBuilder.Entity<Medicion5MPromedioDia>().ToView("vw_Medicion5M_PromedioDia");
        modelBuilder.Entity<Medicion5MPromedioSemana>().ToView("vw_Medicion5M_PromedioSemana");
        modelBuilder.Entity<Medicion5MPromedioMes>().ToView("vw_Medicion5M_PromedioMes");
        modelBuilder.Entity<Medicion5MPromedioAnio>().ToView("vw_Medicion5M_PromedioAnio");
    }
}
