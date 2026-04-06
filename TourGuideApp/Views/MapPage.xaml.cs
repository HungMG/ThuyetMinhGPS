using Microsoft.Maui.Controls;
using Mapsui.UI.Maui;
using Mapsui.Projections;
using TourGuideApp.Services;
using TourGuideApp.Models;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using BruTile.Cache;
using BruTile.Predefined;
using Mapsui.Tiling.Layers;
using System.IO;

namespace TourGuideApp.Views;

public partial class MapPage : ContentPage
{
    private DatabaseService _dbService;
    private NarrationEngine _narrationEngine = new NarrationEngine();
    private POI _temporaryPoi;

    private int _currentTourId = -1;
    private double _targetLat = 0;
    private double _targetLon = 0;
    private bool _isAudioPlaying = false;
    private GeofenceEngine _geofenceEngine = new GeofenceEngine();
    private IDispatcherTimer _geofenceTimer;


    // 🌟 VŨ KHÍ MỚI: Tự tạo một cây cờ riêng làm GPS thay cho cái chấm mặc định bị lỗi
    private Pin _myLocationPin;

    public MapPage(int tourId = -1)
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        _currentTourId = tourId;

        if (tourMap.Map == null) tourMap.Map = new Mapsui.Map();

        // =======================================================
        // 🌟 BÍ THUẬT BẢN ĐỒ OFFLINE: TỰ ĐỘNG LƯU VÀO CACHE 🌟
        // =======================================================
        // 1. Tạo thư mục giấu ảnh bản đồ vào sâu trong máy
        string cacheDir = Path.Combine(FileSystem.AppDataDirectory, "MapCache");
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }

        // 2. Cài đặt bộ nhớ: Lưu file ảnh với thời hạn 1 năm (365 ngày)
        var fileCache = new FileCache(cacheDir, "tile", new TimeSpan(365, 0, 0, 0));

        // 3. Khởi tạo bản đồ có gắn màng lọc Cache
        var tileSource = KnownTileSources.Create(KnownTileSource.OpenStreetMap, persistentCache: fileCache);
        var tileLayer = new TileLayer(tileSource) { Name = "OfflineOSM" };

        // 4. Thêm vào giao diện (Thay thế cho dòng CreateTileLayer cũ)
        tourMap.Map.Layers.Add(tileLayer);
        // =======================================================

        tourMap.IsZoomButtonVisible = false;
        tourMap.IsNorthingButtonVisible = false;

        // ❌ SA THẢI TÍNH NĂNG GPS MẶC ĐỊNH BỊ LỖI CỦA MAPSUI
        tourMap.MyLocationLayer.Enabled = false;
        tourMap.MyLocationEnabled = false;
    }

    // Nhớ khai báo cái biến Timer này ở trên cùng của class MapPage nha sếp:
    // private IDispatcherTimer _geofenceTimer;

    protected override async void OnAppearing()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        base.OnAppearing();

        poiDetailPopup.IsOpen = false;

        string savedLang = Preferences.Get("AppLanguage", "vi");
        PopupLangPicker.SelectedIndexChanged -= OnPopupLangChanged;
        PopupLangPicker.SelectedIndex = savedLang switch { "en" => 1, "zh" => 2, "ko" => 3, "ja" => 4, _ => 0 };
        PopupLangPicker.SelectedIndexChanged += OnPopupLangChanged;

        _currentTourId = Preferences.Get("TargetTourId", -1);
        _targetLat = Preferences.Get("TargetPoiLat", 0.0);
        _targetLon = Preferences.Get("TargetPoiLon", 0.0);

        Preferences.Remove("TargetTourId");
        Preferences.Remove("TargetPoiLat");
        Preferences.Remove("TargetPoiLon");

        _ = Task.Run(async () =>
        {
            await Task.Delay(300);
            await FilterAndShowPins("");
            await GetCurrentLocationAsync();
        });

        // ==========================================
        // 🌟 CẮM ĐIỆN CHO RADAR GEOFENCE CHẠY 🌟
        // ==========================================
        StartGeofenceTracker();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
        }

        // ==========================================
        // 🌟 RÚT ĐIỆN RADAR KHI THOÁT RA ĐỂ ĐỠ HAO PIN 🌟
        // ==========================================
        _geofenceTimer?.Stop();
    }

    private void StartGeofenceTracker()
    {
        // Nếu radar đang chạy rồi thì thôi, không bật chồng lên nhau
        if (_geofenceTimer != null && _geofenceTimer.IsRunning) return;

        _geofenceTimer = Application.Current.Dispatcher.CreateTimer();
        _geofenceTimer.Interval = TimeSpan.FromSeconds(5); // Cứ 5 giây quét GPS 1 lần
        _geofenceTimer.Tick += async (s, e) =>
        {
            try
            {
                // Lấy vị trí hiện tại của khách
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(2));
                var location = await Geolocation.GetLocationAsync(request);

                // GỌI CỖ MÁY CỦA SẾP RA HOẠT ĐỘNG
                // LƯU Ý: Chữ "_allPOIs" là tui ví dụ cái danh sách các điểm trên bản đồ của sếp. 
                // Nếu biến của sếp tên khác (như _danhSachPoi) thì sếp đổi lại cho đúng nha!
                if (location != null)
                {
                    // Gọi hàm kiểm tra sếp đã viết lúc nãy
                    // Giả sử sếp lấy danh sách POI từ Database
                    var danhSachPoi = await _dbService.GetAllPOIsAsync();
                    await CheckGeofenceAndPlayAudio(location, danhSachPoi);
                }
            }
            catch (Exception ex)
            {
                // Khách chưa bật GPS hoặc lỗi mạng thì im lặng bỏ qua, không làm văng App
                Console.WriteLine($"[Lỗi Radar Geofence]: {ex.Message}");
            }
        };

        // Bắt đầu đếm nhịp!
        _geofenceTimer.Start();
    }

    // =========================================================
    // 🌟 LUÔN LUÔN HIỂN THỊ TẤT CẢ POI LÊN BẢN ĐỒ
    // =========================================================
    private async Task FilterAndShowPins(string keyword)
    {
        var allData = await _dbService.GetAllPOIsAsync();
        if (allData == null) return;

        var filtered = allData.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
            filtered = filtered.Where(p => p.CurrentName.ToLower().Contains(keyword.ToLower()));

        var resultList = filtered.ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            tourMap.Pins.Clear();
            foreach (var poi in resultList)
            {
                var pin = new Pin(tourMap)
                {
                    Position = new Position(poi.Latitude, poi.Longitude),
                    Label = poi.CurrentName,
                    Tag = poi,
                    Color = Colors.Red
                };
                tourMap.Pins.Add(pin);
            }

            // 🌟 QUAN TRỌNG: Nạp lại Cây cờ GPS sau khi đã xóa hết Pins cũ
            if (_myLocationPin != null)
            {
                tourMap.Pins.Add(_myLocationPin);
            }

            tourMap.Refresh();

            if (_targetLat != 0 && _targetLon != 0)
            {
                var targetPoi = resultList.FirstOrDefault(p => p.Latitude == _targetLat && p.Longitude == _targetLon);
                if (targetPoi != null)
                {
                    FocusOnLocation(_targetLat, _targetLon);
                    ShowPoiPopup(targetPoi);
                }
            }
            else if (_currentTourId != -1)
            {
                var firstPoiOfTour = resultList.FirstOrDefault(p => p.TourId == _currentTourId);
                if (firstPoiOfTour != null) FocusOnLocation(firstPoiOfTour.Latitude, firstPoiOfTour.Longitude);
            }
        });
    }

    // =========================================================
    // 🌟 TÍNH TOÁN VÀ BÔI XANH ĐIỂM GẦN NHẤT
    // =========================================================
    private void HighlightClosestPin(Location userLocation)
    {
        if (tourMap.Pins == null || !tourMap.Pins.Any()) return;

        Pin closestPin = null;
        double minDistance = double.MaxValue;

        foreach (var pin in tourMap.Pins)
        {
            // Bỏ qua không tính toán với chính cây cờ GPS của mình
            if (pin == _myLocationPin) continue;

            pin.Color = Colors.Red;
            pin.Scale = 1.0f;

            if (pin.Tag is POI poi)
            {
                var poiLoc = new Location(poi.Latitude, poi.Longitude);
                double distance = Location.CalculateDistance(userLocation, poiLoc, DistanceUnits.Kilometers);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPin = pin;
                }
            }
        }

        if (closestPin != null)
        {
            closestPin.Color = Colors.Green;
            closestPin.Scale = 1.3f;
        }
    }

    // =========================================================
    // 🌟 HỆ THỐNG ĐỊNH VỊ MỚI: DÙNG CỜ PIN LÀM CHẤM GPS
    // =========================================================
    private async Task GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.GetLastKnownLocationAsync() ??
                               await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));

                if (location != null)
                {
                    UpdateGPSPin(location);

                    if (_currentTourId == -1 && _targetLat == 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() => FocusOnLocation(location.Latitude, location.Longitude));
                    }
                }

                Geolocation.LocationChanged -= Geolocation_LocationChanged;
                Geolocation.LocationChanged += Geolocation_LocationChanged;

                await Geolocation.StartListeningForegroundAsync(new GeolocationListeningRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5)));
            }
        }
        catch { }
    }

    private void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        UpdateGPSPin(e.Location);
    }
    private async Task CheckGeofenceAndPlayAudio(Location userLocation, List<POI> danhSachPoi)
    {
        // 1. Kiểm tra công tắc "Tự động đọc" trong Cài đặt
        bool isAutoAudio = Preferences.Get("AutoAudioEnabled", true);
        if (!isAutoAudio || _isAudioPlaying) return;

        // 2. DÙNG CỖ MÁY CỦA SẾP ĐỂ TÌM ĐIỂM CHUẨN NHẤT
        // Nó tự lo hết vụ khoảng cách, bán kính, chống lặp và độ ưu tiên!
        POI closestPoi = _geofenceEngine.GetBestPOIToTrigger(userLocation.Latitude, userLocation.Longitude, danhSachPoi);

        // 3. Nếu tìm được điểm -> ĐỌC THUYẾT MINH
        if (closestPoi != null)
        {
            _isAudioPlaying = true;

            // Cập nhật lại thời gian vừa phát cho cỗ máy nó nhớ (Để lần sau nó tính Cooldown)
            closestPoi.LastPlayedTime = DateTime.Now;
            await _dbService.UpdatePOIAsync(closestPoi); // Lưu vào Database luôn cho chắc

            // Lấy ngôn ngữ hiện tại của App
            string currentLang = Preferences.Get("AppLanguage", "vi");
            string textToRead = currentLang switch
            {
                "en" => closestPoi.Description_EN,
                "zh" => closestPoi.Description_ZH,
                "ko" => closestPoi.Description_KO,
                "ja" => closestPoi.Description_JA,
                _ => closestPoi.Description_VI
            };

            // Đọc tên địa điểm + Mô tả
            await _narrationEngine.SpeakAsync($"{closestPoi.CurrentName}. {textToRead}", currentLang);

            _isAudioPlaying = false;
        }
    }

    // 🌟 HÀM CẬP NHẬT TỌA ĐỘ CHO CÂY CỜ GPS XANH DƯƠNG
    private void UpdateGPSPin(Location location)
    {
        if (location == null || location.Latitude == 0 || location.Longitude == 0) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Nếu chưa có cờ thì tạo mới cờ màu xanh dương
            if (_myLocationPin == null)
            {
                _myLocationPin = new Pin(tourMap)
                {
                    Label = "📍 Bạn đang ở đây",
                    Color = Colors.Blue,
                    Scale = 1.2f
                };
            }

            // Ép vào mảng Pins nếu nó bị rớt ra ngoài
            if (!tourMap.Pins.Contains(_myLocationPin))
            {
                tourMap.Pins.Add(_myLocationPin);
            }

            // Gắn tọa độ mới cho cờ GPS
            _myLocationPin.Position = new Position(location.Latitude, location.Longitude);

            // Bôi xanh lá cây cho POI gần nhất
            HighlightClosestPin(location);

            tourMap.Refresh();
        });
    }

    // --- CÁC HÀM CŨ ĐƯỢC GIỮ NGUYÊN BÊN DƯỚI NÀY ---
    private void FocusOnLocation(double lat, double lon)
    {
        var point = SphericalMercator.FromLonLat(lon, lat);

        MainThread.BeginInvokeOnMainThread(() => {
            tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(point.x, point.y));
            tourMap.Map?.Navigator?.ZoomTo(2);
            tourMap.Refresh();
        });
    }

    private void ShowPoiPopup(POI poi)
    {
        _temporaryPoi = poi;
        lblPoiName.Text = poi.CurrentName;
        lblPoiDescription.Text = poi.CurrentDescription;
        lblPoiAddress.Text = $"Tọa độ: {poi.Latitude:F4}, {poi.Longitude:F4}";
        lblFavoriteIcon.Text = poi.IsFavorite ? "❤️" : "🤍";
        imgPoiImage.Source = !string.IsNullOrEmpty(poi.ImageUrl) ? ImageSource.FromUri(new Uri(poi.FullImageUrl)) : "img_default_poi.png";

        btnReadAudio.Text = TourGuideApp.Resources.Languages.AppLang.ListenAudio;
        btnReadAudio.BackgroundColor = Color.FromArgb("#F39C12");
        _isAudioPlaying = false;

        poiDetailPopup.IsOpen = true;
    }

    private void TourMap_PinClicked(object sender, PinClickedEventArgs e)
    {
        e.Handled = true;
        if (e.Pin.Tag is POI poi) ShowPoiPopup(poi);
    }

    private void OnCenterLocationTapped(object sender, TappedEventArgs e)
    {
        // Nhảy thẳng tới vị trí của Cây cờ GPS mới thay vì dùng Layer cũ
        if (_myLocationPin != null)
            FocusOnLocation(_myLocationPin.Position.Latitude, _myLocationPin.Position.Longitude);
    }

    private async void OnReadDescriptionClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;
        if (_isAudioPlaying) { _narrationEngine.Stop(); _isAudioPlaying = false; UpdateAudioButton(false); return; }

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

    private async void OnFavoriteTapped(object sender, TappedEventArgs e)
    {
        if (_temporaryPoi == null) return;
        _temporaryPoi.IsFavorite = !_temporaryPoi.IsFavorite;
        lblFavoriteIcon.Text = _temporaryPoi.IsFavorite ? "❤️" : "🤍";

        var frame = sender as Frame;
        if (frame != null)
        {
            await frame.ScaleTo(1.2, 100);
            await frame.ScaleTo(1.0, 100);
        }
        await _dbService.UpdatePOIAsync(_temporaryPoi);
    }

    private async void OnNavigationClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;
        await Microsoft.Maui.ApplicationModel.Map.OpenAsync(_temporaryPoi.Latitude, _temporaryPoi.Longitude, new MapLaunchOptions { Name = _temporaryPoi.CurrentName });
    }

    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        mapSearchBar.Unfocus();
        await FilterAndShowPins(mapSearchBar.Text?.ToLower() ?? "");
    }

    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue)) await FilterAndShowPins("");
    }
}