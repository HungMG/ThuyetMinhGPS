using Camera.MAUI;

namespace TourGuideApp.Views;

public partial class QRCodePage : ContentPage
{
    private bool _isScanning = false;

    public QRCodePage()
	{
		InitializeComponent();

		cameraView.BarCodeOptions = new()
		{
            AutoRotate = true,
            TryHarder = true,
            PossibleFormats = {BarcodeFormat.QR_CODE}
		}; // Chỉ quét mã QR
	}

    private void cameraView_CamerasLoaded(object sender, EventArgs e)
    {
		if (cameraView.Cameras.Count > 0)
		{
			cameraView.Camera = cameraView.Cameras[0]; // Chọn camera đầu tiên
        }
    }

    private void cameraView_BarcodeDetected(object sender, Camera.MAUI.ZXingHelper.BarcodeEventArgs args)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (args.Result == null || args.Result.Length == 0)
            {
                barcodeResult.Text = "Đang quét...";
                return;
            }

            var result = args.Result[0];

            // 🔥 Dừng animation khi quét được
            _isScanning = false;

            barcodeResult.Text = $"ĐÃ QUÉT: {result.Text}";
        });
    }

    // 🔥 ANIMATION SCAN LINE
    private async void StartScanAnimation()
    {
        if (scanLine == null) return;

        _isScanning = true;

        while (_isScanning)
        {
            await scanLine.TranslateTo(0, 230, 1500, Easing.Linear);
            scanLine.TranslationY = 0;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (cameraView != null && cameraView.Cameras.Count > 0)
        {
            cameraView.Camera = cameraView.Cameras[0];

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await cameraView.StopCameraAsync();
                await cameraView.StartCameraAsync();
            });
        }

        // 🔥 Start animation
        StartScanAnimation();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        _isScanning = false;

        if (cameraView != null)
        {
            await cameraView.StopCameraAsync();
        }
    }
}