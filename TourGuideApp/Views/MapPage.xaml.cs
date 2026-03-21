using Mapsui.UI.Maui;
using Mapsui.Projections;
using TourGuideApp.Services;
using System.Linq; // Thêm cái này để xài hàm lọc danh sách

namespace TourGuideApp.Views;
using TourGuideApp.Models;

public partial class MapPage : ContentPage
{
    private DatabaseService _dbService;

    // Cuốn sổ tay ghi nhớ các địa điểm đã đọc thuyết minh
    private List<int> _daDocThuyetMinh = new List<int>();

    // Biến tạm để nút Dẫn Đường lấy tọa độ
    private POI _temporaryPoi;

    public MapPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();

        tourMap.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
        tourMap.IsZoomButtonVisible = false;
        tourMap.IsMyLocationButtonVisible = false;
        tourMap.IsNorthingButtonVisible = false;

        // 👇 LẮNG NGHE CÚ CHẠM CỦA NGƯỜI DÙNG ĐỂ THÊM ĐIỂM 👇
        tourMap.MapClicked += TourMap_MapClicked;
    }

    // 1. Khi vừa mở tab Bản đồ lên
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Gọi hàm mới: Tải TẤT CẢ điểm (vì từ khóa đang bỏ trống "")
        await FilterAndShowPins("");

        await GetCurrentLocationAsync();  // Gọi hàm bật GPS theo dõi bạn
    }

    // 2. Khi bạn rời khỏi tab Bản đồ (Sang tab khác)
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Ngắt lắng nghe GPS để tiết kiệm pin cho điện thoại
        Geolocation.LocationChanged -= Geolocation_LocationChanged;
        Geolocation.StopListeningForeground();
    }

    // =========================================================
    // 👇 PHẦN 1: SỰ KIỆN TÌM KIẾM 👇
    // =========================================================

    // Khi người dùng bấm nút Tìm kiếm (Kính lúp) trên bàn phím
    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        mapSearchBar.Unfocus(); // Ẩn bàn phím đi cho dễ nhìn
        string keyword = mapSearchBar.Text?.ToLower() ?? "";
        await FilterAndShowPins(keyword);
    }

    // Khi người dùng xóa chữ trên thanh tìm kiếm (Hiện lại tất cả)
    private async void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            await FilterAndShowPins("");
        }
    }

    // =========================================================
    // 👇 PHẦN 2: POPUP CHI TIẾT ĐỊA ĐIỂM (Plugin.Maui.BottomSheet) 👇
    // =========================================================

    // Khi người dùng lấy tay chạm vào cái ghim đỏ
    // =========================================================
    // 👇 SỰ KIỆN CLICK GHIM ĐỎ (PIN): BẬT POPUP BottomSheet 👇
    // =========================================================
    private void TourMap_PinClicked(object sender, PinClickedEventArgs e)
    {
        e.Handled = true; // Chặn bong bóng mặc định của Mapsui

        // 1. Xác định địa điểm vừa được Click (tìm trong DB)
        // Lưu ý: e.Pin.Address đang chứa Mô tả, tui sẽ xài Mô tả thật trong DB để Popup đẹp hơn
        // e.Pin.Label chứa Tên.
        string tenDiaDiem = e.Pin.Label;

        // 💡 Để tìm chính xác địa điểm trong DB, cách tốt nhất là dùng Tọa độ (Lat/Lon)
        // Vì Tên có thể bị trùng. Tọa độ thì luôn duy nhất!
        double lat = e.Pin.Position.Latitude;
        double lon = e.Pin.Position.Longitude;

        // Lấy danh sách điểm từ SQLite
        // (Đây là lúc tui khuyên bạn nên lưu cái danh sách điểm ban đầu ra một biến global cho nhẹ,
        // nhưng tạm thời tui code Get lại từ DB cho bạn dễ hình dung logic nhé).
        // await _dbService.GetAllPOIsAsync() -> Lọc lại -> Tìm điểm có Lat/Lon khớp.
        // Tui sẽ tạm thời giả định bạn đã có list dữ liệu 'allPOIs' nhé.

        // Tạm thời để code Get lại từ DB nhé (không khuyến khích)
        Task.Run(async () => {
            var allPOIs = await _dbService.GetAllPOIsAsync();
            if (allPOIs == null) return;

            // Tìm điểm trùng Tên (hoặc trùng tọa độ cho chính xác)
            var currentPoi = allPOIs.FirstOrDefault(p => p.Name == tenDiaDiem);

            if (currentPoi != null)
            {
                // 👇 CHẠY TRÊN MAIN THREAD ĐỂ CẬP NHẬT GIAO DIỆN 👇
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // 2. "Rót" dữ liệu thật từ SQLite vào các Label trong Popup BottomSheet
                    lblPoiName.Text = currentPoi.Name;
                    lblPoiDescription.Text = currentPoi.Description;
                    lblPoiAddress.Text = $"{currentPoi.Latitude:F5}, {currentPoi.Longitude:F5}"; // VD: Hiện tọa độ làm địa chỉ

                    // Cập nhật hình ảnh (dùng cái tên file mình vừa thêm vào model)
                    if (!string.IsNullOrWhiteSpace(currentPoi.ImageUrl))
                    {
                        imgPoiImage.Source = currentPoi.ImageUrl;
                    }
                    else
                    {
                        // Nếu không có hình, xài hình mặc định
                        imgPoiImage.Source = "img_default_poi.png";
                    }

                    // 💡 Lưu lại địa điểm hiện tại vào một biến tạm để nút Dẫn Đường xài
                    _temporaryPoi = currentPoi;

                    // 3. TUYỆT CHIÊU: BẬT POPUP BottomSheet TRƯỢT LÊN!
                    // Lần này bật full màn hình PeekHeight = 1500px luôn cho nó ngầu
                    poiDetailPopup.PeekHeight = 1500;
                    poiDetailPopup.IsOpen = true;
                });
            }
        });
    }

    // Sự kiện khi bấm nút Dẫn Đường trên Popup
    private async void OnNavigationClicked(object sender, EventArgs e)
    {
        if (_temporaryPoi == null) return;

        // ✅ ✅ ĐÃ SỬA: Ẩn Popup đi cho đỡ vướng
        poiDetailPopup.IsOpen = false; // 👇 ĐÃ SỬA: Dùng IsOpen = false cho Plugin.Maui.BottomSheet

        try
        {
            var toaDoDich = new Location(_temporaryPoi.Latitude, _temporaryPoi.Longitude);
            var options = new MapLaunchOptions { Name = _temporaryPoi.Name, NavigationMode = NavigationMode.Driving };
            await Microsoft.Maui.ApplicationModel.Map.OpenAsync(toaDoDich, options);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể mở ứng dụng bản đồ.", "OK");
        }
    }

    // Hàm cốt lõi: Vừa Lọc vừa Cắm ghim đỏ lên bản đồ (vẫn giữ logic của bạn)
    private async Task FilterAndShowPins(string keyword)
    {
        var danhSachPOI = await _dbService.GetAllPOIsAsync();
        if (danhSachPOI == null) return;

        // Nếu có gõ từ khóa -> Lọc danh sách
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            danhSachPOI = danhSachPOI.Where(p =>
                p.Name.ToLower().Contains(keyword) ||
                p.Description.ToLower().Contains(keyword)
            ).ToList();
        }

        tourMap.Pins.Clear();
        tourMap.PinClicked -= TourMap_PinClicked; // Xóa đăng ký cũ tránh lỗi click đúp
        tourMap.PinClicked += TourMap_PinClicked; // Đăng ký sự kiện Click cho ghim

        foreach (var poi in danhSachPOI)
        {
            var pin = new Pin(tourMap)
            {
                Position = new Position(poi.Latitude, poi.Longitude),
                Type = PinType.Pin,
                Label = poi.Name,
                // Label đang chứa Tên, Address tạm thời chứa Description (vẫn giữ logic cũ)
                Address = poi.Description,
                Color = Colors.Red
            };
            tourMap.Pins.Add(pin);
        }

        // Nếu tìm thấy kết quả thì Zoom camera tới điểm đầu tiên
        if (danhSachPOI.Any())
        {
            var firstPoi = danhSachPOI.First();
            var toaDoZoom = SphericalMercator.FromLonLat(firstPoi.Longitude, firstPoi.Latitude);
            tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoZoom.x, toaDoZoom.y));
            tourMap.Map?.Navigator?.ZoomTo(2);
        }
        else if (!string.IsNullOrWhiteSpace(keyword))
        {
            await DisplayAlert("Rất tiếc", "Không tìm thấy địa điểm nào phù hợp!", "Thử lại");
        }
    }

    // =========================================================
    // 👇 PHẦN 3: GPS VÀ GEOFENCE (AUDIO) CỦA BẠN (GIỮ NGUYÊN) 👇
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
            // Dời chấm xanh và Camera 
            tourMap.MyLocationLayer.UpdateMyLocation(new Position(e.Location.Latitude, e.Location.Longitude));
            var toaDoMoi = SphericalMercator.FromLonLat(e.Location.Longitude, e.Location.Latitude);
            tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoMoi.x, toaDoMoi.y));

            // Lấy lại danh sách POI từ Database
            var danhSachPOI = await _dbService.GetAllPOIsAsync();
            if (danhSachPOI == null) return;

            foreach (var poi in danhSachPOI)
            {
                if (_daDocThuyetMinh.Contains(poi.Id)) continue;

                var toaDoDuLich = new Location(poi.Latitude, poi.Longitude);
                double khoangCachKm = Location.CalculateDistance(e.Location, toaDoDuLich, DistanceUnits.Kilometers);
                double khoangCachMet = khoangCachKm * 1000;

                // Cách dưới 50m -> Đọc thuyết minh
                if (khoangCachMet <= 50)
                {
                    _daDocThuyetMinh.Add(poi.Id);
                    string cauChao = $"Chào mừng bạn đã đến với {poi.Name}. {poi.Description}";

                    await TextToSpeech.Default.SpeakAsync(cauChao);
                    await DisplayAlert("Đã tới nơi!", cauChao, "OK");
                }
            }
        }
    }

    // =========================================================
    // 👇 PHẦN 4: SỰ KIỆN CHẠM BẢN ĐỒ ĐỂ THÊM ĐIỂM (GIỮ NGUYÊN) 👇
    // =========================================================

    private async void TourMap_MapClicked(object sender, MapClickedEventArgs e)
    {
        // 1. Quá sướng! Mapsui đã tự tính sẵn Lat/Lon cho mình, không cần công thức gì hết!
        double viDo = e.Point.Latitude;
        double kinhDo = e.Point.Longitude;

        // 2. Hỏi người dùng muốn làm gì?
        string hanhDong = await DisplayActionSheet("Tọa độ mới", "Hủy", null, "Thêm địa điểm vào đây");

        if (hanhDong == "Thêm địa điểm vào đây")
        {
            // 3. Hiện Popup cho họ nhập tên địa điểm
            string tenDiaDiem = await DisplayPromptAsync("Tạo POI mới", "Nhập tên địa điểm:", "Lưu", "Hủy", "VD: Quán cafe cô Ba...");

            // Nếu họ không nhập gì hoặc bấm Hủy thì thôi
            if (string.IsNullOrWhiteSpace(tenDiaDiem)) return;

            // 4. Hiện Popup cho họ nhập mô tả
            string moTa = await DisplayPromptAsync("Chi tiết", "Nhập mô tả cho nơi này:", "Lưu", "Hủy", "VD: Trà đào cực ngon!");

            // 5. Đóng gói dữ liệu và đẩy xuống Database
            var diemMoi = new Models.POI // Nhớ kiểm tra lại chữ Models.POI xem đúng tên class của bạn chưa nha
            {
                Name = tenDiaDiem,
                Description = moTa ?? "Không có mô tả",
                Latitude = viDo,      // 👇 Đưa thẳng Vĩ độ vào đây
                Longitude = kinhDo    // 👇 Đưa thẳng Kinh độ vào đây
            };

            await _dbService.AddPOIAsync(diemMoi);

            // 6. Thông báo thành công và TẢI LẠI BẢN ĐỒ NGAY LẬP TỨC!
            await DisplayAlert("Thành công", $"Đã thêm '{tenDiaDiem}' vào bản đồ!", "OK");

            await FilterAndShowPins("");
        }
    }
}