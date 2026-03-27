using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJECT_TEAM1_PRN222.Migrations
{
    /// <inheritdoc />
    public partial class PaymentReceiverConfig_QrImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrImageRelativePath",
                table: "PaymentReceiverConfigs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrImageRelativePath",
                table: "PaymentReceiverConfigs");
        }
    }
}
