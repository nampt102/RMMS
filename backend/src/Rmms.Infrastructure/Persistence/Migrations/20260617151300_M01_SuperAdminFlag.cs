using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rmms.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class M01_SuperAdminFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_super_admin",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_super_admin",
                table: "users");
        }
    }
}
