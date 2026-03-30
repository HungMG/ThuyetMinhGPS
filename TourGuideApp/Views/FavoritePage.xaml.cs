using Mapsui.UI.Maui;
using Mapsui.Projections;
using TourGuideApp.Services;
using System.Linq;
using TourGuideApp.Models; // 👇 ĐÃ DỜI LÊN ĐÂY

namespace TourGuideApp.Views;

public partial class FavoritePage : ContentPage
{
    private DatabaseService _dbService;

    public FavoritePage()
    {
        InitializeComponent();
        _dbService = new DatabaseService();
    }

    // Mỗi lần mở tab này lên là tự động load lại dữ liệu mới nhất
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFavoritesAsync();
    }

    private async Task LoadFavoritesAsync()
    {
        var danhSachYeuThich = await _dbService.GetFavoritePOIsAsync();
        favoriteListView.ItemsSource = danhSachYeuThich;
    }


    // Sự kiện khi bấm nút Xóa (Thùng rác)
    private async void OnRemoveFavoriteClicked(object sender, EventArgs e)
    {
        var button = sender as ImageButton;
        var poiBiXoa = button?.CommandParameter as POI;

        if (poiBiXoa != null)
        {
            // 👇 ĐÃ SỬA: Đổi poiBiXoa.Name thành poiBiXoa.CurrentName 👇
            bool xacNhan = await DisplayAlert("Xác nhận", $"Bạn có chắc muốn bỏ '{poiBiXoa.CurrentName}' khỏi danh sách yêu thích?", "Đồng ý", "Hủy");

            if (xacNhan)
            {
                poiBiXoa.IsFavorite = false;
                await _dbService.UpdatePOIAsync(poiBiXoa);
                await LoadFavoritesAsync();
            }
        }
    }
}