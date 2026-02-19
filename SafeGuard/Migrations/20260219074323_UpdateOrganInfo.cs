using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeGuard.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrganInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOrganDonor",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "OrganDetails",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganStatus",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganDetails",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganStatus",
                table: "Users");

            migrationBuilder.AddColumn<bool>(
                name: "IsOrganDonor",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
