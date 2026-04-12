using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TourGuideAdmin.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                    LastPlayedTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ApprovalStatus = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POIs", x => x.Id);
                });

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
                    EstimatedTime = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tours", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "POIs",
                columns: new[] { "Id", "ApprovalStatus", "Description_EN", "Description_JA", "Description_KO", "Description_VI", "Description_ZH", "ImageUrl", "IsFavorite", "LastPlayedTime", "Latitude", "Longitude", "Name_EN", "Name_JA", "Name_KO", "Name_VI", "Name_ZH", "OwnerId", "Priority", "TourId", "TriggerRadius" },
                values: new object[,]
                {
                    { 1, 0, null, null, null, "Nơi lưu giữ dấu ấn lịch sử hào hùng...", null, "img_dinh_doc_lap.jpg", false, null, 10.776889000000001, 106.695083, "Independence Palace", "統一会堂", "독립궁", "Dinh Độc Lập", "独立宫", null, 1, 1, 100 },
                    { 2, 0, null, null, null, "Công trình kiến trúc Pháp cổ điển...", null, "img_buu_dien.jpg", true, null, 10.779833, 106.70005500000001, "Saigon Central Post Office", "サイゴン中央郵便局", "사이공 중앙 우체국", "Bưu điện Trung tâm Sài Gòn", "西贡中心邮局", null, 2, 1, 50 },
                    { 3, 0, null, null, null, "Biểu tượng tôn giáo và kiến trúc của Sài Gòn.", null, "img_nha_tho.jpg", false, null, 10.779785, 106.699017, "Notre Dame Cathedral", "サイゴン大教会", "노트르담 대성당", "Nhà thờ Đức Bà", "圣母大教堂", null, 3, 1, 60 },
                    { 4, 0, null, null, null, "Khu chợ sầm uất và lâu đời nhất thành phố.", null, "img_cho_ben_thanh.jpg", false, null, 10.772500000000001, 106.69799999999999, "Ben Thanh Market", "ベンタイン市場", "벤탄 시장", "Chợ Bến Thành", "边青市场", null, 4, 2, 80 },
                    { 5, 0, null, null, null, "Nơi tái hiện những tàn khốc của chiến tranh.", null, "img_bao_tang.jpg", false, null, 10.779400000000001, 106.6922, "War Remnants Museum", "戦争証跡博物館", "전쟁 잔존물 박물관", "Bảo tàng Chứng tích Chiến tranh", "战争遗迹博物馆", null, 5, 2, 70 },
                    { 6, 0, null, null, null, "Khu phố đi bộ sầm uất ngay trung tâm thành phố.", null, "img_nguyen_hue.jpg", false, null, 10.7743, 106.7032, "Nguyen Hue Walking Street", "グエンフエ歩行者天国", "응우옌 후에 보행자 거리", "Phố đi bộ Nguyễn Huệ", "阮惠步行街", null, 5, 1, 150 },
                    { 7, 0, null, null, null, "Tòa nhà cao nhất Việt Nam với kiến trúc hiện đại.", null, "img_landmark.jpg", false, null, 10.7951, 106.7218, "Landmark 81 Skyscraper", "ランドマーク 81", "랜드마크 81", "Tòa nhà Landmark 81", "地标塔 81", null, 6, 1, 200 }
                });

            migrationBuilder.InsertData(
                table: "Tours",
                columns: new[] { "Id", "Description_EN", "Description_JA", "Description_KO", "Description_VI", "Description_ZH", "EstimatedTime", "ImageUrl", "Name_EN", "Name_JA", "Name_KO", "Name_VI", "Name_ZH" },
                values: new object[,]
                {
                    { 1, null, null, null, null, null, null, null, "Saigon Then & Now", "サイゴンの昔と今", "사이공의 과거와 현재", "Sài Gòn Xưa & Nay", "西贡今昔" },
                    { 2, null, null, null, null, null, null, null, "Cultural Discovery", "文化発見", "문화 탐험", "Khám phá Văn Hóa", "文化探索" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "POIs");

            migrationBuilder.DropTable(
                name: "Tours");
        }
    }
}
