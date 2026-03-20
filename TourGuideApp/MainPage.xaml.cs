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

    protected override async void OnAppearing()
    {
        base.OnAppearing();


        await _dbService.SeedDataAsync();

        var danhSachPOI = await _dbService.GetAllPOIsAsync();

        poiListView.ItemsSource = danhSachPOI;
    }

    public async Task TriggerTourGuideAsync(double currentLat, double currentLon)
    {
        // 1. Kéo toàn bộ data từ Database
        var allPOIs = await _dbService.GetAllPOIsAsync();

        // 2. Geofence quét tìm điểm phù hợp nhất (cooldown 5 phút)
        var bestPOI = _geofenceEngine.GetBestPOIToTrigger(currentLat, currentLon, allPOIs, 5);

        if (bestPOI != null)
        {
            // 3. Đưa văn bản cho TTS đọc
            // Đọc Tiếng Việt
            await _narrationEngine.SpeakAsync("Xin chào, đây là Bưu Điện.", "vi");

            // Đọc Tiếng Anh
            await _narrationEngine.SpeakAsync("Hello, this is the Post Office.", "en");

            // Đọc Tiếng Trung
            await _narrationEngine.SpeakAsync("你好，这是邮局。", "zh");

            // 4. Cập nhật thời gian vào Database để chống lặp
            await _dbService.UpdateLastPlayedTimeAsync(bestPOI.Id);
        }
    }
    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = e.CurrentSelection.FirstOrDefault();

        if (selectedItem != null)
        {
            await Navigation.PushAsync(new DetailPage
            {
                BindingContext = selectedItem
            });
        }
    }
}