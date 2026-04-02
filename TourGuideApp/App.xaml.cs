using System.Globalization;
using TourGuideApp.Views;
using TourGuideApp.Resources.Languages;

namespace TourGuideApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        string savedLang = Preferences.Get("AppLanguage", "");

        if (string.IsNullOrEmpty(savedLang))
        {
            // 🌟 CỦA BẠN: MainPage = new StartPage();
            // 🌟 SỬA THÀNH: Bao bọc trong NavigationPage để có thể dùng PushAsync
            MainPage = new NavigationPage(new StartPage());
        }
        else
        {
            // 🌟 Đã chọn rồi -> Nạp ngôn ngữ đó lên
            string cultureCode = savedLang switch
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

            // 🌟 VÀ QUAN TRỌNG NHẤT: Bỏ qua trang chọn ngôn ngữ, nhảy thẳng vô App chính!
            // 👇 Đã sửa StartPage thành AppShell (Trang chứa các Tab)
            MainPage = new AppShell();
        }
    }
}