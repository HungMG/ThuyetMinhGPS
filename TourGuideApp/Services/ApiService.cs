using System.Net.Http.Json;
using TourGuideApp.Models;
using System.Diagnostics;
using System.IO; // 🌟 Thêm thư viện File

namespace TourGuideApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            // ⚠️ IP máy tính của sếp
            _httpClient.BaseAddress = new Uri("http://192.168.1.151:5136/");
            _httpClient.Timeout = TimeSpan.FromSeconds(3); // 🌟 Khiên chống đơ máy
        }

        public async Task<bool> SyncToursAsync(DatabaseService dbService)
        {
            try
            {
                var toursFromWeb = await _httpClient.GetFromJsonAsync<List<Tour>>("api/ToursApi");

                if (toursFromWeb != null && toursFromWeb.Count > 0)
                {
                    await dbService.SaveToursFromWebAsync(toursFromWeb);

                    // 🌟 GỌI MÁY HÚT ẢNH TOUR (Chạy ngầm không chờ)
                    _ = Task.Run(async () =>
                    {
                        foreach (var t in toursFromWeb)
                            await DownloadAndCacheImageAsync(t.ImageUrl, "tours");
                    });

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MẤT MẠNG] Không thể đồng bộ Tour: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncPOIsAsync(DatabaseService dbService)
        {
            try
            {
                var poisFromWeb = await _httpClient.GetFromJsonAsync<List<POI>>("api/POIsApi");

                if (poisFromWeb != null && poisFromWeb.Count > 0)
                {
                    await dbService.SavePOIsFromWebAsync(poisFromWeb);

                    // 🌟 GỌI MÁY HÚT ẢNH POI (Chạy ngầm không chờ)
                    _ = Task.Run(async () =>
                    {
                        foreach (var p in poisFromWeb)
                            await DownloadAndCacheImageAsync(p.ImageUrl, "pois");
                    });

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

        // ==========================================================
        // 🌟 BĂNG CHUYỀN HÚT ẢNH OFFLINE NẰM Ở ĐÂY
        // ==========================================================
        private async Task DownloadAndCacheImageAsync(string fileName, string folderName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            // CÓ RỒI THÌ THÔI KHÔNG TẢI NỮA
            if (File.Exists(localPath)) return;

            try
            {
                // Lên Web tải về
                string remoteUrl = $"images/{folderName}/{fileName}";
                var response = await _httpClient.GetAsync(remoteUrl);

                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    File.WriteAllBytes(localPath, imageBytes); // Cất vào điện thoại
                    Debug.WriteLine($"[TẢI ẢNH THÀNH CÔNG] {fileName}");
                }
            }
            catch (Exception)
            {
                // Lỗi thì im lặng cho qua
            }
        }
    }
}