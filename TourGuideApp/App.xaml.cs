using System.Globalization;
using TourGuideApp.Views;
using TourGuideApp.Resources.Languages;
using TourGuideApp.Services; // 🌟 Bắt buộc thêm dòng này để gọi ApiService

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
            MainPage = new NavigationPage(new StartPage());
        }
        else
        {
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
            // 🌟 BƯỚC 3: KIỂM TRA ĐĂNG NHẬP
            // =========================================================
            int currentUserId = Preferences.Get("UserId", -1);

            if (currentUserId == -1)
            {
                MainPage = new LoginPage();
            }
            else
            {
                MainPage = new AppShell();
            }
        }
    }

    // ==========================================
    // 🌟 HÀM 1: KHI NGƯỜI DÙNG VỪA MỞ APP LÊN
    // ==========================================
    protected override void OnStart()
    {
        base.OnStart();
        ReportOnlineStatus("Mở ứng dụng");
    }

    // ==========================================
    // 🌟 HÀM 2: KHI NGƯỜI DÙNG ẨN APP RỒI MỞ SÁNG LÊN LẠI
    // ==========================================
    protected override void OnResume()
    {
        base.OnResume();
        ReportOnlineStatus("Quay lại ứng dụng");
    }

    // ==========================================
    // 🌟 LOGIC BÁO CÁO SERVER
    // ==========================================
    private void ReportOnlineStatus(string actionName)
    {
        int userId = Preferences.Get("UserId", -1);
        string guestId = Preferences.Get("AnonymousDeviceId", "");

        // Nếu đã từng lấy thẻ (Khách hoặc User) thì mới báo cáo
        if (userId >= 0 || !string.IsNullOrEmpty(guestId))
        {
            ApiService apiService = new ApiService();
            _ = apiService.TrackActionAsync(actionName);
        }
    }
}