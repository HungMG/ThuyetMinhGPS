using Mapsui.UI.Maui;
using Mapsui.Projections;
using TourGuideApp.Services;
using System.Linq;
using TourGuideApp.Models;

namespace TourGuideApp.Views;

public partial class MapPage : ContentPage
{
    private DatabaseService _dbService;
    private List<int> _daDocThuyetMinh = new List<int>();
    private POI _temporaryPoi;
    private NarrationEngine _narrationEngine = new NarrationEngine();

    private int _currentTourId = -1;
    // 🌟 THÊM 2 BIẾN HỨNG TỌA ĐỘ
    private double _targetLat = 0;
    private double _targetLon = 0;

    public MapPage(int tourId = -1)
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        _currentTourId = tourId;

        tourMap.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        tourMap.IsZoomButtonVisible = false;
        tourMap.IsNorthingButtonVisible = false;
        tourMap.IsMyLocationButtonVisible = true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 🌟 MÓC DỮ LIỆU TỪ MAIN PAGE GỬI QUA
        _currentTourId = Preferences.Get("TargetTourId", -1);
        _targetLat = Preferences.Get("TargetPoiLat", 0.0);
        _targetLon = Preferences.Get("TargetPoiLon", 0.0);

        // Đọc xong xóa luôn cho sạch bộ nhớ
        Preferences.Remove("TargetTourId");
        Preferences.Remove("TargetPoiLat");
        Preferences.Remove("TargetPoiLon");

        await FilterAndShowPins("");
        await GetCurrentLocationAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Geolocation.LocationChanged -= Geolocation_LocationChanged;
        Geolocation.StopListeningForeground();
    }

    // =========================================================
    // 👇 PHẦN 1: SỰ KIỆN TÌM KIẾM 👇
    // =========================================================

    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        mapSearchBar.Unfocus();
        string keyword = mapSearchBar.Text?.ToLower() ?? "";
        await FilterAndShowPins(keyword);
    }

    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            await FilterAndShowPins("");
        }
    }

    // =========================================================
    // 👇 PHẦN 2: POPUP CHI TIẾT ĐỊA ĐIỂM 👇
    // =========================================================

    private void TourMap_PinClicked(object sender, PinClickedEventArgs e)
    {
        e.Handled = true;

        if (e.Pin.Tag is POI currentPoi)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblPoiName.Text = currentPoi.CurrentName;
                lblPoiDescription.Text = currentPoi.CurrentDescription;
                lblPoiAddress.Text = $"Tọa độ: {currentPoi.Latitude:F5}, {currentPoi.Longitude:F5}";
                lblFavoriteIcon.Text = currentPoi.IsFavorite ? "❤️" : "🤍";

                if (!string.IsNullOrWhiteSpace(currentPoi.ImageUrl))
                    imgPoiImage.Source = ImageSource.FromUri(new Uri(currentPoi.FullImageUrl));
                else
                    imgPoiImage.Source = "img_default_poi.png";

                _temporaryPoi = currentPoi;
                poiDetailPopup.PeekHeight = 450;
                poiDetailPopup.IsOpen = true;
            });
        }
    }

    private async void OnNavigationClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;

        poiDetailPopup.IsOpen = false;

        try
        {
            var toaDoDich = new Location(_temporaryPoi.Latitude, _temporaryPoi.Longitude);
            var options = new MapLaunchOptions { Name = _temporaryPoi.CurrentName, NavigationMode = NavigationMode.Driving };
            await Microsoft.Maui.ApplicationModel.Map.OpenAsync(toaDoDich, options);
        }
        catch (Exception)
        {
            await DisplayAlert("Lỗi", "Không thể mở ứng dụng bản đồ.", "OK");
        }
    }

    private async void OnReadDescriptionClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;

        var btn = sender as Button;
        if (btn != null) btn.Text = "🔊 Đang đọc...";

        string langCode = Preferences.Get("AppLanguage", "vi");

        string textToRead = langCode switch
        {
            "en" => _temporaryPoi.Description_EN,
            "zh" => _temporaryPoi.Description_ZH,
            "ko" => _temporaryPoi.Description_KO,
            "ja" => _temporaryPoi.Description_JA,
            _ => _temporaryPoi.Description_VI
        };

        string fullSpeech = $"{_temporaryPoi.CurrentName}. {textToRead}";
        await _narrationEngine.SpeakAsync(fullSpeech, langCode);

        if (btn != null) btn.Text = "🎧 Nghe Audio";
    }

    private async Task FilterAndShowPins(string keyword)
    {
        var danhSachPOI = await _dbService.GetAllPOIsAsync();
        if (danhSachPOI == null) return;

        if (_currentTourId != -1)
        {
            danhSachPOI = danhSachPOI.Where(p => p.TourId == _currentTourId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            danhSachPOI = danhSachPOI.Where(p =>
                (p.CurrentName != null && p.CurrentName.ToLower().Contains(keyword)) ||
                (p.CurrentDescription != null && p.CurrentDescription.ToLower().Contains(keyword))
            ).ToList();
        }

        tourMap.Pins.Clear();
        tourMap.PinClicked -= TourMap_PinClicked;
        tourMap.PinClicked += TourMap_PinClicked;

        foreach (var poi in danhSachPOI)
        {
            var pin = new Pin(tourMap)
            {
                Position = new Position(poi.Latitude, poi.Longitude),
                Type = PinType.Pin,
                Label = poi.CurrentName,
                Address = poi.CurrentDescription,
                Color = Colors.Red,
                Tag = poi
            };
            tourMap.Pins.Add(pin);
        }

        if (danhSachPOI.Any())
        {
            // 🌟 1. NẾU CÓ TỌA ĐỘ MỤC TIÊU -> TRƯỢT CAMERA VÀ BẬT POPUP
            if (_targetLat != 0 && _targetLon != 0)
            {
                var toaDoZoom = SphericalMercator.FromLonLat(_targetLon, _targetLat);
                tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoZoom.x, toaDoZoom.y));
                tourMap.Map?.Navigator?.ZoomTo(2);

                // Tự động bật Popup của điểm đó lên luôn
                var targetPoi = danhSachPOI.FirstOrDefault(p => p.Latitude == _targetLat && p.Longitude == _targetLon);
                if (targetPoi != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        lblPoiName.Text = targetPoi.CurrentName;
                        lblPoiDescription.Text = targetPoi.CurrentDescription;
                        lblPoiAddress.Text = $"Tọa độ: {targetPoi.Latitude:F5}, {targetPoi.Longitude:F5}";
                        lblFavoriteIcon.Text = targetPoi.IsFavorite ? "❤️" : "🤍";

                        if (!string.IsNullOrWhiteSpace(targetPoi.ImageUrl))
                            imgPoiImage.Source = ImageSource.FromUri(new Uri(targetPoi.FullImageUrl));
                        else
                            imgPoiImage.Source = "img_default_poi.png";

                        _temporaryPoi = targetPoi;
                        poiDetailPopup.PeekHeight = 450;
                        poiDetailPopup.IsOpen = true;
                    });
                }
            }
            // 🌟 2. NẾU CHỈ XEM TOUR -> TRƯỢT ĐẾN ĐIỂM ĐẦU TIÊN CỦA TOUR
            else if (_currentTourId != -1)
            {
                var firstPoi = danhSachPOI.First();
                var toaDoZoom = SphericalMercator.FromLonLat(firstPoi.Longitude, firstPoi.Latitude);
                tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoZoom.x, toaDoZoom.y));
                tourMap.Map?.Navigator?.ZoomTo(2);
            }
        }
    }

    // =========================================================
    // 👇 PHẦN 3: GPS VÀ GEOFENCE (THUYẾT MINH SONG NGỮ) 👇
    // =========================================================

    private async Task GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                tourMap.MyLocationEnabled = true;

                var location = await Geolocation.GetLastKnownLocationAsync();
                if (location == null)
                {
                    location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3)));
                }

                // 🌟 ĐÃ SỬA: Ép máy ảnh trượt về chỗ sếp CHỈ KHI sếp không truyền mục tiêu (POI) nào qua
                if (location != null && _currentTourId == -1 && _targetLat == 0 && _targetLon == 0)
                {
                    var toaDoHienTai = Mapsui.Projections.SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                    tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoHienTai.x, toaDoHienTai.y));
                    tourMap.Map?.Navigator?.ZoomTo(2);
                }

                Geolocation.LocationChanged -= Geolocation_LocationChanged;
                Geolocation.LocationChanged += Geolocation_LocationChanged;
                var request = new GeolocationListeningRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                await Geolocation.StartListeningForegroundAsync(request);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi lấy vị trí: {ex.Message}");
        }
    }

    private async void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        if (e.Location == null) return;

        tourMap.MyLocationLayer.UpdateMyLocation(new Position(e.Location.Latitude, e.Location.Longitude));

        HighlightClosestPin(e.Location);

        var allPOIs = await _dbService.GetAllPOIsAsync();

        var poisInRange = allPOIs.Where(poi => {
            var poiLoc = new Location(poi.Latitude, poi.Longitude);
            double distanceMet = Location.CalculateDistance(e.Location, poiLoc, DistanceUnits.Kilometers) * 1000;
            return distanceMet <= poi.TriggerRadius;
        }).ToList();

        if (!poisInRange.Any()) return;

        var bestPOI = poisInRange
            .Where(p => !p.LastPlayedTime.HasValue || (DateTime.Now - p.LastPlayedTime.Value).TotalMinutes >= 30)
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();

        if (bestPOI != null)
        {
            bestPOI.LastPlayedTime = DateTime.Now;
            await _dbService.UpdatePOIAsync(bestPOI);

            string langCode = Preferences.Get("AppLanguage", "vi");
            string textToRead = langCode switch
            {
                "en" => bestPOI.Description_EN,
                "zh" => bestPOI.Description_ZH,
                "ko" => bestPOI.Description_KO,
                "ja" => bestPOI.Description_JA,
                _ => bestPOI.Description_VI
            };

            await _narrationEngine.SpeakAsync($"{bestPOI.CurrentName}. {textToRead}", langCode);
            await DisplayAlert(bestPOI.CurrentName, "Tự động thuyết minh đang phát...", "OK");
        }
    }

    // =========================================================
    // 👇 HÀM XỬ LÝ NÚT TỰ ĐỊNH VỊ VỊ TRÍ 👇
    // =========================================================
    private void OnCenterLocationTapped(object sender, TappedEventArgs e)
    {
        if (tourMap.MyLocationLayer != null && tourMap.MyLocationLayer.MyLocation != null)
        {
            var loc = tourMap.MyLocationLayer.MyLocation;
            var toaDoHienTai = SphericalMercator.FromLonLat(loc.Longitude, loc.Latitude);
            tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoHienTai.x, toaDoHienTai.y));
            tourMap.Map?.Navigator?.ZoomTo(2);
        }
        else
        {
            string langCode = Preferences.Get("AppLanguage", "vi");
            string msg = langCode switch
            {
                "en" => "Finding your location...",
                "zh" => "正在查找您的位置...",
                "ko" => "위치 찾는 중...",
                "ja" => "現在地を検索中...",
                _ => "Đang tìm vị trí của bạn, vui lòng đợi chút nhé!"
            };
            DisplayAlert(langCode == "en" ? "Info" : "Thông báo", msg, "OK");
        }
    }

    // =========================================================
    // 👇 HÀM XỬ LÝ NÚT THẢ TIM (YÊU THÍCH) 👇
    // =========================================================
    private async void OnFavoriteTapped(object sender, TappedEventArgs e)
    {
        if (_temporaryPoi == null) return;

        _temporaryPoi.IsFavorite = !_temporaryPoi.IsFavorite;
        lblFavoriteIcon.Text = _temporaryPoi.IsFavorite ? "❤️" : "🤍";

        var label = sender as Frame;
        if (label != null)
        {
            await label.ScaleTo(1.2, 100);
            await label.ScaleTo(1.0, 100);
        }

        await _dbService.UpdatePOIAsync(_temporaryPoi);
    }

    // =========================================================
    // 👇 HÀM TÌM VÀ ĐỔI MÀU CỜ (PIN) GẦN NHẤT 👇
    // =========================================================
    private void HighlightClosestPin(Location userLocation)
    {
        if (tourMap.Pins == null || !tourMap.Pins.Any()) return;

        Mapsui.UI.Maui.Pin closestPin = null;
        double minDistance = double.MaxValue;

        foreach (var pin in tourMap.Pins)
        {
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
}