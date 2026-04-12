using System.Globalization;
using TourGuideApp.Views;
using TourGuideApp.Resources.Languages;

namespace TourGuideApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // =========================================================
        // 🌟 BƯỚC 1: KHÓA CHẶT ĐỊNH DẠNG SỐ LÀ DẤU CHẤM (CỨU TINH CỦA GPS)
        // =========================================================
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;


        // =========================================================
        // 🌟 BƯỚC 2: KIỂM TRA NGÔN NGỮ
        // =========================================================
        string savedLang = Preferences.Get("AppLanguage", "");

        if (string.IsNullOrEmpty(savedLang))
        {
            // Trạm 1: Chưa chọn ngôn ngữ -> Bắt buộc mở StartPage đầu tiên
            MainPage = new NavigationPage(new StartPage());
        }
        else
        {
            // Đã có ngôn ngữ -> Cài đặt ngôn ngữ hiển thị
            string cultureCode = savedLang switch
            {
                "en" => "en-US",
                "zh" => "zh-CN",
                "ko" => "ko-KR",
                "ja" => "ja-JP",
                _ => "vi-VN"
            };

            var uiCulture = new CultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentUICulture = uiCulture;
            Thread.CurrentThread.CurrentUICulture = uiCulture;
            AppLang.Culture = uiCulture;

            // =========================================================
            // 🌟 BƯỚC 3: KIỂM TRA ĐĂNG NHẬP (TRẠM GÁC SỐ 2)
            // =========================================================
            int currentUserId = Preferences.Get("UserId", -1);

            if (currentUserId == -1)
            {
                // Có ngôn ngữ rồi, nhưng chưa có thẻ (chưa từng Đăng nhập/Ẩn danh) -> Vào trang Login
                MainPage = new LoginPage();
            }
            else
            {
                // Đã có ngôn ngữ VÀ đã có thẻ (Hoặc thẻ User thật, hoặc thẻ Ẩn danh 0) -> Vào Trang chủ
                MainPage = new AppShell();
            }
        }
    }
}