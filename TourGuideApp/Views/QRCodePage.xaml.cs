using ZXing.Net.Maui;
using TourGuideApp.Services;
using TourGuideApp.Models;
using TourGuideApp.Resources.Languages; // 🌟 Gọi thư viện Đa ngôn ngữ

namespace TourGuideApp.Views;

public partial class QRCodePage : ContentPage
{
    private bool _isProcessing = false;
    private string _lastScanned = "";
    private DateTime _lastScanTime = DateTime.MinValue;
    private DatabaseService _dbService;

    public QRCodePage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();

        // Cấu hình reader: chỉ quét QR, autorotate, đọc 1 mã/lần
        barcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,   // QR, DataMatrix, Aztec...
            AutoRotate = true,
            Multiple = false,
            TryHarder = true,
            TryInverted = true
        };
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

            // Debounce: cùng 1 mã trong < 2s thì bỏ qua
            if (qrCode == _lastScanned && (DateTime.Now - _lastScanTime).TotalSeconds < 2)
                return;
            _lastScanned = qrCode;
            _lastScanTime = DateTime.Now;

            _isProcessing = true;

            // 🌟 ẨN CHỮ ĐI LÚC QUÉT ĐƯỢC CHO ĐẸP
            barcodeResult.Text = "";

            var poi = await FindPoiByQrCodeAsync(qrCode);

            if (poi != null)
            {
                Preferences.Set("QRScannedPoiId", poi.Id);
                barcodeReader.IsDetecting = false;
                await Shell.Current.GoToAsync("//MapPage");
            }
            else
            {
                // 🌟 GỌI ĐA NGÔN NGỮ KHI LỖI QR
                barcodeResult.Text = ""; // Ẩn luôn chữ trên màn hình
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
        foreach (var kvp in map)
            r = r.Replace(kvp.Key, kvp.Value);
        return r;
    }

    // 🌟 SỬA Ở ĐÂY NÈ SẾP: DÙNG ĐA NGÔN NGỮ VÀ ẨN CHỮ
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isProcessing = false;
        _lastScanned = "";

        // Ẩn chữ lúc mới vào
        barcodeResult.Text = "";

        Dispatcher.Dispatch(async () =>
        {
            await Task.Delay(300);

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                if (barcodeReader != null)
                    barcodeReader.IsDetecting = true;

                // Ẩn chữ lúc đang quét luôn cho thoáng
                barcodeResult.Text = "";
            }
            else
            {
                // Báo lỗi Đa ngôn ngữ
                barcodeResult.Text = AppLang.QrCameraDenied;
                await DisplayAlert(AppLang.QrCameraErrorTitle, AppLang.QrCameraErrorDesc, "OK");
            }
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (barcodeReader != null)
            barcodeReader.IsDetecting = false;
    }
}