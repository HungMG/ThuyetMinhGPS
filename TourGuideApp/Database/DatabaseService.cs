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

            // Xác định đường dẫn lưu file database trên điện thoại
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "TourGuide.db");

            // Tạo kết nối
            _db = new SQLiteAsyncConnection(databasePath);

            // Tạo bảng (nếu chưa tồn tại)
            await _db.CreateTableAsync<POI>();
        }

        // Hàm lấy toàn bộ điểm đến
        public async Task<List<POI>> GetAllPOIsAsync()
        {
            await InitAsync();
            return await _db.Table<POI>().ToListAsync();
        }

        // Hàm cập nhật trạng thái đã tham quan
        public async Task MarkAsVisitedAsync(int poiId)
        {
            await InitAsync();
            var poi = await _db.Table<POI>().Where(p => p.Id == poiId).FirstOrDefaultAsync();
            if (poi != null)
            {
                poi.IsVisited = true;
                await _db.UpdateAsync(poi);
            }
        }
        public async Task SeedDataAsync()
        {
            await InitAsync(); // Đảm bảo DB đã khởi tạo

            // Đếm xem trong bảng POI có dữ liệu chưa
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
                IsVisited = false
            },
            new POI
            {
                Name = "Bưu điện Trung tâm Sài Gòn",
                Latitude = 10.779833,
                Longitude = 106.700055,
                TriggerRadius = 50,
                Description = "Công trình kiến trúc độc đáo mang phong cách Pháp.",
                IsVisited = false
            }
        };

                // Insert một lúc nhiều dòng vào DB
                await _db.InsertAllAsync(sampleData);
                System.Diagnostics.Debug.WriteLine("Đã thêm dữ liệu mẫu thành công!");
            }
        }
    }

}