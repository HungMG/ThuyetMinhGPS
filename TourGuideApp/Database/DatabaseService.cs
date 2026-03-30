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

            // 👇 ĐỔI THÀNH v6: Ép app tạo Database mới sạch 100%, tự động hết lỗi InvalidCastException!
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TourGuide_v6.db");

            _db = new SQLiteAsyncConnection(databasePath);
            await _db.CreateTableAsync<POI>();
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
        public async Task UpdatePOIAsync(POI poi)
        {
            await InitAsync();
            await _db.UpdateAsync(poi);
        }

        // 7. NẠP DỮ LIỆU MẪU (5 NGÔN NGỮ: VI, EN, ZH, KO, JA)
        public async Task SeedDataAsync()
        {
            await InitAsync();

            var count = await _db.Table<POI>().CountAsync();

            if (count == 0)
            {
                var sampleData = new List<POI>
                {
                    new POI
                    {
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
                    }
                };

                await _db.InsertAllAsync(sampleData);
                System.Diagnostics.Debug.WriteLine("Đã nạp dữ liệu 5 ngôn ngữ thành công!");
            }
        }
    }
}