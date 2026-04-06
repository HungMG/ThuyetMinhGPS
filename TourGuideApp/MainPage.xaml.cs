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

    private List<POI> _allPois = new List<POI>();
    private List<Tour> _allTours = new List<Tour>();

    public MainPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
        _dbService = new DatabaseService();
    }

    // 🌟 1. SỬA LẠI: Thêm async và await để App kiên nhẫn chờ lấy Database xong mới chạy tiếp
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (NarrationLangPicker.SelectedIndex == -1) NarrationLangPicker.SelectedIndex = 0;
        await LoadDataAsync();
    }

    // ==========================================================
    // 🌟 TẢI DỮ LIỆU & TÍNH KHOẢNG CÁCH
    // ==========================================================
    // ==========================================================
    // 🌟 TẢI DỮ LIỆU (ĐÃ FIX LỖI KẸT GPS GÂY TRẮNG MÀN HÌNH)
    // ==========================================================
    private async Task LoadDataAsync()
    {
        // 1. TẢI NHANH TỪ DATABASE
        var danhSachTour = await _dbService.GetAllToursAsync();
        if (danhSachTour != null) _allTours = danhSachTour;

        _allPois = await _dbService.GetAllPOIsAsync() ?? new List<POI>();

        // 2. ÉP ĐỔ DỮ LIỆU LÊN MÀN HÌNH NGAY LẬP TỨC CHO KHÁCH THẤY
        MainThread.BeginInvokeOnMainThread(() =>
        {
            tourListView.ItemsSource = null;
            tourListView.ItemsSource = _allTours;
            BindableLayout.SetItemsSource(poiStackLayout, _allPois);
        });

        // 3. RẢNH RỖI MỚI ĐI XIN QUYỀN VÀ DÒ GPS (Chạy ngầm không làm đơ App)
        await GetUserLocationAsync();

        // 4. CÓ GPS RỒI THÌ TÍNH SỐ KM VÀ SẮP XẾP LẠI (Cập nhật lần 2)
        if (_userLocation != null && _allPois.Any())
        {
            foreach (var poi in _allPois)
            {
                var poiLoc = new Location(poi.Latitude, poi.Longitude);
                poi.DistanceFromUser = Location.CalculateDistance(_userLocation, poiLoc, DistanceUnits.Kilometers);
            }

            _allPois = _allPois.OrderBy(p => p.DistanceFromUser).ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                BindableLayout.SetItemsSource(poiStackLayout, _allPois);
            });
        }
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
    // 🌟 THANH TÌM KIẾM
    // ==========================================================
    private void FilterData(string keyword)
    {
        keyword = keyword?.ToLower();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            tourListView.ItemsSource = _allTours;
            BindableLayout.SetItemsSource(poiStackLayout, _allPois);
            return;
        }

        var filteredPois = _allPois.Where(p =>
            (!string.IsNullOrEmpty(p.CurrentName) && p.CurrentName.ToLower().Contains(keyword)) ||
            (!string.IsNullOrEmpty(p.CurrentDescription) && p.CurrentDescription.ToLower().Contains(keyword))
        ).ToList();

        var filteredTours = _allTours.Where(t =>
            (!string.IsNullOrEmpty(t.Name_VI) && t.Name_VI.ToLower().Contains(keyword)) ||
            (!string.IsNullOrEmpty(t.Description_VI) && t.Description_VI.ToLower().Contains(keyword)) ||
            (!string.IsNullOrEmpty(t.Name_EN) && t.Name_EN.ToLower().Contains(keyword)) ||
            (!string.IsNullOrEmpty(t.Description_EN) && t.Description_EN.ToLower().Contains(keyword))
        ).ToList();

        tourListView.ItemsSource = filteredTours;
        BindableLayout.SetItemsSource(poiStackLayout, filteredPois);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterData(e.NewTextValue);
    }

    private void OnSearchButtonPressed(object sender, EventArgs e)
    {
        FilterData((sender as SearchBar)?.Text);
    }

    // ==========================================================
    // 🌟 SỰ KIỆN CHẠM TRỰC TIẾP VÀO THẺ TOUR
    // ==========================================================
    private void OnTourCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is Tour selectedTour)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var poisInTour = _allPois.Where(p => p.TourId == selectedTour.Id).ToList();

                if (_userLocation != null)
                {
                    poisInTour = poisInTour.OrderBy(p => p.DistanceFromUser).ToList();
                }

                lblPopupTourName.Text = $"📍 {selectedTour.CurrentName}";
                BindableLayout.SetItemsSource(popupPoiStackLayout, poisInTour);

                blackOverlay.IsVisible = true;
                blackOverlay.FadeTo(0.5, 250);

                tourPoiPopup.IsVisible = true;
                tourPoiPopup.TranslateTo(0, 0, 350, Easing.SpringOut);
            });
        }
    }

    private async void OnClosePopupTapped(object sender, TappedEventArgs e)
    {
        await tourPoiPopup.TranslateTo(0, 1000, 250, Easing.CubicIn);
        await blackOverlay.FadeTo(0, 250);
        blackOverlay.IsVisible = false;
    }

    // ==========================================================
    // SỰ KIỆN: KHI BẤM VÀO THẺ POI (NHẢY SANG BẢN ĐỒ)
    // ==========================================================
    private async void OnPoiCardTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is POI selectedPoi)
        {
            tourPoiPopup.TranslationY = 1000;
            blackOverlay.IsVisible = false;

            Preferences.Set("TargetTourId", selectedPoi.TourId);
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
            // 🌟 3. SỬA LẠI: Chờ LoadDataAsync xong mới tắt vòng xoay
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
    // 🌟 DẪN ĐƯỜNG ĐI TOUR BẰNG GOOGLE MAPS
    // ==========================================================
    private async void OnNavigateTourClicked(object sender, EventArgs e)
    {
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

            await Launcher.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Lỗi", "Không thể mở bản đồ chỉ đường: " + ex.Message, "OK");
        }
    }
}