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
    }
}
