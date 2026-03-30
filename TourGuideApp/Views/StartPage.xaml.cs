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
    private void ApplyLanguage(string langCode)
    {
        // 1. Lưu ngôn ngữ khách chọn vào bộ nhớ
        Preferences.Set("AppLanguage", langCode);

        // 2. Xác định mã văn hóa (Culture Code) chuẩn của Microsoft
        string cultureCode = langCode switch
        {
            "en" => "en-US",
            "zh" => "zh-CN", // Trung Quốc (Giản thể)
            "ko" => "ko-KR", // Hàn Quốc
            "ja" => "ja-JP", // Nhật Bản
            _ => "vi-VN"     // Mặc định Việt Nam
        };

        // 3. Ép toàn bộ App đổi ngôn ngữ giao diện (.resx)
        var culture = new CultureInfo(cultureCode);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        AppLang.Culture = culture;

        // 4. Vô thẳng màn hình chính (AppShell)
        Application.Current.MainPage = new AppShell();
    }
}