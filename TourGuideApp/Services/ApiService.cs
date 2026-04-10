using System.Net.Http.Json;
using TourGuideApp.Models;
using System.Diagnostics;

namespace TourGuideApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            // ⚠️ Nhớ đổi cái IP này thành IP máy tính của sếp hiện tại nhé!
            _httpClient.BaseAddress = new Uri("http://192.168.100.220:5136/");
        }

        // 1. Chạy lên mạng gom Tour về giao cho Thủ kho
        public async Task<bool> SyncToursAsync(DatabaseService dbService)
        {
            try
            {
                // Gọi điện lên Web xin danh sách Tour
                // Đổi "api/Tours" thành "api/ToursApi"
                var toursFromWeb = await _httpClient.GetFromJsonAsync<List<Tour>>("api/ToursApi");

                if (toursFromWeb != null && toursFromWeb.Count > 0)
                {
                    // Đưa cho Thủ kho cất vào SQLite
                    await dbService.SaveToursFromWebAsync(toursFromWeb);
                    return true; // Báo cáo: "Đã cất kho thành công!"
                }
                return false;
            }
            catch (Exception ex)
            {
                // 🚨 ĐIỂM ĂN TIỀN LÀ ĐÂY: Nếu rớt mạng, App sẽ rớt vào hàm Catch này.
                // Nó sẽ không văng lỗi sập App, mà chỉ âm thầm báo "Thất bại".
                Debug.WriteLine($"[MẤT MẠNG] Không thể đồng bộ Tour: {ex.Message}");
                return false;
            }
        }

        // 2. Chạy lên mạng gom Địa điểm (POI) về giao cho Thủ kho
        public async Task<bool> SyncPOIsAsync(DatabaseService dbService)
        {
            try
            {
                // Đổi "api/POIs" thành "api/POIsApi"
                var poisFromWeb = await _httpClient.GetFromJsonAsync<List<POI>>("api/POIsApi");

                if (poisFromWeb != null && poisFromWeb.Count > 0)
                {
                    await dbService.SavePOIsFromWebAsync(poisFromWeb);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MẤT MẠNG] Không thể đồng bộ POI: {ex.Message}");
                return false;
            }
        }
    }
}