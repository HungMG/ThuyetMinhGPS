using System.Globalization;
using TourGuideApp.Views;
using TourGuideApp.Resources.Languages;

namespace TourGuideApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // 1. Lục tìm xem khách đã chọn ngôn ngữ nào chưa
        string savedLang = Preferences.Get("AppLanguage", "");

        if (string.IsNullOrEmpty(savedLang))
        {
            // 🌟 Lần đầu tải App (hoặc chưa chọn) -> Hiện trang chọn ngôn ngữ (StartPage)
            // 👇 Đã sửa chữ WelcomePage thành StartPage
            MainPage = new StartPage();
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