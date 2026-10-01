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
}
