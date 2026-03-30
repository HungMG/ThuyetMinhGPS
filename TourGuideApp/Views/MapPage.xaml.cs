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

    public MapPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();

        tourMap.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        tourMap.IsZoomButtonVisible = false;
        tourMap.IsNorthingButtonVisible = false;

        // 🌟 ĐÃ BẬT LẠI: Nút bấm định vị vị trí hiện tại của Mapsui 🌟
        tourMap.IsMyLocationButtonVisible = true;

        tourMap.MapClicked += TourMap_MapClicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
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

        string tenDiaDiem = e.Pin.Label;

        Task.Run(async () => {
            var allPOIs = await _dbService.GetAllPOIsAsync();
            if (allPOIs == null) return;

            var currentPoi = allPOIs.FirstOrDefault(p => p.CurrentName == tenDiaDiem);

            if (currentPoi != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblPoiName.Text = currentPoi.CurrentName;
                    lblPoiDescription.Text = currentPoi.CurrentDescription;
                    lblPoiAddress.Text = $"{currentPoi.Latitude:F5}, {currentPoi.Longitude:F5}";

                    if (!string.IsNullOrWhiteSpace(currentPoi.ImageUrl))
                    {
                        imgPoiImage.Source = currentPoi.ImageUrl;
                    }
                    else
                    {
                        imgPoiImage.Source = "img_default_poi.png";
                    }

                    _temporaryPoi = currentPoi;

                    poiDetailPopup.PeekHeight = 1500;
                    poiDetailPopup.IsOpen = true;
                });
            }
        });
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

    private async Task FilterAndShowPins(string keyword)
    {
        var danhSachPOI = await _dbService.GetAllPOIsAsync();
        if (danhSachPOI == null) return;

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
                Color = Colors.Red
            };
            tourMap.Pins.Add(pin);
        }

        if (danhSachPOI.Any())
        {
            var firstPoi = danhSachPOI.First();
            var toaDoZoom = SphericalMercator.FromLonLat(firstPoi.Longitude, firstPoi.Latitude);
            tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoZoom.x, toaDoZoom.y));
            tourMap.Map?.Navigator?.ZoomTo(2);
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
        if (e.Location != null)
        {
            tourMap.MyLocationLayer.UpdateMyLocation(new Position(e.Location.Latitude, e.Location.Longitude));

            // Xóa dòng trượt tự động này đi nếu bạn không muốn bản đồ cứ giật giật chạy theo GPS
            // var toaDoMoi = SphericalMercator.FromLonLat(e.Location.Longitude, e.Location.Latitude);
            // tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoMoi.x, toaDoMoi.y));

            var danhSachPOI = await _dbService.GetAllPOIsAsync();
            if (danhSachPOI == null) return;

            foreach (var poi in danhSachPOI)
            {
                if (_daDocThuyetMinh.Contains(poi.Id)) continue;

                var toaDoDuLich = new Location(poi.Latitude, poi.Longitude);
                double khoangCachKm = Location.CalculateDistance(e.Location, toaDoDuLich, DistanceUnits.Kilometers);
                double khoangCachMet = khoangCachKm * 1000;

                if (khoangCachMet <= 50)
                {
                    _daDocThuyetMinh.Add(poi.Id);

                    string langCode = Preferences.Get("AppLanguage", "vi");
                    var locales = await TextToSpeech.Default.GetLocalesAsync();
                    var selectedLocale = locales.FirstOrDefault(l => l.Language.ToLower().StartsWith(langCode));

                    var options = new SpeechOptions()
                    {
                        Locale = selectedLocale
                    };

                    string cauThuyetMinh = $"{poi.CurrentName}. {poi.CurrentDescription}";

                    await TextToSpeech.Default.SpeakAsync(cauThuyetMinh, options);
                    await DisplayAlert("Location Reached", poi.CurrentName, "OK");
                }
            }
        }
    }

    // =========================================================
    // 👇 PHẦN 4: SỰ KIỆN CHẠM BẢN ĐỒ ĐỂ THÊM ĐIỂM ĐA NGÔN NGỮ 👇
    // =========================================================

    private async void TourMap_MapClicked(object sender, MapClickedEventArgs e)
    {
        double viDo = e.Point.Latitude;
        double kinhDo = e.Point.Longitude;

        string langCode = Preferences.Get("AppLanguage", "vi");

        // 1. Dịch thuật các nút bấm ActionSheet
        string title = langCode switch { "en" => "New Location", "zh" => "新位置", "ko" => "새 위치", "ja" => "新しい場所", _ => "Tọa độ mới" };
        string cancel = langCode switch { "en" => "Cancel", "zh" => "取消", "ko" => "취소", "ja" => "キャンセル", _ => "Hủy" };
        string addAction = langCode switch { "en" => "Add location here", "zh" => "在此处添加", "ko" => "여기에 추가", "ja" => "ここに追加", _ => "Thêm địa điểm vào đây" };

        string hanhDong = await DisplayActionSheet(title, cancel, null, addAction);

        if (hanhDong == addAction)
        {
            // 2. Dịch thuật bảng nhập liệu Prompt
            string promptTitle = langCode switch { "en" => "New POI", "zh" => "新景点", "ko" => "새 장소", "ja" => "新しいスポット", _ => "Tạo POI mới" };
            string promptMsg = langCode switch { "en" => "Enter name:", "zh" => "输入名称:", "ko" => "이름 입력:", "ja" => "名前を入力:", _ => "Nhập tên địa điểm:" };
            string saveBtn = langCode switch { "en" => "Save", "zh" => "保存", "ko" => "저장", "ja" => "保存", _ => "Lưu" };

            string tenDiaDiem = await DisplayPromptAsync(promptTitle, promptMsg, saveBtn, cancel);
            if (string.IsNullOrWhiteSpace(tenDiaDiem)) return;

            string descMsg = langCode switch { "en" => "Enter description:", "zh" => "输入描述:", "ko" => "설명 입력:", "ja" => "説明を入力:", _ => "Nhập mô tả:" };
            string moTa = await DisplayPromptAsync(promptTitle, descMsg, saveBtn, cancel);
            moTa ??= "Không có mô tả";

            // 3. 🌟 TUYỆT CHIÊU: Ghi đè chữ người dùng nhập vào TẤT CẢ các cột ngôn ngữ
            // Tránh trường hợp nhập bên Tiếng Anh xong chuyển qua Tiếng Nhật bị mất chữ
            var diemMoi = new POI
            {
                Name_VI = tenDiaDiem,
                Name_EN = tenDiaDiem,
                Name_ZH = tenDiaDiem,
                Name_KO = tenDiaDiem,
                Name_JA = tenDiaDiem,
                Description_VI = moTa,
                Description_EN = moTa,
                Description_ZH = moTa,
                Description_KO = moTa,
                Description_JA = moTa,
                Latitude = viDo,
                Longitude = kinhDo
            };

            await _dbService.AddPOIAsync(diemMoi);

            // 4. Dịch thuật thông báo thành công
            string successTitle = langCode switch { "en" => "Success", "zh" => "成功", "ko" => "성공", "ja" => "成功", _ => "Thành công" };
            string successMsg = langCode switch { "en" => $"Added '{tenDiaDiem}'!", "zh" => $"已添加 '{tenDiaDiem}'!", "ko" => $"'{tenDiaDiem}' 추가됨!", "ja" => $"'{tenDiaDiem}' を追加しました!", _ => $"Đã thêm '{tenDiaDiem}'!" };

            await DisplayAlert(successTitle, successMsg, "OK");
            await FilterAndShowPins("");
        }

    }
    // =========================================================
    // 👇 HÀM XỬ LÝ NÚT TỰ ĐỊNH VỊ VỊ TRÍ 👇
    // =========================================================
    private void OnCenterLocationTapped(object sender, TappedEventArgs e)
    {
        // Kiểm tra xem máy đã bắt được GPS của người dùng chưa
        if (tourMap.MyLocationLayer != null && tourMap.MyLocationLayer.MyLocation != null)
        {
            // Lấy tọa độ hiện tại
            var loc = tourMap.MyLocationLayer.MyLocation;
            var toaDoHienTai = SphericalMercator.FromLonLat(loc.Longitude, loc.Latitude);

            // Trượt camera bản đồ về đúng chỗ đó
            tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoHienTai.x, toaDoHienTai.y));

            // Zoom lại gần cho dễ nhìn (Mức 2 là vừa đẹp)
            tourMap.Map?.Navigator?.ZoomTo(2);
        }
        else
        {
            // Lỡ như mạng lag chưa load kịp GPS
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
}