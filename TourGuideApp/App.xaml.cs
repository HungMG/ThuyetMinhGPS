using System.Globalization;
using TourGuideApp.Views;
using TourGuideApp.Resources.Languages;
using TourGuideApp.Services;

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

        // ========================================================
        // 🌟 MÁY TẠO NHỊP TIM (HEARTBEAT) - REALTIME 1 PHÚT
        // ========================================================
        Application.Current.Dispatcher.StartTimer(TimeSpan.FromSeconds(45), () =>
        {
            int userId = Preferences.Get("UserId", -1);
            string guestId = Preferences.Get("AnonymousDeviceId", "");

            // Chỉ đập nhịp tim nếu đã từng lấy thẻ (Khách hoặc User)
            if (userId >= 0 || !string.IsNullOrEmpty(guestId))
            {
                // Gọi API ngầm báo cáo là đang Online
                ApiService apiService = new ApiService();
                _ = apiService.TrackActionAsync("Heartbeat (Online)");
            }

            return true; // Trả về true để vòng lặp này chạy mãi mãi chừng nào App còn mở
        });
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

        if (userId >= 0 || !string.IsNullOrEmpty(guestId))
        {
            ApiService apiService = new ApiService();
            _ = apiService.TrackActionAsync(actionName);
        }
    }

    // 🌟 VŨ KHÍ HẠNG NẶNG: TRỤC XUẤT NGƯỜI DÙNG
    public static void ForceLogout(string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            Preferences.Clear();
            await Application.Current.MainPage.DisplayAlert("Bị khóa", message, "Chấp nhận");
            Application.Current.MainPage = new NavigationPage(new Views.LoginPage());
        });
    }
}