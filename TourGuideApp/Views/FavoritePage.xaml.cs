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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        favoriteDetailPopup.IsOpen = false;
        await LoadFavoritesAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
        }
    }

    // =================================================================
    // 🌟 TRẠM KIỂM SOÁT VÉ VÀO CỔNG 🌟
    // =================================================================
    private async Task LoadFavoritesAsync()
    {
        int userId = Preferences.Get("UserId", -1);

        if (userId <= 0)
        {
            // Nếu chưa đăng nhập: Trưng bảng cấm lên
            pnlGuest.IsVisible = true;
            pnlLoggedIn.IsVisible = false;
        }
        else
        {
            // Nếu có vé: Mở cửa cho xem danh sách
            pnlGuest.IsVisible = false;
            pnlLoggedIn.IsVisible = true;

            var danhSachYeuThich = await _dbService.GetFavoritePOIsAsync();
            favoriteListView.ItemsSource = danhSachYeuThich;
        }
    }

    // 🌟 SỰ KIỆN: Khách bấm nút đòi Đăng Nhập
    private void OnLoginButtonClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new LoginPage();
    }

    // =================================================================
    // CÁC HÀM XỬ LÝ CŨ CỦA SẾP GIỮ NGUYÊN HOÀN TOÀN
    // =================================================================
    private async void OnRemoveFavoriteClicked(object sender, EventArgs e)
    {
        var button = sender as ImageButton;
        var poiBiXoa = button?.CommandParameter as POI;

        if (poiBiXoa != null)
        {
            bool xacNhan = await DisplayAlert(
                TourGuideApp.Resources.Languages.AppLang.SetTitle ?? "Xác nhận",
                $"Bạn có chắc muốn bỏ '{poiBiXoa.CurrentName}' khỏi danh sách yêu thích?",
                "Đồng ý", "Hủy");

            if (xacNhan)
            {
                poiBiXoa.IsFavorite = false;
                await _dbService.UpdatePOIAsync(poiBiXoa);
                await LoadFavoritesAsync();
            }
        }
    }

    private void OnFavoriteItemTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI poi)
        {
            _temporaryPoi = poi;
            lblPopupName.Text = poi.CurrentName;
            lblPopupDescription.Text = poi.CurrentDescription;
            imgPopupImage.Source = !string.IsNullOrEmpty(poi.ImageUrl) ? ImageSource.FromUri(new Uri(poi.FullImageUrl)) : "img_default_poi.png";

            string savedLang = Preferences.Get("AppLanguage", "vi");
            PopupLangPicker.SelectedIndexChanged -= OnPopupLangChanged;
            PopupLangPicker.SelectedIndex = savedLang switch { "en" => 1, "zh" => 2, "ko" => 3, "ja" => 4, _ => 0 };
            PopupLangPicker.SelectedIndexChanged += OnPopupLangChanged;

            btnReadAudio.Text = TourGuideApp.Resources.Languages.AppLang.ListenAudio;
            btnReadAudio.BackgroundColor = Color.FromArgb("#F39C12");
            _isAudioPlaying = false;

            favoriteDetailPopup.IsOpen = true;
        }
    }

    private async void OnReadDescriptionClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
            UpdateAudioButton(false);
            return;
        }
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

    private async void OnNavigationClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;
        await Microsoft.Maui.ApplicationModel.Map.OpenAsync(_temporaryPoi.Latitude, _temporaryPoi.Longitude, new MapLaunchOptions { Name = _temporaryPoi.CurrentName });
    }

    private async void OnViewOnMapClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;
        if (_isAudioPlaying) { _narrationEngine.Stop(); _isAudioPlaying = false; }
        favoriteDetailPopup.IsOpen = false;
        Preferences.Set("TargetPoiLat", _temporaryPoi.Latitude);
        Preferences.Set("TargetPoiLon", _temporaryPoi.Longitude);
        await Shell.Current.GoToAsync("//MapPage");
    }

    private async void OnPopupRemoveFavClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;

        bool isConfirm = await DisplayAlert(
            TourGuideApp.Resources.Languages.AppLang.SetTitle ?? "Xác nhận",
            $"Bạn có chắc muốn bỏ '{_temporaryPoi.CurrentName}' khỏi danh sách?",
            "OK", "Cancel");

        if (isConfirm)
        {
            if (_isAudioPlaying) { _narrationEngine.Stop(); _isAudioPlaying = false; }

            _temporaryPoi.IsFavorite = false;
            await _dbService.UpdatePOIAsync(_temporaryPoi);

            favoriteDetailPopup.IsOpen = false;
            await LoadFavoritesAsync();
        }
    }
}