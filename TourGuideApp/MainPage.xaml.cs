using Microsoft.Maui.Controls;
using TourGuideApp.Models;  
using TourGuideApp.Services;

namespace TourGuideApp;

public partial class MainPage : ContentPage
{
    private DatabaseService _dbService;

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
}