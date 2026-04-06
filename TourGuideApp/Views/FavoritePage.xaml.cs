using TourGuideApp.Services;
using TourGuideApp.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace TourGuideApp.Views;

public partial class FavoritePage : ContentPage
{
    private DatabaseService _dbService;
    private NarrationEngine _narrationEngine = new NarrationEngine();
    private POI _temporaryPoi;
    private bool _isAudioPlaying = false;

    public FavoritePage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
    }

    // Mỗi lần mở tab này lên là tự động load lại dữ liệu mới nhất
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Đóng popup phòng hờ nó bị "ám" từ lần trước
        favoriteDetailPopup.IsOpen = false;

        await LoadFavoritesAsync();
    }

    // Tắt tiếng nếu sếp chuyển qua tab khác
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
        }
    }

    private async Task LoadFavoritesAsync()
    {
        // Lấy danh sách tim đỏ từ Database
        var danhSachYeuThich = await _dbService.GetFavoritePOIsAsync();
        favoriteListView.ItemsSource = danhSachYeuThich;
    }

    // =================================================================
    // 🌟 1. SỰ KIỆN: XÓA KHỎI DANH SÁCH YÊU THÍCH (Ở BÊN NGOÀI LIST)
    // =================================================================
    private async void OnRemoveFavoriteClicked(object sender, EventArgs e)
    {
        var button = sender as ImageButton;
        var poiBiXoa = button?.CommandParameter as POI;

        if (poiBiXoa != null)
        {
            bool xacNhan = await DisplayAlert("Xác nhận", $"Bạn có chắc muốn bỏ '{poiBiXoa.CurrentName}' khỏi danh sách yêu thích?", "Đồng ý", "Hủy");

            if (xacNhan)
            {
                poiBiXoa.IsFavorite = false;
                await _dbService.UpdatePOIAsync(poiBiXoa);
                await LoadFavoritesAsync(); // Tải lại danh sách
            }
        }
    }

    // =================================================================
    // 🌟 2. SỰ KIỆN: BẤM VÀO ITEM ĐỂ MỞ POPUP
    // =================================================================
    private void OnFavoriteItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI poi)
        {
            _temporaryPoi = poi;

            // Đổ dữ liệu lên UI của Popup
            lblPopupName.Text = poi.CurrentName;
            lblPopupDescription.Text = poi.CurrentDescription;
            imgPopupImage.Source = !string.IsNullOrEmpty(poi.ImageUrl) ? ImageSource.FromUri(new Uri(poi.FullImageUrl)) : "img_default_poi.png";

            // Đồng bộ ngôn ngữ hiện tại của máy vào Picker
            string savedLang = Preferences.Get("AppLanguage", "vi");
            PopupLangPicker.SelectedIndexChanged -= OnPopupLangChanged;
            PopupLangPicker.SelectedIndex = savedLang switch { "en" => 1, "zh" => 2, "ko" => 3, "ja" => 4, _ => 0 };
            PopupLangPicker.SelectedIndexChanged += OnPopupLangChanged;

            // Reset trạng thái nút Audio bằng AppLang
            btnReadAudio.Text = TourGuideApp.Resources.Languages.AppLang.ListenAudio;
            btnReadAudio.BackgroundColor = Color.FromArgb("#F39C12");
            _isAudioPlaying = false;

            // Bung lụa!
            favoriteDetailPopup.IsOpen = true;
        }
    }

    // =================================================================
    // 🌟 3. SỰ KIỆN: NGHE AUDIO & ĐỔI NGÔN NGỮ POPUP
    // =================================================================
    private async void OnReadDescriptionClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;

        // Nếu đang đọc thì bấm cái nữa là DỪNG
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
            UpdateAudioButton(false);
            return;
        }

        // Bắt đầu đọc
        _isAudioPlaying = true;
        UpdateAudioButton(true);

        string langCode = PopupLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };
        string text = langCode switch { "en" => _temporaryPoi.Description_EN, "zh" => _temporaryPoi.Description_ZH, "ko" => _temporaryPoi.Description_KO, "ja" => _temporaryPoi.Description_JA, _ => _temporaryPoi.Description_VI };

        await _narrationEngine.SpeakAsync($"{_temporaryPoi.CurrentName}. {text}", langCode);

        _isAudioPlaying = false;
        UpdateAudioButton(false);
    }

    private void UpdateAudioButton(bool playing)
    {
        btnReadAudio.Text = playing ? TourGuideApp.Resources.Languages.AppLang.StopAudio : TourGuideApp.Resources.Languages.AppLang.ListenAudio;
        btnReadAudio.BackgroundColor = playing ? Colors.Black : Color.FromArgb("#F39C12");
    }

    private void OnPopupLangChanged(object sender, EventArgs e)
    {
        string lang = PopupLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };
        Preferences.Set("AppLanguage", lang);

        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
            UpdateAudioButton(false);
        }
    }

    // =================================================================
    // 🌟 4. SỰ KIỆN: CHỈ ĐƯỜNG TRÊN GOOGLE MAPS NGOÀI
    // =================================================================
    private async void OnNavigationClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;
        await Microsoft.Maui.ApplicationModel.Map.OpenAsync(_temporaryPoi.Latitude, _temporaryPoi.Longitude, new MapLaunchOptions { Name = _temporaryPoi.CurrentName });
    }

    // =================================================================
    // 🌟 5. SỰ KIỆN: CHUYỂN QUA BẢN ĐỒ CỦA APP
    // =================================================================
    private async void OnViewOnMapClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;

        // 1. Tắt audio nếu đang rên rỉ
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
        }

        // 2. Dọn dẹp đóng Popup lại
        favoriteDetailPopup.IsOpen = false;

        // 3. Gửi "Mật thư" (Tọa độ) qua cho trang MapPage
        Preferences.Set("TargetPoiLat", _temporaryPoi.Latitude);
        Preferences.Set("TargetPoiLon", _temporaryPoi.Longitude);

        // 4. Đá văng khách sang Tab Bản đồ (MapPage)
        await Shell.Current.GoToAsync("//MapPage");
    }

    // =================================================================
    // 🌟 6. SỰ KIỆN MỚI: XÓA YÊU THÍCH NGAY TRONG POPUP 🌟
    // =================================================================
    private async void OnPopupRemoveFavClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;

        // Xác nhận lại với khách hàng (Sử dụng song ngữ cho oai)
        bool isConfirm = await DisplayAlert(
            TourGuideApp.Resources.Languages.AppLang.SetTitle ?? "Xác nhận",
            $"Bạn có chắc muốn bỏ '{_temporaryPoi.CurrentName}' khỏi danh sách?",
            "OK", "Cancel");

        if (isConfirm)
        {
            // 1. Tắt audio nếu đang đọc
            if (_isAudioPlaying)
            {
                _narrationEngine.Stop();
                _isAudioPlaying = false;
            }

            // 2. Xóa khỏi Database
            _temporaryPoi.IsFavorite = false;
            await _dbService.UpdatePOIAsync(_temporaryPoi);

            // 3. Đóng popup
            favoriteDetailPopup.IsOpen = false;

            // 4. Tải lại danh sách bên ngoài để nó biến mất
            await LoadFavoritesAsync();
        }
    }
}