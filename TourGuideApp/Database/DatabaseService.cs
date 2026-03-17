using SQLite;
using TourGuideApp.Models;

namespace TourGuideApp.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;

        // Hàm khởi tạo kết nối
        public async Task InitAsync()
        {
            if (_db != null) return;

            // Chỉ cần thêm chữ _v2 vào đây để app tạo một cái DB mới toanh
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TourGuide_v3.db");

            _db = new SQLiteAsyncConnection(databasePath);
            await _db.CreateTableAsync<POI>();
        }

        // Hàm lấy toàn bộ điểm đến
        public async Task<List<POI>> GetAllPOIsAsync()
        {
            await InitAsync();
            return await _db.Table<POI>().ToListAsync();
        }


        public async Task UpdateLastPlayedTimeAsync(int poiId)
        {
            await InitAsync();
            var poi = await _db.Table<POI>().Where(p => p.Id == poiId).FirstOrDefaultAsync();
            if (poi != null)
            {
                // Thay vì IsVisited = true, mình lưu thời gian hiện tại
                poi.LastPlayedTime = DateTime.Now;
                await _db.UpdateAsync(poi);
            }
        }
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
                Name = "Dinh Độc Lập",
                Latitude = 10.776889,
                Longitude = 106.695083,
                TriggerRadius = 100,
                Description = "Nơi lưu giữ dấu ấn lịch sử hào hùng.",
                Priority = 1,          // Thêm độ ưu tiên
                LastPlayedTime = null  // Thay cho IsVisited = false (null nghĩa là chưa phát bao giờ)
            },
            new POI
            {
                Name = "Bưu điện Trung tâm Sài Gòn",
                Latitude = 10.779833,
                Longitude = 106.700055,
                TriggerRadius = 50,
                Description = "Công trình kiến trúc độc đáo mang phong cách Pháp.",
                Priority = 2,
                LastPlayedTime = null
            }
        };

                await _db.InsertAllAsync(sampleData);
                System.Diagnostics.Debug.WriteLine("Đã thêm dữ liệu mẫu bản nâng cấp thành công!");
            }
        }
    }
}