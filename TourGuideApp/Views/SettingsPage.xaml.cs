namespace TourGuideApp.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }
        private async void OnOfflineTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OfflinePage());
        }
        private async void OnLogoutTapped(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Đăng xuất", "Bạn có chắc muốn đăng xuất?", "Có", "Không");

            // if (confirm)
            // {
                // TODO: Xóa dữ liệu đăng nhập (token, user...)
                // Preferences.Remove("user");

                // Quay về trang login
                // await Navigation.PushAsync(new LoginPage());
            // }
        }

    }
}
