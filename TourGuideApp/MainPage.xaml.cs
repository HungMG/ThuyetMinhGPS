using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using TourGuideApp.Models;
using TourGuideApp.Services;
using TourGuideApp.Views;

namespace TourGuideApp;

public partial class MainPage : ContentPage
{
    // 🌟 THÊM ÔNG THỦ KHO VÀO ĐỘI HÌNH
    private ApiService _apiService;
    private DatabaseService _dbService;
    private NarrationEngine _narrationEngine = new NarrationEngine();

    public MainPage()
    {
        InitializeComponent();

        // Khởi tạo thẻ nhân viên cho cả 2
        _apiService = new ApiService();
        _dbService = new DatabaseService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Mặc định chọn giọng Tiếng Việt nếu chưa chọn
        if (NarrationLangPicker.SelectedIndex == -1) NarrationLangPicker.SelectedIndex = 0;
        LoadDataAsync();
    }

    // ==========================================================
    // 1. CHẾ ĐỘ OFFLINE-FIRST: CHỈ BỐC DỮ LIỆU TỪ NHÀ KHO SQLITE
    // ==========================================================
    private async Task LoadDataAsync()
    {
        // Bốc Tour từ kho ra
        var danhSachTour = await _dbService.GetAllToursAsync();
        if (danhSachTour != null)
        {
            tourListView.ItemsSource = null;
            tourListView.ItemsSource = danhSachTour;
        }

        // Bốc POI từ kho ra
        var tatCaPoi = await _dbService.GetAllPOIsAsync();
        if (tatCaPoi != null)
        {
            poiListView.ItemsSource = null;
            poiListView.ItemsSource = tatCaPoi;
        }
    }

    private async void OnTourSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Tour selectedTour)
        {
            await Shell.Current.Navigation.PushAsync(new MapPage(selectedTour.Id));
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void OnPoiSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is POI selectedPoi)
        {
            await Navigation.PushAsync(new MapPage(0));
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    // ==========================================================
    // 2. VUỐT LÀM MỚI: SAI THẰNG ĐƯA THƯ ĐI LẤY HÀNG VỀ CẤT KHO
    // ==========================================================
    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            // 1. Sai đưa thư đi lấy hàng
            bool isTourSuccess = await _apiService.SyncToursAsync(_dbService);
            bool isPoiSuccess = await _apiService.SyncPOIsAsync(_dbService);

            if (isTourSuccess || isPoiSuccess)
            {
                await LoadDataAsync();
            }
            else
            {
                await DisplayAlert("Chế độ Offline", "Không có kết nối mạng! Hiển thị dữ liệu cũ.", "Đã hiểu");
            }
        }
        catch (Exception ex)
        {
            // Nếu có lỗi ngầm, báo lên màn hình thay vì xoay hoài
            await DisplayAlert("Lỗi Đồng Bộ", "Đã có lỗi xảy ra: " + ex.Message, "OK");
        }
        finally
        {
            // 🌟 BÙA HỘ MỆNH: Dù thành công hay thất bại cũng bắt buộc tắt vòng xoay!
            mainRefreshView.IsRefreshing = false;
        }
    }

    // 🌟 CÁC HÀM ĐỌC THUYẾT MINH GIỮ NGUYÊN 100% 🌟
    private async void OnTourPlayAudioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Tour tour)
        {
            string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };

            string introText = lang switch
            {
                "en" => $"{tour.Name_EN}. {tour.Description_EN} Estimated time: {tour.EstimatedTime}.",
                "zh" => $"{tour.Name_ZH}。 {tour.Description_ZH} 预计时间：{tour.EstimatedTime}。",
                "ko" => $"{tour.Name_KO}. {tour.Description_KO} 예상 시간: {tour.EstimatedTime}.",
                "ja" => $"{tour.Name_JA}。 {tour.Description_JA} 所要時間：{tour.EstimatedTime}。",
                _ => $"{tour.Name_VI}. {tour.Description_VI} Thời gian dự kiến là {tour.EstimatedTime}."
            };

            await _narrationEngine.SpeakAsync(introText, lang);
        }
    }

    private async void OnPoiPlayAudioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI poi)
        {
            string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };

            string textToRead = lang switch
            {
                "en" => $"{poi.Name_EN}. {poi.Description_EN}",
                "zh" => $"{poi.Name_ZH}. {poi.Description_ZH}",
                "ko" => $"{poi.Name_KO}. {poi.Description_KO}",
                "ja" => $"{poi.Name_JA}. {poi.Description_JA}",
                _ => $"{poi.Name_VI}. {poi.Description_VI}"
            };
            await _narrationEngine.SpeakAsync(textToRead, lang);
        }
    }
}