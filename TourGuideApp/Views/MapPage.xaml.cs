using Mapsui.UI.Maui;
using Mapsui.Projections;
using TourGuideApp.Services;

namespace TourGuideApp.Views;

public partial class MapPage : ContentPage
{
    private DatabaseService _dbService;

    private List<int> _daDocThuyetMinh = new List<int>();

    public MapPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();

        // Nạp bản đồ đường phố OpenStreetMap
        tourMap.Map?.Layers.Add(Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
    }

    // 1. Khi vừa mở tab Bản đồ lên
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadPinsOnMapAsync();       // Gọi hàm cắm ghim trạm ẩm thực
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

    // 3. Hàm lấy dữ liệu từ Database và cắm ghim đỏ
    private async Task LoadPinsOnMapAsync()
    {
        var danhSachPOI = await _dbService.GetAllPOIsAsync();
        if (danhSachPOI == null || !danhSachPOI.Any()) return;

        tourMap.Pins.Clear();

        foreach (var poi in danhSachPOI)
        {
            var pin = new Pin(tourMap)
            {
                Position = new Position(poi.Latitude, poi.Longitude),
                Type = PinType.Pin,
                Label = poi.Name,
                Address = poi.Description,
                Color = Colors.Red
            };
            tourMap.Pins.Add(pin);
        }

        // Tạm thời zoom camera tới điểm đầu tiên trong DB khi vừa mở app
        var firstPoi = danhSachPOI.First();
        var toaDoZoom = SphericalMercator.FromLonLat(firstPoi.Longitude, firstPoi.Latitude);
        tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoZoom.x, toaDoZoom.y));
        tourMap.Map?.Navigator?.ZoomTo(2);
    }

    // 4. Hàm xin quyền và bật chế độ THEO DÕI LIÊN TỤC
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

                // Đăng ký sự kiện: Cứ nhích người là gọi hàm Geolocation_LocationChanged ở dưới
                Geolocation.LocationChanged += Geolocation_LocationChanged;

                // Bật "radar" quét 5 giây 1 lần
                var request = new GeolocationListeningRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(5));
                await Geolocation.StartListeningForegroundAsync(request);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi lấy vị trí: {ex.Message}");
        }
    }

    // 5. Hàm này sẽ TỰ ĐỘNG CHẠY mỗi khi bạn di chuyển bước chân
    private async void Geolocation_LocationChanged(object sender, GeolocationLocationChangedEventArgs e)
    {
        if (e.Location != null)
        {
            // 1. Dời chấm xanh và Camera (Code cũ)
            tourMap.MyLocationLayer.UpdateMyLocation(new Position(e.Location.Latitude, e.Location.Longitude));
            var toaDoMoi = Mapsui.Projections.SphericalMercator.FromLonLat(e.Location.Longitude, e.Location.Latitude);
            tourMap.Map?.Navigator?.CenterOn(new Mapsui.MPoint(toaDoMoi.x, toaDoMoi.y));

            // ==========================================================
            // 👇 BẮT ĐẦU LOGIC ĐO KHOẢNG CÁCH (GEOFENCE) 👇
            // ==========================================================

            // Lấy lại danh sách POI từ Database (hoặc bạn có thể lưu sẵn ra một biến global cho nhẹ)
            var danhSachPOI = await _dbService.GetAllPOIsAsync();
            if (danhSachPOI == null) return;

            // Quét qua từng địa điểm xem mình có đang đứng gần cái nào không
            foreach (var poi in danhSachPOI)
            {
                // Bỏ qua nếu chỗ này đã đọc thuyết minh rồi
                if (_daDocThuyetMinh.Contains(poi.Id)) continue;

                // Tạo tọa độ của điểm du lịch
                var toaDoDuLich = new Location(poi.Latitude, poi.Longitude);

                // HÀM TÍNH KHOẢNG CÁCH THẦN THÁNH CỦA MAUI (Tính bằng Kilomet)
                double khoangCachKm = Location.CalculateDistance(e.Location, toaDoDuLich, DistanceUnits.Kilometers);

                // Đổi ra mét cho dễ tính
                double khoangCachMet = khoangCachKm * 1000;

                // NẾU CÁCH ĐIỂM DU LỊCH DƯỚI 50 MÉT -> KÍCH HOẠT THUYẾT MINH!
                if (khoangCachMet <= 50)
                {
                    // 1. Ghi vào sổ tay là đã đọc rồi, để 5 giây sau không đọc lại nữa
                    _daDocThuyetMinh.Add(poi.Id);

                    // 2. Tạm thời dùng Text-to-Speech của điện thoại đọc lên cho ngầu
                    // (Sau này bạn có file .mp3 thì thay bằng lệnh Play Audio ở đây)
                    string cauChao = $"Chào mừng bạn đã đến với {poi.Name}. {poi.Description}";

                    // Lệnh này sẽ làm điện thoại bạn phát ra tiếng luôn!
                    await TextToSpeech.Default.SpeakAsync(cauChao);

                    // Hiện thêm một thông báo nhỏ trên màn hình
                    await DisplayAlert("Đã tới nơi!", cauChao, "OK");
                }
            }
        }
    }
}