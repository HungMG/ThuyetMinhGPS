using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using TourGuideApp.Models;
using TourGuideApp.Services;
using TourGuideApp.Views;
using System.Linq;
using System.Globalization;

namespace TourGuideApp;

public partial class MainPage : ContentPage
{
    private ApiService _apiService;
    private DatabaseService _dbService;
    private NarrationEngine _narrationEngine = new NarrationEngine();
    private Location _userLocation;
    // Cục phanh dùng cho thanh tìm kiếm
    private CancellationTokenSource _searchCts;

    private List<POI> _allPois = new List<POI>();
    private List<Tour> _allTours = new List<Tour>();

    private bool _isDataLoaded = false;

    // 🌟 BIẾN QUẢN LÝ TRẠNG THÁI NÚT AUDIO
    private Frame _currentPlayingFrame;
    private Label _currentPlayingLabel;
    private bool _isAudioPlaying = false;

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        _dbService = new DatabaseService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // TÌM VÀ SỬA KHÚC NÀY:
        if (NarrationLangPicker.SelectedIndex == -1)
        {
            // Ưu tiên lấy ngôn ngữ Audio đã lưu, nếu chưa có thì lấy ngôn ngữ của App
            string defaultLang = Preferences.Get("AppLanguage", "vi");
            string currentTTSLang = Preferences.Get("TTSLanguage", defaultLang);
            NarrationLangPicker.SelectedIndex = currentTTSLang switch { "en" => 1, "zh" => 2, "ko" => 3, "ja" => 4, _ => 0 };
        }

        if (!_isDataLoaded)
        {
            await LoadDataAsync();
            _isDataLoaded = true;
        }
    }

    // 🌟 HÀM RESET NÚT AUDIO VỀ TRẠNG THÁI BAN ĐẦU
    private void ResetAudioButton()
    {
        if (_currentPlayingFrame != null && _currentPlayingLabel != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _currentPlayingLabel.Text = "▶";
                _currentPlayingFrame.BackgroundColor = Color.FromArgb("#C0502E");
            });
        }
    }

    // Tắt tiếng khi chuyển sang tab khác
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
            ResetAudioButton();
        }
    }

    private void OnNarrationLangChanged(object sender, EventArgs e)
    {
        string selectedLang = NarrationLangPicker.SelectedIndex switch
        {
            1 => "en",
            2 => "zh",
            3 => "ko",
            4 => "ja",
            _ => "vi"
        };

        Preferences.Set("TTSLanguage", selectedLang);

        // Nếu đổi ngôn ngữ lúc đang đọc thì ép nó nín
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
            ResetAudioButton();
        }
    }

    private async Task LoadDataAsync()
    {
        var danhSachTour = await _dbService.GetAllToursAsync();
        if (danhSachTour != null) _allTours = danhSachTour;

        _allPois = await _dbService.GetAllPOIsAsync() ?? new List<POI>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            tourListView.ItemsSource = null;
            tourListView.ItemsSource = _allTours;
            BindableLayout.SetItemsSource(poiStackLayout, _allPois);
        });

        await GetUserLocationAsync();

        if (_userLocation != null && _allPois.Any())
        {
            foreach (var poi in _allPois)
            {
                var poiLoc = new Location(poi.Latitude, poi.Longitude);
                poi.DistanceFromUser = Location.CalculateDistance(_userLocation, poiLoc, DistanceUnits.Kilometers);
            }

            _allPois = _allPois.OrderBy(p => p.DistanceFromUser).ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                BindableLayout.SetItemsSource(poiStackLayout, _allPois);
            });
        }
    }

    private async Task GetUserLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                _userLocation = await Geolocation.GetLastKnownLocationAsync();
                if (_userLocation == null) _userLocation = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3)));
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }

    // 🌟 1. MÁY LỌC ĐA NGÔN NGỮ CHO CẢ TOUR VÀ POI
    private void FilterData(string keyword)
    {
        string searchWord = keyword?.ToLower() ?? "";

        // Nếu người dùng xóa hết chữ -> Trả lại toàn bộ danh sách gốc
        if (string.IsNullOrWhiteSpace(searchWord))
        {
            tourListView.ItemsSource = _allTours;
            BindableLayout.SetItemsSource(poiStackLayout, _allPois);
            return;
        }

        // 🔍 LỌC ĐỊA ĐIỂM (POI): Tìm trong Tên HOẶC Mô tả theo đúng ngôn ngữ đang mở
        var filteredPois = _allPois.Where(p =>
            (!string.IsNullOrEmpty(p.CurrentName) && p.CurrentName.ToLower().Contains(searchWord)) ||
            (!string.IsNullOrEmpty(p.CurrentDescription) && p.CurrentDescription.ToLower().Contains(searchWord))
        ).ToList();

        // 🔍 LỌC LỘ TRÌNH (TOUR): Tìm trong Tên HOẶC Mô tả theo đúng ngôn ngữ đang mở
        var filteredTours = _allTours.Where(t =>
            (!string.IsNullOrEmpty(t.CurrentName) && t.CurrentName.ToLower().Contains(searchWord)) ||
            (!string.IsNullOrEmpty(t.CurrentDescription) && t.CurrentDescription.ToLower().Contains(searchWord))
        ).ToList();

        // Đẩy kết quả đã lọc lên màn hình
        tourListView.ItemsSource = filteredTours;
        BindableLayout.SetItemsSource(poiStackLayout, filteredPois);
    }

    // 🌟 2. SỰ KIỆN GÕ CHỮ (Gõ tới đâu lọc tới đó)
    // 🌟 SỰ KIỆN GÕ CHỮ (ĐÃ LẮP PHANH ABS CHỐNG GIẬT)
    private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        // 1. Hủy ngay cái lệnh tìm kiếm trước đó nếu sếp vẫn đang gõ liên tục
        _searchCts?.Cancel();

        // 2. Tạo một cái phanh mới
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            // 3. Bảo app: "Khoan đã, đợi nửa giây xem sếp có gõ chữ tiếp không"
            await Task.Delay(500, token);

            // 4. Nếu qua 500 mili-giây mà sếp dừng tay -> Mới bắt đầu mang máy lọc ra chạy!
            FilterData(e.NewTextValue);
        }
        catch (TaskCanceledException)
        {
            // Khách vẫn đang gõ chữ tiếp theo -> Máy âm thầm bỏ qua, không báo lỗi.
        }
    }

    // 🌟 3. SỰ KIỆN BẤM KÍNH LÚP TRÊN BÀN PHÍM
    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        FilterData((sender as SearchBar)?.Text);
    }

    private void OnTourCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Tour selectedTour)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var poisInTour = _allPois.Where(p => p.TourId == selectedTour.Id).ToList();

                if (_userLocation != null)
                {
                    poisInTour = poisInTour.OrderBy(p => p.DistanceFromUser).ToList();
                }

                lblPopupTourName.Text = $"📍 {selectedTour.CurrentName}";
                BindableLayout.SetItemsSource(popupPoiStackLayout, poisInTour);

                blackOverlay.IsVisible = true;
                blackOverlay.FadeTo(0.5, 250);

                tourPoiPopup.IsVisible = true;
                tourPoiPopup.TranslateTo(0, 0, 350, Easing.SpringOut);
            });
        }
    }

    private async void OnClosePopupTapped(object sender, TappedEventArgs e)
    {
        await tourPoiPopup.TranslateTo(0, 1000, 250, Easing.CubicIn);
        await blackOverlay.FadeTo(0, 250);
        blackOverlay.IsVisible = false;
    }

    private async void OnPoiCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI selectedPoi)
        {
            tourPoiPopup.TranslationY = 1000;
            blackOverlay.IsVisible = false;

            Preferences.Set("TargetTourId", selectedPoi.TourId);
            Preferences.Set("TargetPoiLat", selectedPoi.Latitude);
            Preferences.Set("TargetPoiLon", selectedPoi.Longitude);

            await Shell.Current.GoToAsync("//MapPage");
        }
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            bool isTourSuccess = await _apiService.SyncToursAsync(_dbService);
            bool isPoiSuccess = await _apiService.SyncPOIsAsync(_dbService);
            if (isTourSuccess || isPoiSuccess) await LoadDataAsync();
        }
        catch { }
        finally { mainRefreshView.IsRefreshing = false; }
    }

    // ==========================================================
    // 🌟 XỬ LÝ AUDIO CHO TOUR BẤM ĐỂ DỪNG/PHÁT
    // ==========================================================
    private async void OnTourPlayAudioTapped(object sender, TappedEventArgs e)
    {
        var frame = sender as Frame;
        var label = frame?.Content as Label;
        if (e.Parameter is not Tour tour) return;

        // 1. Đang phát chính nó -> Bấm để DỪNG
        if (_isAudioPlaying && _currentPlayingFrame == frame)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
            ResetAudioButton();
            return;
        }

        // 2. Dừng audio cũ (nếu có bài khác đang hát)
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            ResetAudioButton();
        }

        // 3. Đổi giao diện nút thành 🛑
        _currentPlayingFrame = frame;
        _currentPlayingLabel = label;
        _isAudioPlaying = true;

        if (label != null) label.Text = "🛑";
        if (frame != null) frame.BackgroundColor = Colors.Black;

        // 4. Lấy ngôn ngữ và phát Audio
        string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };

        // Cập nhật lấy full đa ngôn ngữ cho Tour
        string textToRead = lang switch
        {
            "en" => $"{tour.Name_EN}. {tour.Description_EN}",
            "zh" => $"{tour.Name_ZH}. {tour.Description_ZH}",
            "ko" => $"{tour.Name_KO}. {tour.Description_KO}",
            "ja" => $"{tour.Name_JA}. {tour.Description_JA}",
            _ => $"{tour.Name_VI}. {tour.Description_VI}"
        };

        await _narrationEngine.SpeakAsync(textToRead, lang);

        // 5. Đọc xong tự Reset
        _isAudioPlaying = false;
        ResetAudioButton();
    }

    // ==========================================================
    // 🌟 XỬ LÝ AUDIO CHO POI BẤM ĐỂ DỪNG/PHÁT
    // ==========================================================
    private async void OnPoiPlayAudioTapped(object sender, TappedEventArgs e)
    {
        var frame = sender as Frame;
        var label = frame?.Content as Label;
        if (e.Parameter is not POI poi) return;

        // 1. Đang phát chính nó -> Bấm để DỪNG
        if (_isAudioPlaying && _currentPlayingFrame == frame)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
            ResetAudioButton();
            return;
        }

        // 2. Dừng audio cũ
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            ResetAudioButton();
        }

        // 3. Đổi giao diện nút thành 🛑
        _currentPlayingFrame = frame;
        _currentPlayingLabel = label;
        _isAudioPlaying = true;

        if (label != null) label.Text = "🛑";
        if (frame != null) frame.BackgroundColor = Colors.Black;

        // 4. Lấy ngôn ngữ và phát Audio
        string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };

        // Cập nhật lấy full đa ngôn ngữ cho POI
        string textToRead = lang switch
        {
            "en" => $"{poi.Name_EN}. {poi.Description_EN}",
            "zh" => $"{poi.Name_ZH}. {poi.Description_ZH}",
            "ko" => $"{poi.Name_KO}. {poi.Description_KO}",
            "ja" => $"{poi.Name_JA}. {poi.Description_JA}",
            _ => $"{poi.Name_VI}. {poi.Description_VI}"
        };

        await _narrationEngine.SpeakAsync(textToRead, lang);

        // 5. Đọc xong tự Reset
        _isAudioPlaying = false;
        ResetAudioButton();
    }

    private async void OnNavigateTourClicked(object sender, EventArgs e)
    {
        var poisInTour = BindableLayout.GetItemsSource(popupPoiStackLayout) as List<POI>;

        if (poisInTour == null || !poisInTour.Any()) return;

        try
        {
            var lastPoi = poisInTour.Last();
            string destination = $"{lastPoi.Latitude.ToString(CultureInfo.InvariantCulture)},{lastPoi.Longitude.ToString(CultureInfo.InvariantCulture)}";
            string url = $"https://www.google.com/maps/dir/?api=1&destination={destination}&travelmode=driving";

            if (poisInTour.Count > 1)
            {
                var waypointsList = poisInTour.Take(poisInTour.Count - 1)
                                              .Select(p => $"{p.Latitude.ToString(CultureInfo.InvariantCulture)},{p.Longitude.ToString(CultureInfo.InvariantCulture)}");
                string waypointsStr = string.Join("|", waypointsList);
                url += $"&waypoints={waypointsStr}";
            }

            await Launcher.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể mở bản đồ chỉ đường: " + ex.Message, "OK");
        }
    }
}