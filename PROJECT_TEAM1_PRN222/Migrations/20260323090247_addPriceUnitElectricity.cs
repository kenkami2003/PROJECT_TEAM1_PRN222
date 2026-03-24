using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJECT_TEAM1_PRN222.Migrations
{
    /// <inheritdoc />
    public partial class addPriceUnitElectricity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceUnitElectricity",
                table: "UtilityReadings");

            migrationBuilder.DropColumn(
                name: "PriceUnitWater",
                table: "UtilityReadings");

            migrationBuilder.AddColumn<double>(
                name: "PriceUnitElectricity",
                table: "Properties",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PriceUnitWater",
                table: "Properties",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceUnitElectricity",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PriceUnitWater",
                table: "Properties");

            migrationBuilder.AddColumn<double>(
                name: "PriceUnitElectricity",
                table: "UtilityReadings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PriceUnitWater",
                table: "UtilityReadings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
