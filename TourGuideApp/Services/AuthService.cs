using System.Net.Http.Json;
using TourGuideApp.Models;

namespace TourGuideApp.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        // 🌟 SẾP THAY CÁI PORT 5001 THÀNH PORT CỦA WEB API NHÀ SẾP NHA
        private const string BaseUrl = "http://192.168.1.229:5136/api/Auth/";

        public AuthService()
        {
            // Đoạn này giúp bỏ qua kiểm tra chứng chỉ SSL khi chạy debug (tránh lỗi HTTPS)
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            _httpClient = new HttpClient(handler);
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                var loginData = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kết nối API: {ex.Message}");
                return null;
            }
        }
        // Thêm hàm này vào dưới hàm LoginAsync
        public async Task<bool> RegisterAsync(string username, string password)
        {
            try
            {
                var registerData = new { Username = username, Password = password, Role = 0 };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}register", registerData);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kết nối API: {ex.Message}");
                return false;
            }
        }
    }
}