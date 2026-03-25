using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PROJECT_TEAM1_PRN222.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporaryGuestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityCardBack",
                table: "TemporaryGuests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdentityCardFront",
                table: "TemporaryGuests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "TemporaryGuests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProof",
                table: "TemporaryGuests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "TemporaryGuests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityCardBack",
                table: "TemporaryGuests");

            migrationBuilder.DropColumn(
                name: "IdentityCardFront",
                table: "TemporaryGuests");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "TemporaryGuests");

            migrationBuilder.DropColumn(
                name: "PaymentProof",
                table: "TemporaryGuests");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "TemporaryGuests");
        }
    }
}
