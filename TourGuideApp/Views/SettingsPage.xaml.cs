using System.Globalization;
using TourGuideApp.Resources.Languages;
using TourGuideApp.Services;
using TourGuideApp.Models; // Bổ sung để xài Model POI

namespace TourGuideApp.Views;

public partial class SettingsPage : ContentPage
{
    private bool _isInitialized = false;
    private ApiService _apiService = new ApiService(); // Khởi tạo bộ gọi API

    public SettingsPage()
    {
        InitializeComponent();
        LoadCurrentLanguage();
    }

    // ==========================================================
    // 🌟 SỰ KIỆN: KHI TRANG VỪA MỞ LÊN
    // ==========================================================
    protected override void OnAppearing()
    {
        base.OnAppearing();

        Dispatcher.Dispatch(async () =>
        {
            UpdateAuthButton();
            await LoadUserData(); // Gọi API lấy dữ liệu bài đăng
        });
    }

    // 🌟 HÀM MỚI: TẢI DANH SÁCH ĐỊA ĐIỂM CỦA TÔI
    private async Task LoadUserData()
    {
        int userId = Preferences.Get("UserId", -1);

        if (userId <= 0)
        {
            // Trạng thái Khách vãng lai
            pnlGuest.IsVisible = true;
            pnlLoggedIn.IsVisible = false;
        }
        else
        {
            // Trạng thái Đã đăng nhập
            pnlGuest.IsVisible = false;
            pnlLoggedIn.IsVisible = true;
            lblUserName.Text = Preferences.Get("UserName", "Thành viên");

            // Kéo dữ liệu POI từ Server về
            var myPois = await _apiService.GetMyPoisAsync(userId);

            // Cập nhật giao diện danh sách
            BindableLayout.SetItemsSource(myPoiLayout, myPois);

            // Hiện chữ "Chưa đóng góp" nếu danh sách rỗng
            lblEmptyPoi.IsVisible = (myPois == null || myPois.Count == 0);
        }

        // Tắt vòng xoay làm mới
        refreshView.IsRefreshing = false;
    }

    // Sự kiện kéo xuống để Refresh
    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadUserData();
    }

    // Sự kiện bấm nút (+) Thêm mới
    private async void OnAddNewPoiClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddPoiPage());
    }

    // Sự kiện bấm nút (✏️) Sửa
    private async void OnEditPoiTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI selectedPoi)
        {
            // Nếu bài đã lên App (Status = 1), cảnh báo người dùng trước khi sửa
            if (selectedPoi.ApprovalStatus == 1)
            {
                bool confirm = await DisplayAlert("Cảnh báo", "Bài này đang hiện trên Bản đồ chung. Nếu sếp sửa, nó sẽ bị gỡ xuống để Admin duyệt lại từ đầu. Sếp có chắc chắn muốn sửa không?", "Chơi luôn", "Hủy");

                if (!confirm) return; // Khách đổi ý thì bỏ qua
            }

            // Chuyển sang trang sửa bài, nhét cái selectedPoi vào hành lý mang theo
            await Navigation.PushAsync(new EditPoiPage(selectedPoi));
        }
    }

    private void UpdateAuthButton()
    {
        if (btnAuthAction == null) return;

        int userId = Preferences.Get("UserId", -1);

        if (userId > 0) // Đã đăng nhập thật
        {
            // 🌟 Gắn từ khóa Đa Ngôn Ngữ cho nút Đăng Xuất
            btnAuthAction.Text = AppLang.SettingsAuthLogout;
            btnAuthAction.BackgroundColor = Colors.Transparent;
            btnAuthAction.TextColor = Color.FromArgb("#E74C3C"); // Chữ đỏ
            btnAuthAction.BorderColor = Color.FromArgb("#E74C3C");
            btnAuthAction.BorderWidth = 1;
        }
        else // Khách Ẩn danh (0) hoặc chưa có gì (-1)
        {
            // 🌟 Gắn từ khóa Đa Ngôn Ngữ cho nút Đăng Nhập / Tạo Tài Khoản
            btnAuthAction.Text = AppLang.SettingsAuthLogin;
            btnAuthAction.BackgroundColor = Color.FromArgb("#27AE60"); // Xanh lá
            btnAuthAction.TextColor = Colors.White;
            btnAuthAction.BorderWidth = 0;
        }
    }

    // ==========================================================
    // 🌟 SỰ KIỆN: BẤM NÚT ĐĂNG XUẤT / ĐĂNG NHẬP
    // ==========================================================
    private async void OnAuthActionClicked(object sender, EventArgs e)
    {
        int userId = Preferences.Get("UserId", -1);

        if (userId > 0)
        {
            // Tạm thời để popup tiếng Việt, nếu sếp muốn đổi thì tạo thêm biến đa ngôn ngữ cho Popup nhé
            bool confirm = await DisplayAlert("Đăng Xuất", "Sếp có chắc chắn muốn đăng xuất không?", "Có", "Không");
            if (confirm)
            {
                // Xé bỏ thẻ căn cước
                Preferences.Remove("UserId");
                Preferences.Remove("UserName");
                Preferences.Remove("UserRole");

                // Đá văng về lại trang Đăng Nhập
                Application.Current.MainPage = new LoginPage();
            }
        }
        else
        {
            // Chuyển sang trang Đăng nhập nếu đang là khách
            Application.Current.MainPage = new LoginPage();
        }
    }

    // ==========================================================
    // CÁC HÀM CŨ CỦA SẾP GIỮ NGUYÊN (NGÔN NGỮ VÀ OFFLINE)
    // ==========================================================
    private void LoadCurrentLanguage()
    {
        string currentLang = Preferences.Get("AppLanguage", "vi");
        LanguagePicker.SelectedIndex = currentLang switch
        {
            "en" => 1,
            "zh" => 2,
            "ko" => 3,
            "ja" => 4,
            _ => 0
        };
        _isInitialized = true;
    }

    private async void OnLanguageChanged(object sender, EventArgs e)
    {
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

        if (Preferences.Get("AppLanguage", "vi") == newLangCode) return;

        Preferences.Set("AppLanguage", newLangCode);

        Preferences.Set("TTSLanguage", newLangCode);

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

        await Task.Delay(200);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current.MainPage = new AppShell();
        });
    }

    private async void OnOfflineTapped(object sender, TappedEventArgs e)
    {
        if (sender is Frame frame)
        {
            await frame.ScaleTo(0.95, 100);
            await frame.ScaleTo(1.0, 100);
        }
        await Navigation.PushAsync(new OfflinePage());
    }
}