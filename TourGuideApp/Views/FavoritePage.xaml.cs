using TourGuideApp.Services;
using TourGuideApp.Models;

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
        // 🌟 Lấy danh sách tim đỏ từ Database của sếp (Hàm số 5)
        var danhSachYeuThich = await _dbService.GetFavoritePOIsAsync();

        // Nạp vào giao diện
        favoriteListView.ItemsSource = danhSachYeuThich;
    }

    // Sự kiện khi bấm nút Xóa (Thùng rác)
    private async void OnRemoveFavoriteClicked(object sender, EventArgs e)
    {
        var button = sender as ImageButton;
        var poiBiXoa = button?.CommandParameter as POI;

        if (poiBiXoa != null)
        {
            bool xacNhan = await DisplayAlert("Xác nhận", $"Bạn có chắc muốn bỏ '{poiBiXoa.CurrentName}' khỏi danh sách yêu thích?", "Đồng ý", "Hủy");

            if (xacNhan)
            {
                poiBiXoa.IsFavorite = false;

                // 🌟 Gọi hàm cập nhật của sếp (Hàm số 6)
                await _dbService.UpdatePOIAsync(poiBiXoa);

                // Load lại danh sách sau khi xóa
                await LoadFavoritesAsync();
            }
        }
    }
}