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
            // ⚠️ IP máy tính của sếp
            _httpClient.BaseAddress = new Uri("http://192.168.1.229:5136/");
            _httpClient.Timeout = TimeSpan.FromSeconds(3); // 🌟 Khiên chống đơ máy
        }

        // 🌟 KHUÔN ĐỂ HỨNG DỮ LIỆU ĐĂNG NHẬP TỪ SERVER TRẢ VỀ
        public class LoginResponse
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public int Role { get; set; }
            public string Message { get; set; }
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
        public async Task<bool> SubmitPoiAsync(POI poi, string localImagePath)
        {
            try
            {
                // Dùng MultipartFormDataContent để chứa cả CHỮ và ẢNH (Bưu kiện tổng hợp)
                using var form = new MultipartFormDataContent();

                // 1. Nhét thông tin chữ vào bưu kiện
                form.Add(new StringContent(poi.Name_VI ?? ""), "Name_VI");
                form.Add(new StringContent(poi.Description_VI ?? ""), "Description_VI");
                form.Add(new StringContent(poi.Latitude.ToString()), "Latitude");
                form.Add(new StringContent(poi.Longitude.ToString()), "Longitude");
                form.Add(new StringContent(poi.OwnerId.ToString()), "OwnerId");
                form.Add(new StringContent(poi.ApprovalStatus.ToString()), "ApprovalStatus"); // Gửi số 0 = Chờ duyệt

                // 2. Nhét file ảnh vào bưu kiện (nếu sếp có chọn ảnh)
                if (!string.IsNullOrEmpty(localImagePath) && File.Exists(localImagePath))
                {
                    var fileStream = File.OpenRead(localImagePath);
                    var streamContent = new StreamContent(fileStream);

                    // Đóng dấu đây là file hình ảnh để Server nhận diện
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                    // Lưu ý: Tên "imageFile" phải trùng khớp với tên tham số bên Web API sếp nhé
                    form.Add(streamContent, "imageFile", Path.GetFileName(localImagePath));
                }

                // 3. Phóng bưu kiện lên Server (Sử dụng link tương đối vì đã có BaseAddress ở trên)
                var response = await _httpClient.PostAsync("api/MobilePoi/submit", form);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("[GỬI POI] Đã gửi thành công lên Server chờ duyệt!");
                    return true;
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[GỬI POI BỊ TỪ CHỐI] Lỗi {response.StatusCode}: {errorMsg}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI ĐƯỜNG TRUYỀN] Không thể gửi POI: {ex.Message}");
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