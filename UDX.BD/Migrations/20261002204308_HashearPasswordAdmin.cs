using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UDX.BD.Migrations
{
    /// <inheritdoc />
    public partial class HashearPasswordAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hash BCrypt de la contraseña del admin insertado en SeedCatalogoMaterias.
            // Solo se actualiza si todavía está en texto plano.
            migrationBuilder.Sql(
                "UPDATE Usuarios " +
                "SET Password = '$2a$11$ZBVqYchUgN1npEmcolqUieKcWObWUkscriy8L1wFdUva8YDnHLdKa' " +
                "WHERE Email = 'admin@udx.com' AND Password = 'Admin123*';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE Usuarios " +
                "SET Password = 'Admin123*' " +
                "WHERE Email = 'admin@udx.com' " +
                "AND Password = '$2a$11$ZBVqYchUgN1npEmcolqUieKcWObWUkscriy8L1wFdUva8YDnHLdKa';");
        }
    }
}
