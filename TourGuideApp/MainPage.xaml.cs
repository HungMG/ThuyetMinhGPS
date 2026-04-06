using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using TourGuideApp.Models;
using TourGuideApp.Services;
using TourGuideApp.Views;
using System.Linq;
using System.Globalization;

namespace TourGuideApp;

public partial class MainPage : ContentPage
{
    private ApiService _apiService;
    private DatabaseService _dbService;
    private NarrationEngine _narrationEngine = new NarrationEngine();
    private Location _userLocation;
    private List<POI> _allPOIsCache = new List<POI>();

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        _dbService = new DatabaseService();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (NarrationLangPicker.SelectedIndex == -1) NarrationLangPicker.SelectedIndex = 0;
        LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        var danhSachTour = await _dbService.GetAllToursAsync();
        if (danhSachTour != null)
        {
            tourListView.ItemsSource = null;
            tourListView.ItemsSource = danhSachTour;
        }

        _allPOIsCache = await _dbService.GetAllPOIsAsync() ?? new List<POI>();
        await GetUserLocationAsync();

        if (_userLocation != null && _allPOIsCache.Any())
        {
            foreach (var poi in _allPOIsCache)
            {
                var poiLoc = new Location(poi.Latitude, poi.Longitude);
                poi.DistanceFromUser = Location.CalculateDistance(_userLocation, poiLoc, DistanceUnits.Kilometers);
            }
            _allPOIsCache = _allPOIsCache.OrderBy(p => p.DistanceFromUser).ToList();
        }

        // 🌟 Bơm dữ liệu vào BindableLayout thay vì CollectionView
        BindableLayout.SetItemsSource(poiStackLayout, _allPOIsCache);
    }

    private async Task GetUserLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted) status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                _userLocation = await Geolocation.GetLastKnownLocationAsync();
                if (_userLocation == null) _userLocation = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3)));
            }
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }

    // ==========================================================
    // 🌟 HIỆU ỨNG TỰ CODE: MỞ POPUP MƯỢT MÀ
    // ==========================================================
    private void OnTourSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Tour selectedTour)
        {
            var poisInTour = _allPOIsCache.Where(p => p.TourId == selectedTour.Id).ToList();
            if (_userLocation != null) poisInTour = poisInTour.OrderBy(p => p.DistanceFromUser).ToList();

            // Đổ dữ liệu vào Popup
            lblPopupTourName.Text = $"📍 {selectedTour.CurrentName}";
            BindableLayout.SetItemsSource(popupPoiStackLayout, poisInTour);

            // BẬT LỚP PHỦ ĐEN
            blackOverlay.IsVisible = true;
            blackOverlay.FadeTo(0.5, 250); // Mờ dần trong 0.25s

            // TRƯỢT POPUP TỪ DƯỚI LÊN
            tourPoiPopup.TranslateTo(0, 0, 350, Easing.SpringOut);

            // Bỏ highlight thẻ Tour
            ((CollectionView)sender).SelectedItem = null;
        }
    }

    // ==========================================================
    // 🌟 ĐÃ SỬA: SỰ KIỆN CHẠM TRỰC TIẾP VÀO THẺ TOUR
    // ==========================================================
    private void OnTourCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Tour selectedTour)
        {
            // Ép hệ thống chạy trên luồng Giao diện chính (MainThread)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var poisInTour = _allPOIsCache.Where(p => p.TourId == selectedTour.Id).ToList();
                if (_userLocation != null)
                {
                    poisInTour = poisInTour.OrderBy(p => p.DistanceFromUser).ToList();
                }

                // Đổ dữ liệu vào Popup
                lblPopupTourName.Text = $"📍 {selectedTour.CurrentName}";
                BindableLayout.SetItemsSource(popupPoiStackLayout, poisInTour);

                // 1. BẬT LỚP PHỦ ĐEN MỜ DẦN
                blackOverlay.IsVisible = true;
                blackOverlay.FadeTo(0.5, 250);

                // 2. ÉP POPUP HIỆN LÊN VÀ TRƯỢT TỪ DƯỚI LÊN
                tourPoiPopup.IsVisible = true; // Chắc cú 100% hiển thị
                tourPoiPopup.TranslateTo(0, 0, 350, Easing.SpringOut);
            });
        }
    }

    // ==========================================================
    // 🌟 HIỆU ỨNG TỰ CODE: ĐÓNG POPUP
    // ==========================================================
    private async void OnClosePopupTapped(object sender, TappedEventArgs e)
    {
        // TRƯỢT POPUP XUỐNG ĐÁY LẠI
        await tourPoiPopup.TranslateTo(0, 1000, 250, Easing.CubicIn);

        // TẮT LỚP PHỦ ĐEN
        await blackOverlay.FadeTo(0, 250);
        blackOverlay.IsVisible = false;
    }

    // ==========================================================
    // SỰ KIỆN: KHI BẤM VÀO THẺ POI (BÊN MAIN PAGE)
    // ==========================================================
    private async void OnPoiCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI selectedPoi)
        {
            tourPoiPopup.TranslationY = 1000;
            blackOverlay.IsVisible = false;

            Preferences.Set("TargetTourId", selectedPoi.TourId);

            // 🌟 THÊM 2 DÒNG NÀY ĐỂ GỬI TỌA ĐỘ CHÍNH XÁC QUA BẢN ĐỒ 🌟
            Preferences.Set("TargetPoiLat", selectedPoi.Latitude);
            Preferences.Set("TargetPoiLon", selectedPoi.Longitude);

            await Shell.Current.GoToAsync("//MapPage");
        }
    }
    private async void OnRefreshing(object sender, EventArgs e)
    {
        try
        {
            bool isTourSuccess = await _apiService.SyncToursAsync(_dbService);
            bool isPoiSuccess = await _apiService.SyncPOIsAsync(_dbService);
            if (isTourSuccess || isPoiSuccess) await LoadDataAsync();
        }
        catch { }
        finally { mainRefreshView.IsRefreshing = false; }
    }

    private async void OnTourPlayAudioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Tour tour)
        {
            string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };
            string introText = lang switch { "en" => $"{tour.Name_EN}. {tour.Description_EN}", _ => $"{tour.Name_VI}. {tour.Description_VI}" };
            await _narrationEngine.SpeakAsync(introText, lang);
        }
    }

    private async void OnPoiPlayAudioTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI poi)
        {
            string lang = NarrationLangPicker.SelectedIndex switch { 1 => "en", 2 => "zh", 3 => "ko", 4 => "ja", _ => "vi" };
            string textToRead = lang switch { "en" => $"{poi.Name_EN}. {poi.Description_EN}", _ => $"{poi.Name_VI}. {poi.Description_VI}" };
            await _narrationEngine.SpeakAsync(textToRead, lang);
        }
    }
    // ==========================================================
    // 🌟 DẪN ĐƯỜNG ĐI TOUR BẰNG GOOGLE MAPS (ĐÃ FIX LỖI)
    // ==========================================================
    private async void OnNavigateTourClicked(object sender, EventArgs e)
    {
        // 🌟 Dùng BindableLayout.GetItemsSource để móc dữ liệu ra
        var poisInTour = BindableLayout.GetItemsSource(popupPoiStackLayout) as List<POI>;

        if (poisInTour == null || !poisInTour.Any()) return;

        try
        {
            var lastPoi = poisInTour.Last();
            string destination = $"{lastPoi.Latitude.ToString(CultureInfo.InvariantCulture)},{lastPoi.Longitude.ToString(CultureInfo.InvariantCulture)}";
            string url = $"https://www.google.com/maps/dir/?api=1&destination={destination}&travelmode=driving";

            if (poisInTour.Count > 1)
            {
                var waypointsList = poisInTour.Take(poisInTour.Count - 1)
                                              .Select(p => $"{p.Latitude.ToString(CultureInfo.InvariantCulture)},{p.Longitude.ToString(CultureInfo.InvariantCulture)}");
                string waypointsStr = string.Join("|", waypointsList);
                url += $"&waypoints={waypointsStr}";
            }

            await Launcher.OpenAsync(url);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể mở bản đồ chỉ đường: " + ex.Message, "OK");
        }
    }
}