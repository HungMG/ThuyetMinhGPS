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
        InitializeComponent(); // Dám cá là dán vào xong nó XANH MƯỢT luôn!
        _dbService = new DatabaseService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _dbService.SeedDataAsync();
        var danhSachPOI = await _dbService.GetAllPOIsAsync();

        // Nó đã nhìn thấy poiListView!
        poiListView.ItemsSource = danhSachPOI;
    }

    public async Task TriggerTourGuideAsync(double currentLat, double currentLon)
    {
        var allPOIs = await _dbService.GetAllPOIsAsync();
        var bestPOI = _geofenceEngine.GetBestPOIToTrigger(currentLat, currentLon, allPOIs, 5);

        if (bestPOI != null)
        {
            await _narrationEngine.SpeakAsync("Xin chào, đây là Bưu Điện.", "vi");
            await _narrationEngine.SpeakAsync("Hello, this is the Post Office.", "en");
            await _narrationEngine.SpeakAsync("你好，这是邮局。", "zh");
            await _dbService.UpdateLastPlayedTimeAsync(bestPOI.Id);
        }
    }

    private async void OnItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        var selectedItem = e.SelectedItem;
        if (selectedItem != null)
        {
            await Navigation.PushAsync(new DetailPage
            {
                BindingContext = selectedItem
            });
            // Bỏ chọn sau khi bấm
            ((ListView)sender).SelectedItem = null;
        }
    }
}