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
            await _db.CreateTableAsync<Tour>();
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
    }
}