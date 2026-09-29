using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedicionPQ.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProcedimientoMedidores : Migration
    {
        /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE PROCEDURE sp_ObtenerMedidores
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT idMedidor, nombre, ubicacion, tipo, edo
                FROM Medidor;
            END
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP PROCEDURE sp_ObtenerMedidores");
    }
      
    }
}
