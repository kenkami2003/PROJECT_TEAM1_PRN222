using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJECT_TEAM1_PRN222.Migrations
{
    /// <inheritdoc />
    public partial class ServiceFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ServiceFee",
                table: "Properties",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceFee",
                table: "Properties");
        }
    }
}
