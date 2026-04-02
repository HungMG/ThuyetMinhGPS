using SQLite;
using TourGuideApp.Models;

namespace TourGuideApp.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;

        // 1. Khởi tạo kết nối và tạo bảng
        public async Task InitAsync()
        {
            if (_db != null) return;

            // Đổi hẳn lên v10 để ép nó quên sạch tình cũ!
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TourGuide_v12.db");

            _db = new SQLiteAsyncConnection(databasePath);

            // 🌟 QUAN TRỌNG NHẤT LÀ 2 DÒNG NÀY 🌟
            await _db.CreateTableAsync<POI>();
            await _db.CreateTableAsync<Tour>(); // <-- Thiếu dòng này là dính lỗi savePoint ngay!
        }

        // 2. Lấy toàn bộ điểm đến
        public async Task<List<POI>> GetAllPOIsAsync()
        {
            await InitAsync();
            return await _db.Table<POI>().ToListAsync();
        }

        // 3. Cập nhật thời gian đọc thuyết minh
        public async Task UpdateLastPlayedTimeAsync(int poiId)
        {
            await InitAsync();
            var poi = await _db.Table<POI>().Where(p => p.Id == poiId).FirstOrDefaultAsync();
            if (poi != null)
            {
                poi.LastPlayedTime = DateTime.Now;
                await _db.UpdateAsync(poi);
            }
        }

        // 4. Thêm điểm POI mới (Khi người dùng chạm vào bản đồ)
        public async Task AddPOIAsync(POI newPoi)
        {
            await InitAsync();

            // 👇 ĐÃ SỬA: Lấp đầy dữ liệu cho cả 5 ngôn ngữ nếu người dùng chỉ nhập tiếng Việt
            if (string.IsNullOrEmpty(newPoi.Name_EN)) newPoi.Name_EN = newPoi.Name_VI;
            if (string.IsNullOrEmpty(newPoi.Name_ZH)) newPoi.Name_ZH = newPoi.Name_VI;
            if (string.IsNullOrEmpty(newPoi.Name_KO)) newPoi.Name_KO = newPoi.Name_VI;
            if (string.IsNullOrEmpty(newPoi.Name_JA)) newPoi.Name_JA = newPoi.Name_VI;

            if (string.IsNullOrEmpty(newPoi.Description_EN)) newPoi.Description_EN = newPoi.Description_VI;
            if (string.IsNullOrEmpty(newPoi.Description_ZH)) newPoi.Description_ZH = newPoi.Description_VI;
            if (string.IsNullOrEmpty(newPoi.Description_KO)) newPoi.Description_KO = newPoi.Description_VI;
            if (string.IsNullOrEmpty(newPoi.Description_JA)) newPoi.Description_JA = newPoi.Description_VI;

            if (newPoi.TriggerRadius == 0) newPoi.TriggerRadius = 50;
            if (newPoi.Priority == 0) newPoi.Priority = 99;

            await _db.InsertAsync(newPoi);
        }

        // 5. Lấy danh sách Yêu thích
        public async Task<List<POI>> GetFavoritePOIsAsync()
        {
            await InitAsync();
            return await _db.Table<POI>().Where(p => p.IsFavorite).ToListAsync();
        }

        // 6. Cập nhật trạng thái Thả tim / Bỏ tim
        public async Task<int> UpdatePOIAsync(POI poi)
        {
            await InitAsync();
            return await _db.UpdateAsync(poi);
        }

        // Lấy toàn bộ danh sách Lộ trình (Tours)
        public async Task<List<Tour>> GetAllToursAsync()
        {
            await InitAsync();
            return await _db.Table<Tour>().ToListAsync();
        }

        // 7. NẠP DỮ LIỆU MẪU (BAO GỒM TOUR VÀ POI ĐA NGÔN NGỮ)
        public async Task SeedDataAsync()
        {
            await InitAsync();

            // 1. NẠP DỮ LIỆU TOUR (Nếu chưa có)
            var tourCount = await _db.Table<Tour>().CountAsync();
            if (tourCount == 0)
            {
                var tours = new List<Tour>
                {
                    new Tour {
                        Name_VI = "Sài Gòn Xưa & Nay", Name_EN = "Saigon Then & Now",
                        Name_ZH = "西贡今昔", Name_KO = "사이공의 과거와 현재", Name_JA = "サイゴンの昔と今",
                        EstimatedTime = "3 Giờ"
                    },
                    new Tour {
                        Name_VI = "Khám phá Văn Hóa", Name_EN = "Cultural Discovery",
                        Name_ZH = "文化探索", Name_KO = "문화 탐험", Name_JA = "文化発見",
                        EstimatedTime = "2 Giờ"
                    }
                };
                await _db.InsertAllAsync(tours);
            }

            // 2. NẠP DỮ LIỆU ĐỊA ĐIỂM - POI (Nếu chưa có)
            var poiCount = await _db.Table<POI>().CountAsync();
            if (poiCount == 0)
            {
                var sampleData = new List<POI>
                {
                    // --- 3 ĐIỂM THUỘC TOUR 1 ---
                    new POI
                    {
                        TourId = 1, // 👈 Đánh dấu thuộc Tour 1
                        Name_VI = "Dinh Độc Lập",
                        Name_EN = "Independence Palace",
                        Name_ZH = "独立宫",
                        Name_KO = "독립궁",
                        Name_JA = "統一会堂",
                        Latitude = 10.776889,
                        Longitude = 106.695083,
                        TriggerRadius = 100,
                        Description_VI = "Nơi lưu giữ dấu ấn lịch sử hào hùng của dân tộc Việt Nam.",
                        Description_EN = "A historic landmark representing the victory and peace of Vietnam.",
                        Description_ZH = "越南民族英雄历史印记的保存地。",
                        Description_KO = "베트남 민족의 영웅적인 역사적 자취를 간직한 곳.",
                        Description_JA = "ベトナム民族の英雄的な歴史の足跡を保存する場所。",
                        Priority = 1,
                        IsFavorite = false,
                        ImageUrl = "img_dinh_doc_lap.jpg"
                    },
                    new POI
                    {
                        TourId = 1, // 👈 Đánh dấu thuộc Tour 1
                        Name_VI = "Bưu điện Trung tâm Sài Gòn",
                        Name_EN = "Saigon Central Post Office",
                        Name_ZH = "西贡中心邮局",
                        Name_KO = "사이공 중앙 우체국",
                        Name_JA = "サイゴン中央郵便局",
                        Latitude = 10.779833,
                        Longitude = 106.700055,
                        TriggerRadius = 50,
                        Description_VI = "Công trình kiến trúc Pháp cổ điển tuyệt đẹp giữa lòng thành phố.",
                        Description_EN = "A stunning example of French colonial architecture in the heart of the city.",
                        Description_ZH = "位于市中心的绝美经典法式建筑。",
                        Description_KO = "도심 한가운데 있는 아름다운 고전 프랑스 건축물.",
                        Description_JA = "市の中心部にある美しい古典的なフランス建築。",
                        Priority = 2,
                        IsFavorite = true,
                        ImageUrl = "img_buu_dien.jpg"
                    },
                    new POI
                    {
                        TourId = 1, // 👈 Đánh dấu thuộc Tour 1
                        Name_VI = "Nhà thờ Đức Bà",
                        Name_EN = "Notre Dame Cathedral",
                        Name_ZH = "圣母大教堂",
                        Name_KO = "노트르담 대성당",
                        Name_JA = "サイゴン大教会",
                        Latitude = 10.779785,
                        Longitude = 106.699017,
                        TriggerRadius = 60,
                        Priority = 3,
                        Description_VI = "Biểu tượng tôn giáo và kiến trúc của Sài Gòn.",
                        Description_EN = "Religious and architectural symbol of Saigon.",
                        Description_ZH = "西贡的宗教和建筑标志。",
                        Description_KO = "사이공의 종교 및 건축적 상징.",
                        Description_JA = "サイゴンの宗教的および建築的象徴。",
                        ImageUrl = "img_nha_tho.jpg"
                    },

                    // --- 2 ĐIỂM THUỘC TOUR 2 ---
                    new POI
                    {
                        TourId = 2, // 👈 Đánh dấu thuộc Tour 2
                        Name_VI = "Chợ Bến Thành",
                        Name_EN = "Ben Thanh Market",
                        Name_ZH = "边青市场",
                        Name_KO = "벤탄 시장",
                        Name_JA = "ベンタイン市場",
                        Latitude = 10.7725,
                        Longitude = 106.6980,
                        TriggerRadius = 80,
                        Priority = 4,
                        Description_VI = "Khu chợ sầm uất và lâu đời nhất thành phố.",
                        Description_EN = "The most bustling and oldest market.",
                        Description_ZH = "全市最繁华、最古老的市场。",
                        Description_KO = "도시에서 가장 붐비고 오래된 시장.",
                        Description_JA = "市内で最も賑やかで古い市場。",
                        ImageUrl = "img_cho_ben_thanh.jpg"
                    },
                    new POI
                    {
                        TourId = 2, // 👈 Đánh dấu thuộc Tour 2
                        Name_VI = "Bảo tàng Chứng tích Chiến tranh",
                        Name_EN = "War Remnants Museum",
                        Name_ZH = "战争遗迹博物馆",
                        Name_KO = "전쟁 잔존물 박물관",
                        Name_JA = "戦争証跡博物館",
                        Latitude = 10.7794,
                        Longitude = 106.6922,
                        TriggerRadius = 70,
                        Priority = 5,
                        Description_VI = "Nơi tái hiện những tàn khốc của chiến tranh.",
                        Description_EN = "Exhibits relating to the Vietnam War.",
                        Description_ZH = "重现战争残酷的地方。",
                        Description_KO = "전쟁의 잔혹함을 재현한 곳.",
                        Description_JA = "戦争の残酷さを再現する場所。",
                        ImageUrl = "img_bao_tang.jpg"
                    },
               // 👇 ĐÃ BỔ SUNG ĐẦY ĐỦ 5 NGÔN NGỮ KHÔNG THIẾU 1 CHỮ 👇
                    new POI
                    {
                        TourId = 0,
                        Name_VI = "Phố đi bộ Nguyễn Huệ",
                        Name_EN = "Nguyen Hue Walking Street",
                        Name_ZH = "阮惠步行街",
                        Name_KO = "응우옌 후에 보행자 거리",
                        Name_JA = "グエンフエ歩行者天国",

                        Latitude = 10.7743,
                        Longitude = 106.7032,
                        TriggerRadius = 150,
                        Priority = 5,

                        Description_VI = "Khu phố đi bộ sầm uất ngay trung tâm thành phố.",
                        Description_EN = "Bustling walking street in the heart of the city.",
                        Description_ZH = "位于市中心的繁华步行街。",
                        Description_KO = "도심 한가운데 있는 번화한 보행자 거리.",
                        Description_JA = "市の中心部にある賑やかな歩行者天国。",

                        ImageUrl = "img_nguyen_hue.jpg"
                    },
                    new POI
                    {
                        TourId = 0,
                        Name_VI = "Tòa nhà Landmark 81",
                        Name_EN = "Landmark 81 Skyscraper",
                        Name_ZH = "地标塔 81",
                        Name_KO = "랜드마크 81",
                        Name_JA = "ランドマーク 81",

                        Latitude = 10.7951,
                        Longitude = 106.7218,
                        TriggerRadius = 200,
                        Priority = 6,

                        Description_VI = "Tòa nhà cao nhất Việt Nam với kiến trúc hiện đại.",
                        Description_EN = "The tallest building in Vietnam with modern architecture.",
                        Description_ZH = "越南最高建筑，拥有现代建筑风格。",
                        Description_KO = "현대적인 건축미를 자랑하는 베트남 최고층 빌딩.",
                        Description_JA = "現代的な建築様式を持つベトナムで最も高いビル。",

                        ImageUrl = "img_landmark.jpg"
                    }
                };
                await _db.InsertAllAsync(sampleData);
            }
        }
    }
}