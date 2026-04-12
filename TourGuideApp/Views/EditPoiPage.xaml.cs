using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using System.IO;
using System.Xml.Linq;
using TourGuideApp.Models;
using TourGuideApp.Services;

namespace TourGuideApp.Views;

public partial class EditPoiPage : ContentPage
{
    private string _localImagePath = "";
    private POI _editingPoi; // 🌟 Biến lưu giữ cái POI đang được sửa

    // Hàm khởi tạo này yêu cầu phải đưa vào 1 cái POI
    public EditPoiPage(POI poiToEdit)
    {
        InitializeComponent();
        _editingPoi = poiToEdit;
        LoadDataToForm();
    }

    // 🌟 Đổ dữ liệu cũ ra Form
    private void LoadDataToForm()
    {
        txtName.Text = _editingPoi.Name_VI;
        txtDescription.Text = _editingPoi.Description_VI;
        txtLat.Text = _editingPoi.Latitude.ToString();
        txtLng.Text = _editingPoi.Longitude.ToString();

        // Hiện ảnh cũ lên (nếu có)
        if (!string.IsNullOrEmpty(_editingPoi.ImageUrl))
        {
            pnlImagePlaceholder.IsVisible = false;
            imgPreview.IsVisible = true;
            // Dùng thuộc tính FullImageUrl của sếp để load ảnh từ Web
            imgPreview.Source = _editingPoi.FullImageUrl;
        }
    }

    private async void OnSelectImageTapped(object sender, EventArgs e)
    {
        try
        {
            string action = await DisplayActionSheet("Tải ảnh mới lên", "Hủy", null, "Chụp ảnh mới", "Chọn từ thư viện");
            FileResult photo = null;

            if (action == "Chụp ảnh mới" && MediaPicker.Default.IsCaptureSupported)
                photo = await MediaPicker.Default.CapturePhotoAsync();
            else if (action == "Chọn từ thư viện")
                photo = await MediaPicker.Default.PickPhotoAsync();

            if (photo != null)
            {
                _localImagePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using Stream sourceStream = await photo.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(_localImagePath);
                await sourceStream.CopyToAsync(localFileStream);

                pnlImagePlaceholder.IsVisible = false;
                imgPreview.IsVisible = true;
                imgPreview.Source = ImageSource.FromFile(_localImagePath);
            }
        }
        catch (Exception ex) { await DisplayAlert("Lỗi", "Không thể tải ảnh: " + ex.Message, "OK"); }
    }

    private async void OnGetLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync();
            if (location != null)
            {
                txtLat.Text = location.Latitude.ToString();
                txtLng.Text = location.Longitude.ToString();
            }
        }
        catch { await DisplayAlert("Lỗi", "Không lấy được vị trí GPS!", "OK"); }
    }

    // 🌟 SỰ KIỆN LƯU CẬP NHẬT
    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtLat.Text))
        {
            await DisplayAlert("Lỗi", "Sếp phải nhập tên quán và lấy tọa độ GPS nhé!", "OK");
            return;
        }

        // Cập nhật dữ liệu mới vào cái hộp POI cũ
        _editingPoi.Name_VI = txtName.Text.Trim();
        _editingPoi.Description_VI = txtDescription.Text?.Trim();
        _editingPoi.Latitude = Convert.ToDouble(txtLat.Text);
        _editingPoi.Longitude = Convert.ToDouble(txtLng.Text);

        ApiService apiService = new ApiService();
        bool isSuccess = await apiService.UpdatePoiAsync(_editingPoi, _localImagePath);

        if (isSuccess)
        {
            await DisplayAlert("Thành công", "Đã cập nhật bài! Admin sẽ xem xét lại nhé sếp.", "Tuyệt vời");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Thất bại", "Sửa không thành công, sếp kiểm tra lại mạng!", "Đóng");
        }
    }
}