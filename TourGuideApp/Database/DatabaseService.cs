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
                poi.LastPlayedTime = DateTime.Now;
                await _db.UpdateAsync(poi);
            }
        }

        // 👇 ĐÃ SỬA: Thêm InitAsync và gán giá trị mặc định cho an toàn
        public async Task AddPOIAsync(POI newPoi)
        {
            await InitAsync();

            // Gán giá trị mặc định cho an toàn nếu người dùng không nhập
            if (newPoi.TriggerRadius == 0) newPoi.TriggerRadius = 50;
            if (newPoi.Priority == 0) newPoi.Priority = 99; // Điểm tự tạo thì ưu tiên thấp

            await _db.InsertAsync(newPoi);
        }

        // 👇 ĐÃ SỬA: Thêm InitAsync()
        public async Task<List<POI>> GetFavoritePOIsAsync()
        {
            await InitAsync();
            return await _db.Table<POI>().Where(p => p.IsFavorite).ToListAsync();
        }

        // 👇 ĐÃ SỬA: Thêm InitAsync()
        public async Task UpdatePOIAsync(POI poi)
        {
            await InitAsync();
            await _db.UpdateAsync(poi);
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
                        Description = "Nơi lưu giữ dấu ấn lịch sử hào hùng, là biểu tượng của sự thống nhất và hòa bình, thu hút hàng triệu lượt khách đến tham quan mỗi năm.",
                        Priority = 1,
                        LastPlayedTime = null,
                        IsFavorite = false,
                        
                        // 👇 THÊM HÌNH ẢNH MẪU 👇
                        ImageUrl = "img_dinh_doc_lap.jpg"
                    },
                    new POI
                    {
                        Name = "Bưu điện Trung tâm Sài Gòn",
                        Latitude = 10.779833,
                        Longitude = 106.700055,
                        TriggerRadius = 50,
                        Description = "Công trình kiến trúc độc đáo mang phong cách Pháp, nằm ngay giữa lòng thành phố, là một trong những điểm check-in không thể bỏ qua.",
                        Priority = 2,
                        LastPlayedTime = null,
                        IsFavorite = true,
                        
                        // 👇 THÊM HÌNH ẢNH MẪU 👇
                        ImageUrl = "img_buu_dien.jpg"
                    }
                };

                await _db.InsertAllAsync(sampleData);
                System.Diagnostics.Debug.WriteLine("Đã thêm dữ liệu mẫu bản nâng cấp thành công!");
            }
        }
    }
}