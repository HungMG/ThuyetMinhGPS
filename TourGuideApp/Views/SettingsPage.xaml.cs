using System.Globalization;
using TourGuideApp.Resources.Languages;

namespace TourGuideApp.Views;

public partial class SettingsPage : ContentPage
{
    private bool _isInitialized = false;

    public SettingsPage()
    {
        InitializeComponent();
        LoadCurrentLanguage();
    }

    // Nạp ngôn ngữ hiện tại đang dùng lên Picker
    private void LoadCurrentLanguage()
    {
        string currentLang = Preferences.Get("AppLanguage", "vi");
        LanguagePicker.SelectedIndex = currentLang switch
        {
            "en" => 1,
            "zh" => 2,
            "ko" => 3,
            "ja" => 4,
            _ => 0 // Mặc định là Tiếng Việt
        };
        _isInitialized = true;
    }

    // Sự kiện khi người dùng chọn một ngôn ngữ khác trong hộp thoại
    private async void OnLanguageChanged(object sender, EventArgs e) // 👈 Nhớ thêm chữ 'async' ở đây nha
    {
        // Chặn sự kiện lúc vừa mở trang lên
        if (!_isInitialized) return;

        var picker = (Picker)sender;
        int selectedIndex = picker.SelectedIndex;

        string newLangCode = selectedIndex switch
        {
            1 => "en",
            2 => "zh",
            3 => "ko",
            4 => "ja",
            _ => "vi"
        };

        // Nếu người dùng chọn ngôn ngữ đang xài thì bỏ qua
        if (Preferences.Get("AppLanguage", "vi") == newLangCode) return;

        // 1. Lưu ngôn ngữ mới
        Preferences.Set("AppLanguage", newLangCode);

        // 2. Chuyển đổi Culture
        string cultureCode = newLangCode switch
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

        // 🌟 BÍ KÍP TRỊ VĂNG APP NẰM Ở ĐÂY 🌟
        // Bắt hệ thống đợi 200 mili-giây cho cái Menu Picker đóng lại an toàn tuyệt đối
        await Task.Delay(200);

        // Nhờ MainThread (Luồng chính) từ từ thay đổi giao diện để không bị sốc
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current.MainPage = new AppShell();
        });
    }

    // Cái hàm của tính năng Offline (của bạn)
    private async void OnOfflineTapped(object sender, EventArgs e)
    {
        // await Navigation.PushAsync(new OfflinePage());
    }
}