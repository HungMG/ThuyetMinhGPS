using Microsoft.EntityFrameworkCore;

namespace TourGuideAdmin.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // 👇 Đổi dòng này thành DbSet<POI>
        public DbSet<POI> POIs { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. NẠP DỮ LIỆU TOUR MẪU (Bắt buộc phải có Id)
            modelBuilder.Entity<Tour>().HasData(
                new Tour { Id = 1, Name_VI = "Sài Gòn Xưa & Nay", Name_EN = "Saigon Then & Now", Name_ZH = "西贡今昔", Name_KO = "사이공의 과거와 현재", Name_JA = "サイゴンの昔と今" },
                new Tour { Id = 2, Name_VI = "Khám phá Văn Hóa", Name_EN = "Cultural Discovery", Name_ZH = "文化探索", Name_KO = "문화 탐험", Name_JA = "文化発見" }
            );

            // 2. NẠP DỮ LIỆU POI MẪU CỦA BẠN (Bắt buộc phải có Id)
            modelBuilder.Entity<POI>().HasData(
                // --- TOUR 1 ---
                new POI { Id = 1, TourId = 1, Name_VI = "Dinh Độc Lập", Name_EN = "Independence Palace", Name_ZH = "独立宫", Name_KO = "독립궁", Name_JA = "統一会堂", Latitude = 10.776889, Longitude = 106.695083, TriggerRadius = 100, Description_VI = "Nơi lưu giữ dấu ấn lịch sử hào hùng...", Priority = 1, IsFavorite = false, ImageUrl = "img_dinh_doc_lap.jpg" },
                new POI { Id = 2, TourId = 1, Name_VI = "Bưu điện Trung tâm Sài Gòn", Name_EN = "Saigon Central Post Office", Name_ZH = "西贡中心邮局", Name_KO = "사이공 중앙 우체국", Name_JA = "サイゴン中央郵便局", Latitude = 10.779833, Longitude = 106.700055, TriggerRadius = 50, Description_VI = "Công trình kiến trúc Pháp cổ điển...", Priority = 2, IsFavorite = true, ImageUrl = "img_buu_dien.jpg" },
                new POI { Id = 3, TourId = 1, Name_VI = "Nhà thờ Đức Bà", Name_EN = "Notre Dame Cathedral", Name_ZH = "圣母大教堂", Name_KO = "노트르담 대성당", Name_JA = "サイゴン大教会", Latitude = 10.779785, Longitude = 106.699017, TriggerRadius = 60, Priority = 3, Description_VI = "Biểu tượng tôn giáo và kiến trúc của Sài Gòn.", ImageUrl = "img_nha_tho.jpg" },

                // --- TOUR 2 ---
                new POI { Id = 4, TourId = 2, Name_VI = "Chợ Bến Thành", Name_EN = "Ben Thanh Market", Name_ZH = "边青市场", Name_KO = "벤탄 시장", Name_JA = "ベンタイン市場", Latitude = 10.7725, Longitude = 106.6980, TriggerRadius = 80, Priority = 4, Description_VI = "Khu chợ sầm uất và lâu đời nhất thành phố.", ImageUrl = "img_cho_ben_thanh.jpg" },
                new POI { Id = 5, TourId = 2, Name_VI = "Bảo tàng Chứng tích Chiến tranh", Name_EN = "War Remnants Museum", Name_ZH = "战争遗迹博物馆", Name_KO = "전쟁 잔존물 박물관", Name_JA = "戦争証跡博物館", Latitude = 10.7794, Longitude = 106.6922, TriggerRadius = 70, Priority = 5, Description_VI = "Nơi tái hiện những tàn khốc của chiến tranh.", ImageUrl = "img_bao_tang.jpg" },

                // --- ĐIỂM TỰ DO (Cho TourId = 1 tạm để tránh lỗi Khóa ngoại) ---
                new POI { Id = 6, TourId = 1, Name_VI = "Phố đi bộ Nguyễn Huệ", Name_EN = "Nguyen Hue Walking Street", Name_ZH = "阮惠步行街", Name_KO = "응우옌 후에 보행자 거리", Name_JA = "グエンフエ歩行者天国", Latitude = 10.7743, Longitude = 106.7032, TriggerRadius = 150, Priority = 5, Description_VI = "Khu phố đi bộ sầm uất ngay trung tâm thành phố.", ImageUrl = "img_nguyen_hue.jpg" },
                new POI { Id = 7, TourId = 1, Name_VI = "Tòa nhà Landmark 81", Name_EN = "Landmark 81 Skyscraper", Name_ZH = "地标塔 81", Name_KO = "랜드마크 81", Name_JA = "ランドマーク 81", Latitude = 10.7951, Longitude = 106.7218, TriggerRadius = 200, Priority = 6, Description_VI = "Tòa nhà cao nhất Việt Nam với kiến trúc hiện đại.", ImageUrl = "img_landmark.jpg" }
            );
        }
    }
}