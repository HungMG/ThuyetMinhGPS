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
    private int _currentTourId = -1; // -1 nghĩa là hiển thị tất cả
    public MapPage(int tourId = -1)
    {
        InitializeComponent();
        _dbService = new DatabaseService();
        _currentTourId = tourId;

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

        // 🌟 KHÚC BẠN BỎ QUÊN LÀ Ở ĐÂY NÈ 🌟
        // Lọc ra đúng những địa điểm thuộc cái Tour khách vừa bấm
        if (_currentTourId != -1)
        {
            danhSachPOI = danhSachPOI.Where(p => p.TourId == _currentTourId).ToList();
        }

        // Lọc tiếp theo từ khóa tìm kiếm (nếu khách có gõ vào ô Search)
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
            tourMap.Map?.Navigator?.ZoomTo(2); // Zoom lại gần
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
        if (e.Location == null) return;

        // 1. Cập nhật vị trí của mình trên bản đồ
        tourMap.MyLocationLayer.UpdateMyLocation(new Position(e.Location.Latitude, e.Location.Longitude));

        // 2. Lấy danh sách địa điểm và lọc ra những điểm người dùng ĐANG ĐỨNG TRONG BÁN KÍNH
        var allPOIs = await _dbService.GetAllPOIsAsync();

        var poisInRange = allPOIs.Where(poi => {
            var poiLoc = new Location(poi.Latitude, poi.Longitude);
            double distanceMet = Location.CalculateDistance(e.Location, poiLoc, DistanceUnits.Kilometers) * 1000;

            // Kiểm tra: Khoảng cách < Bán kính thiết lập của POI đó
            return distanceMet <= poi.TriggerRadius;
        }).ToList();

        if (!poisInRange.Any()) return;

        // 3. Chọn ra điểm có ĐỘ ƯU TIÊN (Priority) cao nhất và ĐÃ HẾT THỜI GIAN CHỜ (Cooldown)
        // Ở đây tui để thời gian chờ là 30 phút (có thể chỉnh lại tùy ý)
        var bestPOI = poisInRange
            .Where(p => !p.LastPlayedTime.HasValue || (DateTime.Now - p.LastPlayedTime.Value).TotalMinutes >= 30)
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();

        if (bestPOI != null)
        {
            // 4. Cập nhật thời gian vừa đọc để chống spam
            bestPOI.LastPlayedTime = DateTime.Now;
            await _dbService.UpdatePOIAsync(bestPOI); // Lưu lại vào DB để lần sau mở app vẫn nhớ

            // 5. Phát thuyết minh đa ngôn ngữ (Dùng đúng logic NarrationEngine)
            string langCode = Preferences.Get("AppLanguage", "vi");
            string textToRead = langCode switch
            {
                "en" => bestPOI.Description_EN,
                "zh" => bestPOI.Description_ZH,
                "ko" => bestPOI.Description_KO,
                "ja" => bestPOI.Description_JA,
                _ => bestPOI.Description_VI
            };

            // Gửi lệnh đọc cho loa
            await _narrationEngine.SpeakAsync($"{bestPOI.CurrentName}. {textToRead}", langCode);

            // Hiện thông báo cho đẹp
            await DisplayAlert(bestPOI.CurrentName, "Tự động thuyết minh đang phát...", "OK");
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