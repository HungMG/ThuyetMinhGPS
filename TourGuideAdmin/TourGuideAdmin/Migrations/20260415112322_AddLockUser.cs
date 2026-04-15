using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourGuideAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddLockUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "POIs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 1,
                column: "OwnerId",
                value: 0);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 2,
                column: "OwnerId",
                value: 0);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 3,
                column: "OwnerId",
                value: 0);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 4,
                column: "OwnerId",
                value: 0);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 5,
                column: "OwnerId",
                value: 0);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 6,
                column: "OwnerId",
                value: 0);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 7,
                column: "OwnerId",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "POIs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 1,
                column: "OwnerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 2,
                column: "OwnerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 3,
                column: "OwnerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 4,
                column: "OwnerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 5,
                column: "OwnerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 6,
                column: "OwnerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 7,
                column: "OwnerId",
                value: null);
        }
    }
}
