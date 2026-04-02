using Microsoft.Maui.Controls;
using TourGuideApp.Models;
using TourGuideApp.Services;
using TourGuideApp.Views;

namespace TourGuideApp;

public partial class MainPage : ContentPage
{
    private DatabaseService _dbService;
    private NarrationEngine _narrationEngine = new NarrationEngine();

    public MainPage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Mặc định chọn giọng Tiếng Việt nếu chưa chọn
        if (NarrationLangPicker.SelectedIndex == -1) NarrationLangPicker.SelectedIndex = 0;
        LoadDataAsync();
    }

    private async void LoadDataAsync()
    {
        await _dbService.SeedDataAsync();

        var danhSachTour = await _dbService.GetAllToursAsync();
        tourListView.ItemsSource = null;
        tourListView.ItemsSource = danhSachTour;

        var tatCaPoi = await _dbService.GetAllPOIsAsync();
        var diemTuDo = tatCaPoi.Where(p => p.TourId == 0).ToList();
        poiListView.ItemsSource = null;
        poiListView.ItemsSource = diemTuDo;
    }

    private async void OnTourSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Tour selectedTour)
        {
            // Dùng Shell để điều hướng cho chắc chắn nhảy
            // Chúng ta truyền TourId qua Constructor của MapPage ở bước sau
            await Shell.Current.Navigation.PushAsync(new MapPage(selectedTour.Id));

            ((CollectionView)sender).SelectedItem = null;
        }
    }

    private async void OnPoiSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is POI selectedPoi)
        {
            // Nếu bấm vào điểm tự do, truyền TourId = 0 để nó chỉ hiện các điểm tự do
            await Navigation.PushAsync(new MapPage(0));
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    // 🌟 CHỈ ĐỔI GIỌNG ĐỌC, KHÔNG ĐỔI GIAO DIỆN 🌟
    private async void OnTourPlayAudioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Tour tour)
        {
            string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };

            // Lấy đúng cột ngôn ngữ để đọc, không dùng CurrentName
            string introText = lang switch
            {
                "en" => $"Welcome to the {tour.Name_EN} tour. Estimated time: {tour.EstimatedTime}.",
                "zh" => $"欢迎参加：{tour.Name_ZH} 路线。 预计时间：{tour.EstimatedTime}。",
                "ko" => $"투어에 오신 것을 환영합니다: {tour.Name_KO}. 예상 시간: {tour.EstimatedTime}.",
                "ja" => $"ツアーへようこそ：{tour.Name_JA}。 所要時間：{tour.EstimatedTime}。",
                _ => $"Chào mừng bạn đến với lộ trình: {tour.Name_VI}. Thời gian dự kiến là {tour.EstimatedTime}."
            };
            await _narrationEngine.SpeakAsync(introText, lang);
        }
    }

    private async void OnPoiPlayAudioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI poi)
        {
            string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };

            // Lấy đúng cột ngôn ngữ để đọc
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