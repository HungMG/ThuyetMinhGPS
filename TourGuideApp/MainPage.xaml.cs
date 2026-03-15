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

    // Hàm này chạy ngay khi màn hình vừa hiện lên
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // 1. Chạy hàm tạo dữ liệu mẫu
        await _dbService.SeedDataAsync();

        // 2. Lấy toàn bộ dữ liệu ra
        var danhSachPOI = await _dbService.GetAllPOIsAsync();

        // 3. Đổ dữ liệu vào giao diện
        poiListView.ItemsSource = danhSachPOI;
    }
}