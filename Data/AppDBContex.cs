using Microsoft.EntityFrameworkCore;
using MedicionPQ.Modelos;

namespace MedicionPQ.Data;

public class AppDBContex : DbContext
{
    public AppDBContex(DbContextOptions<AppDBContex> options) : base(options){}
    public DbSet<Usuario> Usuarios { get; set; }
}
