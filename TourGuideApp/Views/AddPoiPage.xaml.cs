using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using TourGuideApp.Models;
using System.IO;
using TourGuideApp.Services;

namespace TourGuideApp.Views;

public partial class AddPoiPage : ContentPage
{
    // Biến này để lưu đường dẫn tạm của tấm ảnh sếp vừa chụp
    private string _localImagePath = "";

    public AddPoiPage()
    {
        InitializeComponent();
    }

    // ==========================================
    // 🌟 1. SỰ KIỆN: CHỌN HOẶC CHỤP ẢNH
    // ==========================================
    private async void OnSelectImageTapped(object sender, EventArgs e)
    {
        try
        {
            // Mở Menu hỏi sếp muốn làm gì
            string action = await DisplayActionSheet("Tải ảnh lên", "Hủy", null, "Chụp ảnh mới", "Chọn từ thư viện");

            FileResult photo = null;

            if (action == "Chụp ảnh mới")
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    photo = await MediaPicker.Default.CapturePhotoAsync();
                }
                else
                {
                    await DisplayAlert("Lỗi", "Thiết bị của sếp không hỗ trợ chụp ảnh!", "OK");
                }
            }
            else if (action == "Chọn từ thư viện")
            {
                photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Chọn ảnh địa điểm"
                });
            }

            // Nếu sếp đã chọn được ảnh
            if (photo != null)
            {
                // Lưu tạm ảnh vào bộ nhớ đệm của điện thoại
                _localImagePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using Stream sourceStream = await photo.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(_localImagePath);
                await sourceStream.CopyToAsync(localFileStream);

                // Giấu cái biểu tượng Camera đi, và hiện tấm ảnh thật lên
                pnlImagePlaceholder.IsVisible = false;
                imgPreview.Source = ImageSource.FromFile(_localImagePath);
                imgPreview.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", $"Không thể tải ảnh: {ex.Message}", "Đóng");
        }
    }

    // ==========================================
    // 🌟 2. SỰ KIỆN: LẤY TỌA ĐỘ GPS
    // ==========================================
    private async void OnGetLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                txtLat.Text = location.Latitude.ToString();
                txtLng.Text = location.Longitude.ToString();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi GPS", $"Không lấy được vị trí: {ex.Message}", "Đóng");
        }
    }

    // ==========================================
    // 🌟 3. SỰ KIỆN: GỬI BÀI CHỜ DUYỆT
    // ==========================================
    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        // 1. Kiểm tra Tên và Tọa độ
        if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtLat.Text))
        {
            await DisplayAlert("Lỗi", "Sếp phải nhập tên quán và lấy tọa độ GPS nhé!", "OK");
            return;
        }

        // 🌟 2. CỔNG AN NINH KIỂM TRA ẢNH NẰM Ở ĐÂY NÈ SẾP
        if (string.IsNullOrEmpty(_localImagePath))
        {
            await DisplayAlert("Khoan đã sếp ơi", "Sếp chưa chọn ảnh cho địa điểm này kìa! Phải có ảnh thì Admin mới duyệt chứ!", "Đã hiểu");
            return;
        }

        int currentUserId = Preferences.Get("UserId", 0);

        {
            // 1. Khởi tạo đối tượng POI mới
            var newPoi = new POI
            {
                Name_VI = txtName.Text,
                Description_VI = txtDescription.Text,
                Latitude = double.Parse(txtLat.Text),
                Longitude = double.Parse(txtLng.Text),

                // 🌟 ÉP SỐ 50 Ở ĐÂY: Để đảm bảo khi lên Web nó không bị thành số 0
                TriggerRadius = 50,

                ApprovalStatus = 0, // Chờ duyệt
                OwnerId = Preferences.Get("UserId", 0)
            };

            // 🌟 BẮT ĐẦU GỌI SERVER
            ApiService apiService = new ApiService();
            bool isSuccess = await apiService.SubmitPoiAsync(newPoi, _localImagePath);

            if (isSuccess)
            {
                await DisplayAlert("Thành công", "Đã gửi địa điểm! Đang chờ Admin duyệt.", "Tuyệt vời");
                await Navigation.PopAsync(); // Đóng trang, quay về bản đồ
            }
            else
            {
                await DisplayAlert("Thất bại", "Máy chủ không phản hồi, sếp kiểm tra lại mạng nhé!", "Đóng");
            }
        }
    }
}
