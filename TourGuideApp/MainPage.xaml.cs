using Microsoft.Maui.Controls;
using TourGuideApp.Models;
using TourGuideApp.Services;
using TourGuideApp.Views;

namespace TourGuideApp;

public partial class MainPage : ContentPage
{
    private DatabaseService _dbService;
    private GeofenceEngine _geofenceEngine = new GeofenceEngine();
    private NarrationEngine _narrationEngine = new NarrationEngine();

    public MainPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 1. Gọi hàm nạp dữ liệu từ Database
        LoadDataAsync();

        // 2. Đặt mặc định cho Picker thuyết minh giống với ngôn ngữ App hiện tại
        if (NarrationLangPicker.SelectedIndex == -1)
        {
            string currentAppLang = Preferences.Get("AppLanguage", "vi");
            NarrationLangPicker.SelectedItem = currentAppLang;
        }
    }

    // 🔥 ĐÃ THÊM HÀM LOADDATA MÀ BẠN QUÊN 🔥
    private async void LoadDataAsync()
    {
        await _dbService.SeedDataAsync(); // Khởi tạo dữ liệu mẫu nếu chưa có
        var danhSachPOI = await _dbService.GetAllPOIsAsync();
        poiListView.ItemsSource = danhSachPOI; // Đổ dữ liệu lên màn hình
    }

    // 🔥 ĐÃ SỬA LỖI VĂNG APP KHI BẤM VÀO ĐỊA ĐIỂM (Dùng SelectionChangedEventArgs) 🔥
    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        // CollectionView trả về một danh sách (dù mình chọn Single), nên phải lấy cái đầu tiên
        var selectedItem = e.CurrentSelection.FirstOrDefault();

        if (selectedItem != null)
        {
            await Navigation.PushAsync(new DetailPage
            {
                BindingContext = selectedItem
            });

            // Bỏ chọn sau khi bấm (Ép đúng kiểu CollectionView)
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    // 🔥 HÀM PHÁT LOA ĐÃ SỬA CHUẨN XÁC THEO POI.CS 🔥
    private async void OnPlayAudioTapped(object sender, TappedEventArgs e)
    {
        var selectedPOI = e.Parameter as POI;
        if (selectedPOI == null) return;

        int selectedIndex = NarrationLangPicker.SelectedIndex;

        string narrationLang = "vi";
        string textToRead = selectedPOI.Description_VI;

        switch (selectedIndex)
        {
            case 1: // English
                narrationLang = "en";
                textToRead = selectedPOI.Description_EN;
                break;
            case 2: // Chinese
                narrationLang = "zh";
                textToRead = selectedPOI.Description_ZH;
                break;
            case 3: // Korean
                narrationLang = "ko";
                textToRead = selectedPOI.Description_KO;
                break;
            case 4: // Japanese
                narrationLang = "ja";
                textToRead = selectedPOI.Description_JA;
                break;
        }

        // 🌟 BÍ KÍP CHỐNG CÂM: Nếu cột ngôn ngữ đó trong Database bị trống (Null)
        // Mình tự động nhét tiếng Việt vào đọc tạm và báo cho khách biết!
        if (string.IsNullOrWhiteSpace(textToRead))
        {
            await DisplayAlert("Thông báo", "Dữ liệu ngôn ngữ này đang được cập nhật. Tạm thời phát Tiếng Việt nhé!", "OK");
            textToRead = selectedPOI.Description_VI;
            narrationLang = "vi";
        }

        // Phát loa!
        await _narrationEngine.SpeakAsync(textToRead, narrationLang);
    }
}