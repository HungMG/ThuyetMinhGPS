using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourGuideAdmin.Migrations
{
    /// <inheritdoc />
    public partial class AddTourTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tours",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name_VI = table.Column<string>(type: "TEXT", nullable: true),
                    Name_EN = table.Column<string>(type: "TEXT", nullable: true),
                    Name_ZH = table.Column<string>(type: "TEXT", nullable: true),
                    Name_KO = table.Column<string>(type: "TEXT", nullable: true),
                    Name_JA = table.Column<string>(type: "TEXT", nullable: true),
                    Description_VI = table.Column<string>(type: "TEXT", nullable: true),
                    Description_EN = table.Column<string>(type: "TEXT", nullable: true),
                    Description_ZH = table.Column<string>(type: "TEXT", nullable: true),
                    Description_KO = table.Column<string>(type: "TEXT", nullable: true),
                    Description_JA = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tours");
        }
    }
}
