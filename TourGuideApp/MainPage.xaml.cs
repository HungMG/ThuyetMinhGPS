using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using TourGuideApp.Models;
using TourGuideApp.Services;
using TourGuideApp.Views;

namespace TourGuideApp;

public partial class MainPage : ContentPage
{
    // 1. ĐỔI SANG DÙNG API SERVICE
    private ApiService _apiService;
    private NarrationEngine _narrationEngine = new NarrationEngine();

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Mặc định chọn giọng Tiếng Việt nếu chưa chọn
        if (NarrationLangPicker.SelectedIndex == -1) NarrationLangPicker.SelectedIndex = 0;
        LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var danhSachTour = await _apiService.GetToursAsync();
        tourListView.ItemsSource = null;
        tourListView.ItemsSource = danhSachTour;

        var tatCaPoi = await _apiService.GetPOIsAsync();
        poiListView.ItemsSource = null;
        poiListView.ItemsSource = tatCaPoi;
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

    private async void OnRefreshing(object sender, EventArgs e)
    {
        // Bước 1: Gọi hàm hút dữ liệu mới nhất từ mạng về
        await LoadDataAsync();

        // Bước 2: Tải xong rồi thì giấu cái vòng xoay (Loading) đi
        mainRefreshView.IsRefreshing = false;
    }

    // 🌟 CHỈ ĐỔI GIỌNG ĐỌC, KHÔNG ĐỔI GIAO DIỆN 🌟
    private async void OnTourPlayAudioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Tour tour)
        {
            string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };

            // XÓA BỎ mấy câu cứng nhắc cũ đi.
            // Bây giờ nó sẽ đọc: Tên Tour + Lời giới thiệu (lấy từ Web) + Thời gian dự kiến.
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