using System.Globalization;
using TourGuideApp.Resources.Languages;

namespace TourGuideApp.Views;

public partial class StartPage : ContentPage // 👇 Đổi tên Class
{
    public StartPage() // 👇 Đổi tên hàm
    {
        InitializeComponent(); // Gạch đỏ ở đây sẽ BAY MÀU ngay lập tức!
    }

    // 👇 5 SỰ KIỆN KHI BẤM 5 NÚT NGÔN NGỮ 👇
    private void OnVietnameseTapped(object sender, TappedEventArgs e) => ApplyLanguage("vi");
    private void OnEnglishTapped(object sender, TappedEventArgs e) => ApplyLanguage("en");
    private void OnChineseTapped(object sender, TappedEventArgs e) => ApplyLanguage("zh");
    private void OnKoreanTapped(object sender, TappedEventArgs e) => ApplyLanguage("ko");
    private void OnJapaneseTapped(object sender, TappedEventArgs e) => ApplyLanguage("ja");

    // 👇 HÀM XỬ LÝ ĐỔI NGÔN NGỮ CHÍNH 👇
    // 👇 HÀM XỬ LÝ ĐỔI NGÔN NGỮ ĐÃ ĐƯỢC "BỌC ĐƯỜNG" CHỐNG TREO MÁY 👇
    private async void ApplyLanguage(string langCode)
    {
        // 1. Kéo rèm xuống che màn hình lại, cấm bấm bậy!
        loadingOverlay.IsVisible = true;

        // 2. Chờ 0.1 giây để giao diện kịp vẽ cái màn che lên (Cực kỳ quan trọng)
        await Task.Delay(100);

        try
        {
            // 3. Đẩy việc xử lý Data ra đằng sau
            await Task.Run(() =>
            {
                Preferences.Set("AppLanguage", langCode);

                string cultureCode = langCode switch
                {
                    "en" => "en-US",
                    "zh" => "zh-CN",
                    "ko" => "ko-KR",
                    "ja" => "ja-JP",
                    _ => "vi-VN"
                };

                var culture = new CultureInfo(cultureCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                AppLang.Culture = culture;
            });

            // 4. Việc đổi Giao diện chính (UI) PHẢI GIAO LẠI CHO ANH TIẾP TÂN làm (Bắt buộc của .NET MAUI)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new AppShell();
            });
        }
        catch (Exception)
        {
            // Kéo rèm lên nếu lỡ có lỗi
            loadingOverlay.IsVisible = false;
        }
    }
}