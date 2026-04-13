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
    // Phanh chống giật cho thanh tìm kiếm bản đồ
    private CancellationTokenSource _mapSearchCts;

    private int _currentTourId = -1;
    private double _targetLat = 0;
    private double _targetLon = 0;
    private bool _isAudioPlaying = false;
    private GeofenceEngine _geofenceEngine = new GeofenceEngine();
    private bool _isRadarRunning = false;

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
        string cacheDir = Path.Combine(FileSystem.AppDataDirectory, "MapCache");
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }

        var fileCache = new FileCache(cacheDir, "tile", new TimeSpan(365, 0, 0, 0));
        var tileSource = KnownTileSources.Create(KnownTileSource.OpenStreetMap, persistentCache: fileCache);
        var tileLayer = new TileLayer(tileSource) { Name = "OfflineOSM" };

        tourMap.Map.Layers.Add(tileLayer);

        tourMap.IsZoomButtonVisible = false;
        tourMap.IsNorthingButtonVisible = false;

        tourMap.MyLocationLayer.Enabled = false;
        tourMap.MyLocationEnabled = false;
    }

    protected override async void OnAppearing()
    {
        Console.WriteLine("===============================================");
        Console.WriteLine("[KIỂM TRA] SẾP ĐÃ VÀO TRANG BẢN ĐỒ (MAPPAGE)!!!");
        Console.WriteLine("===============================================");
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        base.OnAppearing();

        poiDetailPopup.IsOpen = false;

        // Lấy ngôn ngữ Audio riêng biệt
        string savedTTSLang = Preferences.Get("TTSLanguage", Preferences.Get("AppLanguage", "vi"));

        PopupLangPicker.SelectedIndexChanged -= OnPopupLangChanged;
        PopupLangPicker.SelectedIndex = savedTTSLang switch { "en" => 1, "zh" => 2, "ko" => 3, "ja" => 4, _ => 0 };
        PopupLangPicker.SelectedIndexChanged += OnPopupLangChanged;

        _currentTourId = Preferences.Get("TargetTourId", -1);
        _targetLat = Preferences.Get("TargetPoiLat", 0.0);
        _targetLon = Preferences.Get("TargetPoiLon", 0.0);

        // Kiểm tra xem có được mở từ QR không
        int qrPoiId = Preferences.Get("QRScannedPoiId", -1);

        Preferences.Remove("TargetTourId");
        Preferences.Remove("TargetPoiLat");
        Preferences.Remove("TargetPoiLon");
        Preferences.Remove("QRScannedPoiId"); // Xóa luôn sau khi đọc

        _ = Task.Run(async () =>
        {
            await Task.Delay(300);
            await FilterAndShowPins("");
            await GetCurrentLocationAsync();

            // =========================================================
            // 🌟 XỬ LÝ KHI MỞ TỪ QR: TÌM POI VÀ MỞ BOTTOM SHEET
            // =========================================================
            // Ưu tiên QR trước (nếu quét QR thì bỏ qua TargetPoiLat/Lon)
            if (qrPoiId != -1)
            {
                Console.WriteLine($"[MAPPAGE] Mở từ QR, POI Id = {qrPoiId}");
                var allPois = await _dbService.GetAllPOIsAsync();
                var targetPoi = allPois?.FirstOrDefault(p => p.Id == qrPoiId);

                if (targetPoi != null)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        // 1. Zoom bản đồ tới vị trí POI
                        FocusOnLocation(targetPoi.Latitude, targetPoi.Longitude);
                        // 2. Mở bottom sheet (popup)
                        ShowPoiPopup(targetPoi);

                        // 3. 🔊 TỰ ĐỘNG ĐỌC THUYẾT MINH NGAY SAU KHI QUÉT QR
                        // Đợi 1 nhịp cho popup + picker render xong rồi mới phát.
                        await Task.Delay(400);
                        await AutoPlayNarrationAsync(targetPoi);
                    });
                }
                else
                {
                    Console.WriteLine($"[MAPPAGE] Không tìm thấy POI Id={qrPoiId} trong database local.");
                }
            }
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

        // Tắt công tắc để cái vòng lặp While nó tự dừng
        _isRadarRunning = false;
    }


    // =========================================================
    // 🌟 ĐỘNG CƠ RADAR V8 (ĐÃ FIX LỖI _allPois)
    // =========================================================
    // =========================================================
    // 🌟 ĐỘNG CƠ RADAR V8 (BẢN CLEAN - CHỈ LOG ĐIỂM GẦN NHẤT)
    // =========================================================
    private void StartGeofenceTracker()
    {
        if (_isRadarRunning) return;
        _isRadarRunning = true;

        Console.WriteLine("[RADAR] Đã dọn dẹp code! Động cơ chạy ngầm siêu mượt...");

        Task.Run(async () =>
        {
            while (_isRadarRunning)
            {
                try // Vẫn phải giữ cái khiên này để khách mất sóng GPS app không bị văng nha sếp!
                {
                    if (_isAudioPlaying) continue;

                    var location = await GetFastLocation();
                    if (location == null) continue;

                    var danhSachPoi = await _dbService.GetAllPOIsAsync();
                    if (danhSachPoi == null || !danhSachPoi.Any()) continue;

                    // ---------------------------------------------------------
                    // 🌟 TÌM VÀ BÁO CÁO ĐÚNG 1 ĐIỂM GẦN NHẤT LÊN OUTPUT
                    // ---------------------------------------------------------
                    var diemGanNhat = danhSachPoi.OrderBy(p =>
                        _geofenceEngine.CalculateHaversineDistance(location.Latitude, location.Longitude, p.Latitude, p.Longitude)).FirstOrDefault();

                    if (diemGanNhat != null)
                    {
                        double khoangCach = _geofenceEngine.CalculateHaversineDistance(location.Latitude, location.Longitude, diemGanNhat.Latitude, diemGanNhat.Longitude);

                        // Tính toán Cooldown
                        string trangThaiCooldown = "Sẵn sàng hát 🟢";
                        if (diemGanNhat.LastPlayedTime.HasValue)
                        {
                            double phutDaQua = (DateTime.Now - diemGanNhat.LastPlayedTime.Value).TotalMinutes;
                            if (phutDaQua < 5) trangThaiCooldown = $"Đang nghỉ mệt ({phutDaQua:F1}/5 phút) 🔴";
                        }

                        // In ra đúng 1 dòng siêu gọn
                        Console.WriteLine($"[RADAR] Gần nhất: {diemGanNhat.CurrentName} ({khoangCach:F0}m) | {trangThaiCooldown}");
                    }
                    // ---------------------------------------------------------

                    // Cỗ máy xét duyệt xem có đủ điều kiện hát không
                    var bestPoi = _geofenceEngine.GetBestPOIToTrigger(location.Latitude, location.Longitude, danhSachPoi);

                    // 🌟 PHẦN CẬP NHẬT LOGIC ĐỌC THOẠI TRONG ĐỘNG CƠ V8
                    // Sếp tìm đến khúc "Bước 3" trong vòng lặp While của StartGeofenceTracker

                    if (bestPoi == null)
                    {
                        Console.WriteLine("[RADAR] ❌ Không có POI nào trong bán kính, hoặc bị Cooldown.");
                    }
                    else
                    {
                        Console.WriteLine($"[RADAR] 3. TÌM THẤY MỤC TIÊU: {bestPoi.CurrentName}! Chuẩn bị hát!");

                        _isAudioPlaying = true;
                        bestPoi.LastPlayedTime = DateTime.Now;
                        await _dbService.UpdatePOIAsync(bestPoi);

                        // 1. Lấy ngôn ngữ hiện tại của hệ thống
                        string currentLang = Preferences.Get("AppLanguage", "vi");

                        // 2. TẠO CÂU DẪN TỰ NHIÊN (Intro) tùy theo ngôn ngữ
                        string intro = currentLang switch
                        {
                            "en" => $"You are near {bestPoi.Name_EN}. Let me introduce you to this place: ",
                            "zh" => $"您正在靠近 {bestPoi.Name_ZH}。让我为您介绍一下这个地方：",
                            "ko" => $"{bestPoi.Name_KO} 근처에 있습니다. 이곳을 소개해 드리겠습니다: ",
                            "ja" => $"{bestPoi.Name_JA} に近づいています。ここを紹介しましょう：",
                            _ => $"Bạn đang ở gần {bestPoi.Name_VI}. Sau đây là phần giới thiệu về địa điểm này: "
                        };

                        // 3. Lấy nội dung mô tả tương ứng
                        string content = currentLang switch
                        {
                            "en" => bestPoi.Description_EN,
                            "zh" => bestPoi.Description_ZH,
                            "ko" => bestPoi.Description_KO,
                            "ja" => bestPoi.Description_JA,
                            _ => bestPoi.Description_VI
                        };

                        // 4. Nối chúng lại và cho chị Google "lên mic"
                        string fullText = intro + content;
                        Console.WriteLine($"[RADAR] 4. Bắt đầu thuyết minh: {fullText}");

                        await _narrationEngine.SpeakAsync(fullText, currentLang);

                        Console.WriteLine($"[RADAR] 5. Đã thuyết minh xong!");
                        _isAudioPlaying = false;
                    }
                }
                catch (Exception)
                {
                    // Lỗi mạng, lỗi hệ điều hành thì im lặng ngậm bồ hòn làm ngọt, chờ 5s sau quét lại
                }

                await Task.Delay(5000);
            }
        });
    }

    // ==========================================================
    // 🌟 HÀM TRỢ THỦ: LẤY VỊ TRÍ GPS SIÊU TỐC
    // ==========================================================
    private async Task<Location> GetFastLocation()
    {
        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            if (location == null)
            {
                var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                location = await Geolocation.GetLocationAsync(request);
            }
            return location;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Lỗi lấy GPS]: {ex.Message}");
            return null;
        }
    }

    private async Task CheckGeofenceAndPlayAudio(Location userLocation, List<POI> danhSachPoi)
    {
        bool isAutoAudio = Preferences.Get("AutoAudioEnabled", true);
        if (!isAutoAudio || _isAudioPlaying) return;

        POI closestPoi = _geofenceEngine.GetBestPOIToTrigger(userLocation.Latitude, userLocation.Longitude, danhSachPoi);

        if (closestPoi != null)
        {
            _isAudioPlaying = true;

            closestPoi.LastPlayedTime = DateTime.Now;
            await _dbService.UpdatePOIAsync(closestPoi);

            // 1. Lấy ngôn ngữ dành riêng cho Loa
            string currentAudioLang = Preferences.Get("TTSLanguage", Preferences.Get("AppLanguage", "vi"));

            // 2. Gom cả TÊN và MÔ TẢ dịch theo đúng ngôn ngữ của Loa
            string textToRead = currentAudioLang switch
            {
                "en" => $"{closestPoi.Name_EN}. {closestPoi.Description_EN}",
                "zh" => $"{closestPoi.Name_ZH}. {closestPoi.Description_ZH}",
                "ko" => $"{closestPoi.Name_KO}. {closestPoi.Description_KO}",
                "ja" => $"{closestPoi.Name_JA}. {closestPoi.Description_JA}",
                _ => $"{closestPoi.Name_VI}. {closestPoi.Description_VI}"
            };

            // 3. Truyền đúng biến currentAudioLang vào cho chị Google đọc
            await _narrationEngine.SpeakAsync(textToRead, currentAudioLang);

            _isAudioPlaying = false;
        }
    }

    // 🌟 SỬA LẠI HÀM LỌC: Bao trọn gói Tên + Mô tả và chống văng App
    private async Task FilterAndShowPins(string keyword)
    {
        var allData = await _dbService.GetAllPOIsAsync();
        if (allData == null) return;

        var resultList = allData.AsEnumerable();

        // Nếu có gõ chữ thì mới lọc, không thì lấy hết
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.ToLower();
            resultList = resultList.Where(p =>
                (!string.IsNullOrEmpty(p.CurrentName) && p.CurrentName.ToLower().Contains(keyword)) ||
                (!string.IsNullOrEmpty(p.CurrentDescription) && p.CurrentDescription.ToLower().Contains(keyword))
            );
        }

        var finalPins = resultList.ToList();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            tourMap.Pins.Clear();
            foreach (var poi in finalPins)
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

            if (_myLocationPin != null)
            {
                tourMap.Pins.Add(_myLocationPin);
            }

            tourMap.Refresh();

            // Xử lý zoom tới Pin như cũ
            if (_targetLat != 0 && _targetLon != 0)
            {
                var targetPoi = finalPins.FirstOrDefault(p => p.Latitude == _targetLat && p.Longitude == _targetLon);
                if (targetPoi != null)
                {
                    FocusOnLocation(_targetLat, _targetLon);
                    ShowPoiPopup(targetPoi);
                }
            }
            else if (_currentTourId != -1)
            {
                var firstPoiOfTour = finalPins.FirstOrDefault(p => p.TourId == _currentTourId);
                if (firstPoiOfTour != null) FocusOnLocation(firstPoiOfTour.Latitude, firstPoiOfTour.Longitude);
            }
        });
    }

    // 🌟 SỬA LẠI SỰ KIỆN NÚT TÌM KIẾM
    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        mapSearchBar.Unfocus();
        await FilterAndShowPins(mapSearchBar.Text ?? "");
    }

    // 🌟 SỬA LẠI SỰ KIỆN GÕ CHỮ (Xóa cái lệnh IF cản đường đi)
    // 🌟 SỰ KIỆN GÕ CHỮ TRÊN BẢN ĐỒ (ĐÃ LẮP PHANH CHỐNG GIẬT)
    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        // 1. Khách gõ chữ liên tục -> Cắt cầu dao cái cũ ngay!
        _mapSearchCts?.Cancel();

        // 2. Lên cầu dao mới
        _mapSearchCts = new CancellationTokenSource();
        var token = _mapSearchCts.Token;

        try
        {
            // 3. Đợi nửa giây xem khách có gõ thêm không
            await Task.Delay(500, token);

            // 4. Khách dừng tay rồi -> Lấy danh sách ghim (Pins) đem đi lọc!
            await FilterAndShowPins(e.NewTextValue ?? "");
        }
        catch (TaskCanceledException)
        {
            // Bị cắt cầu dao thì im lặng cho qua, không báo lỗi văng app
        }
    }

    private void HighlightClosestPin(Location userLocation)
    {
        if (tourMap.Pins == null || !tourMap.Pins.Any()) return;

        Pin closestPin = null;
        double minDistance = double.MaxValue;

        foreach (var pin in tourMap.Pins)
        {
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

    private async Task GetCurrentLocationAsync()
    {
        try
        {
            // 🌟 1. Kiểm tra ngầm trước (Không làm giật UI)
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            // 🌟 2. Nếu chưa có quyền thì mới trồi lên mặt nước xin phép
            if (status != PermissionStatus.Granted)
            {
                status = await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    return await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                });
            }

            // 🌟 3. Nếu khách cho phép thì chạy GPS
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
            else
            {
                // Nếu khách từ chối
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Cần quyền Vị trí", "Sếp chưa cấp quyền GPS thì Bản đồ không tự động tìm điểm được nhé!", "Đã hiểu");
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Lỗi GPS]: {ex.Message}");
        }
    }

    private void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        UpdateGPSPin(e.Location);
    }

    private void UpdateGPSPin(Location location)
    {
        if (location == null || location.Latitude == 0 || location.Longitude == 0) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_myLocationPin == null)
            {
                _myLocationPin = new Pin(tourMap)
                {
                    Label = "📍 Bạn đang ở đây",
                    Color = Colors.Blue,
                    Scale = 1.2f
                };
            }

            if (!tourMap.Pins.Contains(_myLocationPin))
            {
                tourMap.Pins.Add(_myLocationPin);
            }

            _myLocationPin.Position = new Position(location.Latitude, location.Longitude);
            HighlightClosestPin(location);
            tourMap.Refresh();
        });
    }

    private void FocusOnLocation(double lat, double lon)
    {
        var point = SphericalMercator.FromLonLat(lon, lat);

        MainThread.BeginInvokeOnMainThread(() => {
            tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(point.x, point.y));
            tourMap.Map?.Navigator?.ZoomTo(2);
            tourMap.Refresh();
        });
    }

    // =========================================================
    // 🌟 HIỂN THỊ BOTTOM SHEET (POPUP) THÔNG TIN POI
    // Được gọi bởi: nhấn pin trên bản đồ, nhấn POI trên MainPage,
    //              và quét QR từ QRCodePage
    // =========================================================
    private void ShowPoiPopup(POI poi)
    {
        _temporaryPoi = poi;
        lblPoiName.Text = poi.CurrentName;
        lblPoiDescription.Text = poi.CurrentDescription;
        lblPoiAddress.Text = $"Tọa độ: {poi.Latitude:F4}, {poi.Longitude:F4}";
        lblFavoriteIcon.Text = poi.IsFavorite ? "❤️" : "🤍";
        imgPoiImage.Source = poi.LocalImageSource;

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

    // =========================================================
    // 🔊 TỰ ĐỘNG PHÁT THUYẾT MINH (dùng khi mở từ QR)
    // =========================================================
    private async Task AutoPlayNarrationAsync(POI poi)
    {
        if (poi == null) return;

        // Nếu đang phát thì dừng cái cũ
        if (_isAudioPlaying)
        {
            _narrationEngine.Stop();
            _isAudioPlaying = false;
        }

        string langCode = PopupLangPicker.SelectedIndex switch
        {
            1 => "en",
            2 => "zh",
            3 => "ko",
            4 => "ja",
            _ => "vi"
        };
        string text = langCode switch
        {
            "en" => poi.Description_EN,
            "zh" => poi.Description_ZH,
            "ko" => poi.Description_KO,
            "ja" => poi.Description_JA,
            _ => poi.Description_VI
        };

        if (string.IsNullOrWhiteSpace(text)) return;

        _isAudioPlaying = true;
        UpdateAudioButton(true);

        await _narrationEngine.SpeakAsync($"{poi.CurrentName}. {text}", langCode);

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
        Preferences.Set("TTSLanguage", lang);

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
}