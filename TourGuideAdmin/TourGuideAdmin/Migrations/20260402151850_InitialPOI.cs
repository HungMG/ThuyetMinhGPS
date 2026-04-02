using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourGuideAdmin.Migrations
{
    /// <inheritdoc />
    public partial class InitialPOI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "POIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TourId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name_VI = table.Column<string>(type: "TEXT", nullable: true),
                    Name_EN = table.Column<string>(type: "TEXT", nullable: true),
                    Name_ZH = table.Column<string>(type: "TEXT", nullable: true),
                    Name_KO = table.Column<string>(type: "TEXT", nullable: true),
                    Name_JA = table.Column<string>(type: "TEXT", nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    TriggerRadius = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Description_VI = table.Column<string>(type: "TEXT", nullable: true),
                    Description_EN = table.Column<string>(type: "TEXT", nullable: true),
                    Description_ZH = table.Column<string>(type: "TEXT", nullable: true),
                    Description_KO = table.Column<string>(type: "TEXT", nullable: true),
                    Description_JA = table.Column<string>(type: "TEXT", nullable: true),
                    IsFavorite = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LastPlayedTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POIs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "POIs");
        }
    }
}
