using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using TourGuideApp.Models;
using System.IO;
using TourGuideApp.Services;

namespace TourGuideApp.Views;

public partial class AddPoiPage : ContentPage
{
    private string _localImagePath = "";
    private string _proofImagePath = ""; // Đường dẫn ảnh giấy phép

    public AddPoiPage()
    {
        InitializeComponent();
    }

    // Sự kiện thay đổi loại hình địa điểm
    private void OnPoiTypeChanged(object sender, EventArgs e)
    {
        frmProofImage.IsVisible = (pckPoiType.SelectedIndex == 1);
    }

    // Chọn ảnh địa điểm
    private async void OnSelectImageTapped(object sender, EventArgs e)
    {
        var photo = await PickPhotoAsync();
        if (photo != null)
        {
            _localImagePath = photo;
            pnlImagePlaceholder.IsVisible = false;
            imgPreview.Source = ImageSource.FromFile(_localImagePath);
            imgPreview.IsVisible = true;
        }
    }

    // Chọn ảnh giấy phép kinh doanh
    private async void OnSelectProofImageTapped(object sender, EventArgs e)
    {
        var photo = await PickPhotoAsync();
        if (photo != null)
        {
            _proofImagePath = photo;
            imgProofPreview.Source = ImageSource.FromFile(_proofImagePath);
            imgProofPreview.IsVisible = true;
        }
    }

    private async Task<string> PickPhotoAsync()
    {
        try
        {
            string action = await DisplayActionSheet("Tải ảnh", "Hủy", null, "Chụp ảnh mới", "Chọn từ thư viện");
            FileResult photo = action switch
            {
                "Chụp ảnh mới" => await MediaPicker.Default.CapturePhotoAsync(),
                "Chọn từ thư viện" => await MediaPicker.Default.PickPhotoAsync(),
                _ => null
            };

            if (photo != null)
            {
                string localPath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using Stream sourceStream = await photo.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(localPath);
                await sourceStream.CopyToAsync(localFileStream);
                return localPath;
            }
        }
        catch (Exception ex) { await DisplayAlert("Lỗi", ex.Message, "OK"); }
        return null;
    }

    private async void OnGetLocationClicked(object sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10)));
            if (location != null)
            {
                txtLat.Text = location.Latitude.ToString();
                txtLng.Text = location.Longitude.ToString();
            }
        }
        catch { await DisplayAlert("Lỗi GPS", "Vui lòng bật định vị!", "OK"); }
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtLat.Text))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập tên và GPS!", "OK");
            return;
        }

        if (string.IsNullOrEmpty(_localImagePath))
        {
            await DisplayAlert("Thông báo", TourGuideApp.Resources.Languages.AppLang.AddPoiErrorNoImage, "OK");
            return;
        }

        if (pckPoiType.SelectedIndex == 1 && string.IsNullOrEmpty(_proofImagePath))
        {
            await DisplayAlert("Xác thực", TourGuideApp.Resources.Languages.AppLang.AddPoiErrorNoProof, "OK");
            return;
        }

        var newPoi = new POI
        {
            Name_VI = txtName.Text.Trim(),
            Description_VI = txtDescription.Text?.Trim(),
            Latitude = double.Parse(txtLat.Text),
            Longitude = double.Parse(txtLng.Text),
            TriggerRadius = 50,
            ApprovalStatus = 0,
            OwnerId = Preferences.Get("UserId", 0),
            PoiType = pckPoiType.SelectedIndex == 1 ? 1 : 0
        };

        ApiService apiService = new ApiService();
        // GỌI HÀM VỚI 3 THAM SỐ
        bool isSuccess = await apiService.SubmitPoiAsync(newPoi, _localImagePath, _proofImagePath);

        if (isSuccess)
        {
            // 🌟 GẮN MÁY DÒ: BÁO CÁO HÀNH VI LÊN SERVER
            _ = apiService.TrackActionAsync($"Thêm địa điểm: {newPoi.Name_VI}");

            await DisplayAlert("Thành công", "Đã gửi yêu cầu xét duyệt!", "OK");
            await Navigation.PopAsync();
        }
        else
        {
            await DisplayAlert("Thất bại", "Kiểm tra mạng và thử lại sếp ơi!", "Đóng");
        }
    }
}