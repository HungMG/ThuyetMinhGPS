using Camera.MAUI;

namespace TourGuideApp.Views;

public partial class QRCodePage : ContentPage
{
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
            barcodeResult.Text = $"ĐÃ QUÉT: {result.Text}";
        });
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
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();

        if (cameraView != null)
        {
            await cameraView.StopCameraAsync();
        }
    }
}