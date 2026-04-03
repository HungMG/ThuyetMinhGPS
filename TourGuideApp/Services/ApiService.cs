using System.Net.Http.Json; // Dùng cái này của Microsoft, cực nhanh!
using TourGuideApp.Models; // Đổi thành tên namespace thật của bạn

namespace TourGuideApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;

        // ĐIỀN ĐỊA CHỈ WEB ADMIN CỦA BẠN VÀO ĐÂY
        // Thay 5136 bằng đúng cái số port localhost của bạn
        private readonly string _baseUrl = "http://192.168.100.230";

        public ApiService()
        {
            _client = new HttpClient();
        }

        // Hàm này sẽ hút toàn bộ danh sách địa điểm về
        public async Task<List<POI>> GetPOIsAsync()
        {
            try
            {
                string url = $"{_baseUrl}/api/POIsApi";

                // Hút dữ liệu và tự động nén vào List<POI>
                var pois = await _client.GetFromJsonAsync<List<POI>>(url);

                return pois ?? new List<POI>(); // Trả về danh sách (nếu rỗng thì trả về [])
            }
            catch (Exception ex)
            {
                // Lỡ đứt cáp thì báo lỗi ra đây
                Console.WriteLine($"🚨 LỖI MÁY BƠM: {ex.Message}");
                return new List<POI>();
            }
        }
        // Hàm này sẽ hút toàn bộ danh sách Tour về
        public async Task<List<Tour>> GetToursAsync()
        {
            try
            {
                string url = $"{_baseUrl}/api/ToursApi"; // Nhớ trỏ đúng tên API của Tour
                var tours = await _client.GetFromJsonAsync<List<Tour>>(url);
                return tours ?? new List<Tour>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 LỖI MÁY BƠM TOUR: {ex.Message}");
                return new List<Tour>();
            }
        }
    }
}