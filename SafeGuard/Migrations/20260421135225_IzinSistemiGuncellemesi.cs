using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SafeGuard.Migrations
{
    /// <inheritdoc />
    public partial class IzinSistemiGuncellemesi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CanViewMedicalId",
                table: "Helpers",
                newName: "UserAllowsHelperToView");

            migrationBuilder.AddColumn<bool>(
                name: "HelperAllowsUserToView",
                table: "Helpers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HelperAllowsUserToView",
                table: "Helpers");

            migrationBuilder.RenameColumn(
                name: "UserAllowsHelperToView",
                table: "Helpers",
                newName: "CanViewMedicalId");
        }
    }
}
