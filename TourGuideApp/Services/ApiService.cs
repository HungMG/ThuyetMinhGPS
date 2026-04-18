using System.Net.Http.Json;
using TourGuideApp.Models;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;

namespace TourGuideApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            // Link Ngrok của sếp
            _httpClient.BaseAddress = new Uri("https://stauroscopically-unlethargical-merideth.ngrok-free.dev/");

            // =========================================================
            // 🌟 SỬA LỖI 1: Tăng thời gian chờ lên 15 giây để xài 4G/Wifi yếu không bị văng
            // =========================================================
            _httpClient.Timeout = TimeSpan.FromSeconds(15);

            // =========================================================
            // 🌟 SỬA LỖI 2: Đưa thẻ VIP cho Ngrok để nó không chặn màn hình "Visit Site"
            // =========================================================
            _httpClient.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
        }

        // 🌟 KHUÔN ĐỂ HỨNG DỮ LIỆU ĐĂNG NHẬP TỪ SERVER TRẢ VỀ
        public class LoginResponse
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public int Role { get; set; }
            public string Message { get; set; }
        }

        // ==========================================================
        // 🌟 API: TỰ ĐỘNG BÁO CÁO HÀNH VI (CÓ TÍCH HỢP ĐÁ VĂNG APP)
        // ==========================================================
        public async Task TrackActionAsync(string actionName)
        {
            try
            {
                int userId = Preferences.Get("UserId", 0);
                string identifier = "";

                if (userId > 0)
                {
                    identifier = Preferences.Get("UserName", "Thành Viên Không Tên");
                }
                else
                {
                    identifier = Preferences.Get("AnonymousDeviceId", "Guest_Unknown");
                }

                var data = new { Identifier = identifier, ActionName = actionName };
                var response = await _httpClient.PostAsJsonAsync("api/Analytics/track", data);

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    App.ForceLogout("Tài khoản của bạn đã bị khóa! Vui lòng liên hệ Admin để biết thêm chi tiết.");
                }
            }
            catch
            {
                // Rớt mạng thì im lặng bỏ qua, không làm crash app
            }
        }

        public async Task<bool> SyncToursAsync(DatabaseService dbService)
        {
            try
            {
                var toursFromWeb = await _httpClient.GetFromJsonAsync<List<Tour>>("api/ToursApi");

                if (toursFromWeb != null && toursFromWeb.Count > 0)
                {
                    await dbService.SaveToursFromWebAsync(toursFromWeb);
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

        public async Task<List<POI>> GetMyPoisAsync(int userId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<List<POI>>($"api/MobilePoi/my-pois/{userId}");
                return response ?? new List<POI>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI API] Không lấy được danh sách POI của tôi: {ex.Message}");
                return new List<POI>();
            }
        }

        public async Task<bool> SubmitPoiAsync(POI poi, string localImagePath, string proofImagePath = "")
        {
            try
            {
                using var form = new MultipartFormDataContent();

                form.Add(new StringContent(poi.Name_VI ?? ""), "Name_VI");
                form.Add(new StringContent(poi.Description_VI ?? ""), "Description_VI");
                form.Add(new StringContent(poi.Latitude.ToString()), "Latitude");
                form.Add(new StringContent(poi.Longitude.ToString()), "Longitude");
                form.Add(new StringContent(poi.OwnerId.ToString()), "OwnerId");
                form.Add(new StringContent(poi.ApprovalStatus.ToString()), "ApprovalStatus");
                form.Add(new StringContent(poi.PoiType.ToString()), "PoiType");
                form.Add(new StringContent(poi.TriggerRadius.ToString()), "TriggerRadius");

                if (!string.IsNullOrEmpty(localImagePath) && File.Exists(localImagePath))
                {
                    var fileStream = File.OpenRead(localImagePath);
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    form.Add(streamContent, "imageFile", Path.GetFileName(localImagePath));
                }

                if (!string.IsNullOrEmpty(proofImagePath) && File.Exists(proofImagePath))
                {
                    var fileStream2 = File.OpenRead(proofImagePath);
                    var streamContent2 = new StreamContent(fileStream2);
                    streamContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    form.Add(streamContent2, "proofImageFile", Path.GetFileName(proofImagePath));
                }

                var response = await _httpClient.PostAsync("api/MobilePoi/submit", form);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("[GỬI POI] Đã gửi thành công lên Server chờ duyệt!");
                    return true;
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[GỬI POI THẤT BẠI] Lỗi {response.StatusCode}: {errorMsg}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI ĐƯỜNG TRUYỀN] {ex.Message}");
                return false;
            }
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                var loginData = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync("api/Auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponse>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Preferences.Remove("Offline_User");
                    Preferences.Remove("Offline_Pass");
                    Preferences.Remove("Offline_Id");
                    return new LoginResponse { UserId = -999, Message = "Tài khoản của bạn đã bị khóa bởi Admin!" };
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Preferences.Remove("Offline_User");
                    Preferences.Remove("Offline_Pass");
                    return new LoginResponse { UserId = -401, Message = "Sai tài khoản hoặc mật khẩu!" };
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI ĐĂNG NHẬP] {ex.Message}");
                return null;
            }
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            try
            {
                var registerData = new { Username = username, Password = password };
                var response = await _httpClient.PostAsJsonAsync("api/Auth/register", registerData);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI ĐĂNG KÝ] {ex.Message}");
                return false;
            }
        }

        private async Task DownloadAndCacheImageAsync(string fileName, string folderName)
        {
            if (string.IsNullOrEmpty(fileName)) return;
            string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);
            if (File.Exists(localPath)) return;

            try
            {
                string remoteUrl = $"images/{folderName}/{fileName}";
                var response = await _httpClient.GetAsync(remoteUrl);
                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    File.WriteAllBytes(localPath, imageBytes);
                    Debug.WriteLine($"[TẢI ẢNH THÀNH CÔNG] {fileName}");
                }
            }
            catch (Exception)
            {
                // Lỗi thì im lặng cho qua
            }
        }

        public async Task<bool> UpdatePoiAsync(POI poi, string localImagePath)
        {
            try
            {
                using var form = new MultipartFormDataContent();

                form.Add(new StringContent(poi.Name_VI ?? ""), "Name_VI");
                form.Add(new StringContent(poi.Description_VI ?? ""), "Description_VI");
                form.Add(new StringContent(poi.Latitude.ToString()), "Latitude");
                form.Add(new StringContent(poi.Longitude.ToString()), "Longitude");

                if (!string.IsNullOrEmpty(localImagePath) && File.Exists(localImagePath))
                {
                    var fileStream = File.OpenRead(localImagePath);
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    form.Add(streamContent, "imageFile", Path.GetFileName(localImagePath));
                }

                var response = await _httpClient.PostAsync($"api/MobilePoi/edit/{poi.Id}", form);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("[SỬA POI] Thành công!");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI SỬA POI] {ex.Message}");
                return false;
            }
        }
    }
}