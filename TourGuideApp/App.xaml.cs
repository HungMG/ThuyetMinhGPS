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
        // Khóa cho luồng mặc định (các luồng đẻ ra sau này cũng bị khóa)
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        // Khóa cho luồng chạy giao diện hiện tại
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;


        string savedLang = Preferences.Get("AppLanguage", "");

        if (string.IsNullOrEmpty(savedLang))
        {
            // Chưa chọn ngôn ngữ -> Mở trang StartPage
            MainPage = new NavigationPage(new StartPage());
        }
        else
        {
            // =========================================================
            // 🌟 BƯỚC 2: CÀI ĐẶT NGÔN NGỮ HIỂN THỊ (CHỮ)
            // =========================================================
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

            // Nhảy thẳng vào App chính
            MainPage = new AppShell();
        }
    }
}