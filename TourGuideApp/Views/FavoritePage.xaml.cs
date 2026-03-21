using TourGuideApp.Services;
using TourGuideApp.Models; // Đổi lại cho khớp với namespace của bạn

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
        // 1. Xác định xem người dùng đang bấm vào món nào
        var button = sender as ImageButton;
        var poiBiXoa = button?.CommandParameter as POI;

        if (poiBiXoa != null)
        {
            // 2. Hỏi lại cho chắc ăn
            bool xacNhan = await DisplayAlert("Xác nhận", $"Bạn có chắc muốn bỏ '{poiBiXoa.Name}' khỏi danh sách yêu thích?", "Đồng ý", "Hủy");

            if (xacNhan)
            {
                // 3. Tắt cờ yêu thích và lưu xuống DB
                poiBiXoa.IsFavorite = false;
                await _dbService.UpdatePOIAsync(poiBiXoa);

                // 4. Load lại danh sách cho nó biến mất khỏi màn hình
                await LoadFavoritesAsync();
            }
        }
    }
}