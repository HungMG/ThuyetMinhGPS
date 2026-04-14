using System.Net.Http.Json;
using TourGuideApp.Models;
using System.Diagnostics;
using System.IO; // 🌟 Thêm thư viện File
using System.Net.Http.Headers; // 🌟 BẮT BUỘC THÊM CÁI NÀY ĐỂ ĐÓNG GÓI FILE ẢNH

namespace TourGuideApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
            // 🌟 Đã đổi sang link Ngrok
            _httpClient.BaseAddress = new Uri("https://stauroscopically-unlethargical-merideth.ngrok-free.dev/");
            _httpClient.Timeout = TimeSpan.FromSeconds(3);
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
        // 🌟 API: TỰ ĐỘNG BÁO CÁO HÀNH VI (BẢN V2 TỰ NHẬN DIỆN USER)
        // ==========================================================
        public async Task TrackActionAsync(string actionName)
        {
            try
            {
                // 1. Tự động kiểm tra xem ai đang dùng App
                int userId = Preferences.Get("UserId", 0);
                string identifier = "";

                if (userId > 0)
                {
                    // Nếu là Thành viên có tài khoản -> Lấy Tên thật
                    identifier = Preferences.Get("UserName", "Thành Viên Không Tên");
                }
                else
                {
                    // Nếu là Khách vãng lai -> Lấy mã Device ID
                    identifier = Preferences.Get("AnonymousDeviceId", "Guest_Unknown");
                }

                // 2. Đóng gói và gửi lên Server
                var data = new { Identifier = identifier, ActionName = actionName };
                await _httpClient.PostAsJsonAsync("api/Analytics/track", data);
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

        // 🌟 HÀM LẤY DANH SÁCH ĐỊA ĐIỂM CỦA TÔI TỪ SERVER
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

        // ==========================================================
        // 🌟 HÀM MỚI: GỬI ĐỊA ĐIỂM + HÌNH ẢNH LÊN WEB SERVER
        // ==========================================================
        public async Task<bool> SubmitPoiAsync(POI poi, string localImagePath, string proofImagePath = "")
        {
            try
            {
                // Sử dụng link Ngrok tĩnh sếp đã thiết lập trong constructor
                using var form = new MultipartFormDataContent();

                // 1. Nhét các thông tin văn bản
                form.Add(new StringContent(poi.Name_VI ?? ""), "Name_VI");
                form.Add(new StringContent(poi.Description_VI ?? ""), "Description_VI");
                form.Add(new StringContent(poi.Latitude.ToString()), "Latitude");
                form.Add(new StringContent(poi.Longitude.ToString()), "Longitude");
                form.Add(new StringContent(poi.OwnerId.ToString()), "OwnerId");
                form.Add(new StringContent(poi.ApprovalStatus.ToString()), "ApprovalStatus");
                form.Add(new StringContent(poi.PoiType.ToString()), "PoiType"); // 🌟 Loại hình (0: Công cộng, 1: Kinh doanh)
                form.Add(new StringContent(poi.TriggerRadius.ToString()), "TriggerRadius"); // Luôn ép 50m

                // 2. Nhét ảnh đại diện địa điểm (Bắt buộc)
                if (!string.IsNullOrEmpty(localImagePath) && File.Exists(localImagePath))
                {
                    var fileStream = File.OpenRead(localImagePath);
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    form.Add(streamContent, "imageFile", Path.GetFileName(localImagePath));
                }

                // 🌟 3. Nhét ảnh Giấy phép kinh doanh (Nếu có)
                if (!string.IsNullOrEmpty(proofImagePath) && File.Exists(proofImagePath))
                {
                    var fileStream2 = File.OpenRead(proofImagePath);
                    var streamContent2 = new StreamContent(fileStream2);
                    streamContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                    form.Add(streamContent2, "proofImageFile", Path.GetFileName(proofImagePath));
                }

                // Gửi bưu kiện lên Server
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

        // ==========================================================
        // 🌟 API: ĐĂNG NHẬP
        // ==========================================================
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
            {
                // Đóng gói tài khoản, mật khẩu
                var loginData = new { Username = username, Password = password };

                // Gửi lên cổng api/Auth/login của sếp
                var response = await _httpClient.PostAsJsonAsync("api/Auth/login", loginData);

                if (response.IsSuccessStatusCode)
                {
                    // Lấy cục dữ liệu trả về (gồm UserId, Role...) ép vào cái Khuôn
                    return await response.Content.ReadFromJsonAsync<LoginResponse>();
                }
                return null; // Sai pass hoặc không tồn tại
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI ĐĂNG NHẬP] {ex.Message}");
                return null;
            }
        }

        // ==========================================================
        // 🌟 API: ĐĂNG KÝ TÀI KHOẢN
        // ==========================================================
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

        // ==========================================================
        // 🌟 HÀM MỚI: GỬI LỆNH SỬA ĐỊA ĐIỂM LÊN SERVER
        // ==========================================================
        public async Task<bool> UpdatePoiAsync(POI poi, string localImagePath)
        {
            try
            {
                using var form = new MultipartFormDataContent();

                form.Add(new StringContent(poi.Name_VI ?? ""), "Name_VI");
                form.Add(new StringContent(poi.Description_VI ?? ""), "Description_VI");
                form.Add(new StringContent(poi.Latitude.ToString()), "Latitude");
                form.Add(new StringContent(poi.Longitude.ToString()), "Longitude");

                // Chỉ gói ảnh gửi đi NẾU sếp có chụp/chọn ảnh mới
                if (!string.IsNullOrEmpty(localImagePath) && File.Exists(localImagePath))
                {
                    var fileStream = File.OpenRead(localImagePath);
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    form.Add(streamContent, "imageFile", Path.GetFileName(localImagePath));
                }

                // Phóng bưu kiện cập nhật lên Server
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