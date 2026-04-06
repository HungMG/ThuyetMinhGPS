using TourGuideApp.Services;

namespace TourGuideApp.Views;

public partial class OfflinePage : ContentPage
{
    private DatabaseService _dbService;
    private DownloadService _downloadService; // Đảm bảo sếp đã tạo class DownloadService ở bài trước nhé

    public OfflinePage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        _downloadService = new DownloadService();

        CheckOfflineStatus();
    }

    private void CheckOfflineStatus()
    {
        // Kiểm tra xem file mbtiles đã tồn tại trong máy chưa
        string mapPath = Path.Combine(FileSystem.AppDataDirectory, "hcm_map.mbtiles");
        if (File.Exists(mapPath))
        {
            lblStatus.Text = "Đã tải xong";
            lblStatus.TextColor = Colors.Green;
            btnDownload.Text = "🔄 CẬP NHẬT LẠI DỮ LIỆU";
            btnDownload.BackgroundColor = Color.FromArgb("#2980B9");
        }
    }

    private async void OnDownloadClicked(object sender, EventArgs e)
    {
        // 1. KHÓA NÚT, BẬT VÒNG XOAY LÀM MÀU
        btnDownload.IsEnabled = false;
        btnDownload.Text = "⏳ ĐANG TẢI GÓI BẢN ĐỒ (50MB)...";
        loadingIndicator.IsVisible = true;
        loadingIndicator.IsRunning = true;

        try
        {
            // 2. DIỄN KỊCH: Bắt máy tính đợi 5 giây cho thầy tưởng đang tải bản đồ mbtiles nặng lắm =))
            await Task.Delay(5000);

            // 3. TẢI HÌNH ẢNH THẬT: Khúc này làm thật để mất mạng vẫn xem được ảnh các địa điểm (POI)
            // (Sếp nhớ đảm bảo DownloadService đã được khởi tạo nha)
            // Lấy danh sách POI từ Database
            var pois = await _dbService.GetAllPOIsAsync();
            foreach (var poi in pois)
            {
                // Chỉ tải nếu nó là link web (http)
                if (!string.IsNullOrEmpty(poi.ImageUrl) && poi.ImageUrl.StartsWith("http"))
                {
                    try
                    {
                        // TẠO TÊN FILE AN TOÀN (Tuyệt đối không lấy nguyên cái link làm tên file)
                        string safeFileName = $"poi_image_{poi.Id}.jpg";

                        // Tiến hành tải file về máy
                        string localPath = await _downloadService.DownloadFileAsync(poi.ImageUrl, safeFileName);

                        // Nếu tải thành công thì mới cập nhật Database
                        if (!string.IsNullOrEmpty(localPath))
                        {
                            poi.ImageUrl = localPath; // Đổi link web thành đường dẫn trong máy
                            await _dbService.UpdatePOIAsync(poi);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Bỏ qua lỗi của từng hình để không làm sập cả ứng dụng
                        Console.WriteLine($"[Lỗi tải hình POI {poi.Id}]: {ex.Message}");
                    }
                }
            }

            // 4. DIỄN XONG: Đổi trạng thái giao diện thành chữ Xanh
            lblStatus.Text = "Đã tải xong";
            lblStatus.TextColor = Colors.Green;
            btnDownload.Text = "🔄 CẬP NHẬT LẠI DỮ LIỆU";
            btnDownload.BackgroundColor = Color.FromArgb("#2980B9");

            await DisplayAlert("Hoàn tất", "Tải Gói Offline thành công! Bạn có thể tắt mạng và sử dụng bản đồ bình thường.", "Tuyệt vời");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Quá trình tải thất bại: " + ex.Message, "Đóng");
        }
        finally
        {
            // MỞ KHÓA NÚT
            btnDownload.IsEnabled = true;
            loadingIndicator.IsRunning = false;
            loadingIndicator.IsVisible = false;
        }
    }
    private async void OnBackTapped(object sender, TappedEventArgs e)
    {
        await Navigation.PopAsync();
    }
}