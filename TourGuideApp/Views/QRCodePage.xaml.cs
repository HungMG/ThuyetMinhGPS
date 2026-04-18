using ZXing.Net.Maui;
using TourGuideApp.Services;
using TourGuideApp.Models;
using TourGuideApp.Resources.Languages;

namespace TourGuideApp.Views;

public partial class QRCodePage : ContentPage
{
    private bool _isProcessing = false;
    private string _lastScanned = "";
    private DateTime _lastScanTime = DateTime.MinValue;
    private DatabaseService _dbService;
    private ZXing.Net.Maui.Controls.CameraBarcodeReaderView barcodeReader;

    // 🌟 VŨ KHÍ MỚI: KHIÊN BẢO VỆ GIAO DIỆN KHỎI BỊ RESET OAN UỔNG
    private bool _isRequestingPermission = false;

    public QRCodePage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
    }

    private void barcodeReader_BarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (e?.Results == null || e.Results.Length == 0) return;

        var first = e.Results.FirstOrDefault();
        if (first == null || string.IsNullOrWhiteSpace(first.Value)) return;

        string qrCode = first.Value.Trim();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_isProcessing) return;

            if (qrCode == _lastScanned && (DateTime.Now - _lastScanTime).TotalSeconds < 2)
                return;
            _lastScanned = qrCode;
            _lastScanTime = DateTime.Now;

            _isProcessing = true;
            barcodeResult.Text = "";

            var poi = await FindPoiByQrCodeAsync(qrCode);

            if (poi != null)
            {
                Preferences.Set("QRScannedPoiId", poi.Id);
                barcodeReader.IsDetecting = false;

                ApiService apiService = new ApiService();
                _ = apiService.TrackActionAsync($"Quét QR: {poi.Name_VI}");

                await Shell.Current.GoToAsync("//MapPage");
            }
            else
            {
                barcodeResult.Text = "";
                await DisplayAlert(AppLang.QrNotFoundTitle, AppLang.QrNotFoundDesc, "OK");
                _isProcessing = false;
            }
        });
    }

    private async Task<POI> FindPoiByQrCodeAsync(string qrCode)
    {
        try
        {
            var allPois = await _dbService.GetAllPOIsAsync();
            if (allPois == null || allPois.Count == 0) return null;

            string normalizedQr = qrCode.Trim().ToLower();

            foreach (var poi in allPois)
            {
                string expectedQr = GenerateQrCode(poi.Name_VI);
                if (normalizedQr == expectedQr)
                    return poi;
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[QR Error] {ex.Message}");
            return null;
        }
    }

    public static string GenerateQrCode(string poiName)
    {
        if (string.IsNullOrWhiteSpace(poiName)) return "";
        string result = poiName.ToLower().Trim();
        result = RemoveDiacritics(result);
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", "_");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"[^a-z0-9_]", "");
        return $"poi_{result}";
    }

    private static string RemoveDiacritics(string text)
    {
        var map = new Dictionary<string, string>
        {
            {"à","a"},{"á","a"},{"â","a"},{"ã","a"},{"ä","a"},{"å","a"},
            {"ă","a"},{"ắ","a"},{"ặ","a"},{"ằ","a"},{"ẳ","a"},{"ẵ","a"},
            {"ấ","a"},{"ầ","a"},{"ẩ","a"},{"ẫ","a"},{"ậ","a"},
            {"è","e"},{"é","e"},{"ê","e"},{"ë","e"},
            {"ế","e"},{"ề","e"},{"ể","e"},{"ễ","e"},{"ệ","e"},
            {"ì","i"},{"í","i"},{"î","i"},{"ï","i"},{"ị","i"},{"ỉ","i"},{"ĩ","i"},
            {"ò","o"},{"ó","o"},{"ô","o"},{"õ","o"},{"ö","o"},{"ø","o"},
            {"ố","o"},{"ồ","o"},{"ổ","o"},{"ỗ","o"},{"ộ","o"},
            {"ơ","o"},{"ớ","o"},{"ờ","o"},{"ở","o"},{"ỡ","o"},{"ợ","o"},
            {"ù","u"},{"ú","u"},{"û","u"},{"ü","u"},
            {"ư","u"},{"ứ","u"},{"ừ","u"},{"ử","u"},{"ữ","u"},{"ự","u"},{"ụ","u"},
            {"ỳ","y"},{"ý","y"},{"ỷ","y"},{"ỹ","y"},{"ỵ","y"},
            {"đ","d"},{"ñ","n"},{"ç","c"},
        };
        string r = text;
        foreach (var kvp in map) r = r.Replace(kvp.Key, kvp.Value);
        return r;
    }

    // ==========================================================
    // 🌟 QUẢN LÝ VÒNG ĐỜI: DỌN RÁC MỖI KHI RA VÀO TAB
    // ==========================================================
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isProcessing = false;
        _lastScanned = "";
        barcodeResult.Text = "";

        if (_isRequestingPermission) return;

        ResetButton(true);
        DestroyCamera(); // Vào là dọn sạch tàn dư
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DestroyCamera(); // Ra cũng dọn sạch để nhường RAM cho Map
    }

    private void DestroyCamera()
    {
        if (barcodeReader != null)
        {
            barcodeReader.IsDetecting = false;
            barcodeReader.IsVisible = false;
        }
        CameraContainer.Children.Clear();
        barcodeReader = null;
    }

    /// ==========================================================
    // 🌟 SỰ KIỆN BẤM NÚT & GỌI CAMERA (BẢN VÁ LỖI BÓNG MA ASYNC)
    // ==========================================================
    private void OnStartScanClicked(object sender, EventArgs e)
    {
        // Lưu ý: Hàm này đã bỏ chữ 'async' để chống rớt Context của Android!

        if (_isRequestingPermission) return;
        _isRequestingPermission = true;

        // 1. Phản hồi giao diện NGAY LẬP TỨC
        btnStartScan.IsEnabled = false;
        btnStartScan.Text = AppLang.QROpeningCamera;
        btnStartScan.BackgroundColor = Colors.Gray;

        // 2. TRÓI CHẶT TOÀN BỘ TIẾN TRÌNH VÀO LUỒNG GIAO DIỆN CHÍNH
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Cho nút kịp đổi sang màu xám trước khi màn hình bị Android khóa lại để hỏi quyền
                await Task.Delay(100);

                // 3. Xin quyền (Lúc này chắc chắn 100% Android sẽ hiện bảng vì đang bị ép trên MainThread)
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                }

                // 4. Nếu khách đồng ý
                if (status == PermissionStatus.Granted)
                {
                    // Tạo Camera mới cứng
                    barcodeReader = new ZXing.Net.Maui.Controls.CameraBarcodeReaderView
                    {
                        IsVisible = true,
                        CameraLocation = ZXing.Net.Maui.CameraLocation.Rear,
                        Options = new ZXing.Net.Maui.BarcodeReaderOptions
                        {
                            Formats = ZXing.Net.Maui.BarcodeFormats.TwoDimensional,
                            AutoRotate = true,
                            Multiple = false,
                            TryHarder = true
                        }
                    };

                    barcodeReader.BarcodesDetected += barcodeReader_BarcodesDetected;

                    // Vũ khí "Đợi chín mới hái"
                    barcodeReader.Loaded += async (s, args) =>
                    {
                        try
                        {
                            await Task.Delay(250);
                            barcodeReader.IsDetecting = true;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Lỗi phần cứng Camera]: {ex.Message}");
                        }
                    };

                    // Ném Camera vào khung và giấu nút
                    CameraContainer.Children.Add(barcodeReader);
                    btnStartScan.IsVisible = false;
                }
                else
                {
                    // Nếu từ chối
                    barcodeResult.Text = "Sếp chưa cho phép dùng Camera!";
                    ResetButton(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Lỗi QR Chung]: {ex.Message}");
                ResetButton(false);
            }
            finally
            {
                _isRequestingPermission = false;
            }
        });
    }


    private void ResetButton(bool isInitial)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            btnStartScan.IsVisible = true;
            btnStartScan.IsEnabled = true;
            btnStartScan.Text = AppLang.QRStartScan;
            btnStartScan.BackgroundColor = Color.FromArgb("#F39C12");
        });
    }

}