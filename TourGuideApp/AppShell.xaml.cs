namespace TourGuideApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("mappage", typeof(TourGuideApp.Views.MapPage));
        }
    }
}
