using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Plugin.Maui.BottomSheet.Hosting;
using ZXing.Net.Maui.Controls;

namespace TourGuideApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseBarcodeReader()// bật camera 
            .UseBottomSheet()// Cái công tắc bật cọ vẽ SkiaSharp bữa trước mình thêm vào
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // 👇 CHỈ CẦN DÁN THÊM ĐÚNG DÒNG NÀY VÀO ĐÂY 👇
        Mapsui.Widgets.InfoWidgets.LoggingWidget.ShowLoggingInMap = Mapsui.Widgets.ActiveMode.No;


#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}