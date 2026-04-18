using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourGuideAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioFile_EN",
                table: "POIs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFile_JA",
                table: "POIs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFile_KO",
                table: "POIs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFile_VI",
                table: "POIs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AudioFile_ZH",
                table: "POIs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AudioType_EN",
                table: "POIs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AudioType_JA",
                table: "POIs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AudioType_KO",
                table: "POIs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AudioType_VI",
                table: "POIs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AudioType_ZH",
                table: "POIs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AudioFile_EN", "AudioFile_JA", "AudioFile_KO", "AudioFile_VI", "AudioFile_ZH", "AudioType_EN", "AudioType_JA", "AudioType_KO", "AudioType_VI", "AudioType_ZH" },
                values: new object[] { null, null, null, null, null, 0, 0, 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AudioFile_EN", "AudioFile_JA", "AudioFile_KO", "AudioFile_VI", "AudioFile_ZH", "AudioType_EN", "AudioType_JA", "AudioType_KO", "AudioType_VI", "AudioType_ZH" },
                values: new object[] { null, null, null, null, null, 0, 0, 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AudioFile_EN", "AudioFile_JA", "AudioFile_KO", "AudioFile_VI", "AudioFile_ZH", "AudioType_EN", "AudioType_JA", "AudioType_KO", "AudioType_VI", "AudioType_ZH" },
                values: new object[] { null, null, null, null, null, 0, 0, 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "AudioFile_EN", "AudioFile_JA", "AudioFile_KO", "AudioFile_VI", "AudioFile_ZH", "AudioType_EN", "AudioType_JA", "AudioType_KO", "AudioType_VI", "AudioType_ZH" },
                values: new object[] { null, null, null, null, null, 0, 0, 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "AudioFile_EN", "AudioFile_JA", "AudioFile_KO", "AudioFile_VI", "AudioFile_ZH", "AudioType_EN", "AudioType_JA", "AudioType_KO", "AudioType_VI", "AudioType_ZH" },
                values: new object[] { null, null, null, null, null, 0, 0, 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "AudioFile_EN", "AudioFile_JA", "AudioFile_KO", "AudioFile_VI", "AudioFile_ZH", "AudioType_EN", "AudioType_JA", "AudioType_KO", "AudioType_VI", "AudioType_ZH" },
                values: new object[] { null, null, null, null, null, 0, 0, 0, 0, 0 });

            migrationBuilder.UpdateData(
                table: "POIs",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "AudioFile_EN", "AudioFile_JA", "AudioFile_KO", "AudioFile_VI", "AudioFile_ZH", "AudioType_EN", "AudioType_JA", "AudioType_KO", "AudioType_VI", "AudioType_ZH" },
                values: new object[] { null, null, null, null, null, 0, 0, 0, 0, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioFile_EN",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioFile_JA",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioFile_KO",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioFile_VI",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioFile_ZH",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioType_EN",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioType_JA",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioType_KO",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioType_VI",
                table: "POIs");

            migrationBuilder.DropColumn(
                name: "AudioType_ZH",
                table: "POIs");
        }
    }
}
