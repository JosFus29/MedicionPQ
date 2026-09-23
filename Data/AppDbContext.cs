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
}
