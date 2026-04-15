using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using TourGuideApp.Services;

namespace TourGuideApp.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService = new ApiService();

    public LoginPage()
    {
        InitializeComponent();
    }

    // ==========================================
    // 1. LUỒNG ĐĂNG NHẬP CHÍNH THỨC
    // ==========================================
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = txtUsername.Text?.Trim();
        string password = txtPassword.Text?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập tài khoản và mật khẩu!", "OK");
            return;
        }

        bool serverIsDown = false;

        try
        {
            // 1. THỬ ĐĂNG NHẬP ONLINE TRƯỚC
            var loginResult = await _apiService.LoginAsync(username, password);

            if (loginResult != null)
            {
                if (loginResult.UserId > 0)
                {
                    // 🌟 ĐĂNG NHẬP THÀNH CÔNG
                    Preferences.Set("UserId", loginResult.UserId);
                    Preferences.Set("UserName", loginResult.Username);
                    Preferences.Set("Role", loginResult.Role);

                    Preferences.Set("Offline_User", username);
                    Preferences.Set("Offline_Pass", password);
                    Preferences.Set("Offline_Id", loginResult.UserId);

                    _ = _apiService.TrackActionAsync("Đăng nhập hệ thống");

                    await DisplayAlert("Thành công", $"Chào mừng {loginResult.Username} trở lại!", "OK");
                    Application.Current.MainPage = new AppShell();
                    return;
                }
                else if (loginResult.UserId == -999 || loginResult.UserId == -401)
                {
                    // 🌟 NẾU BỊ KHÓA HOẶC SAI PASS -> Báo lỗi và DỪNG LẠI NGAY LẬP TỨC
                    await DisplayAlert("Thất bại", loginResult.Message, "OK");
                    return;
                }
            }
            else
            {
                // Lúc này loginResult == null (Rớt mạng hoặc Web sập thật sự)
                serverIsDown = true;
            }
        }
        catch (Exception)
        {
            serverIsDown = true;
        }

        // ==========================================================
        // 🌟 LUỒNG CỨU HỘ: CHỈ KÍCH HOẠT KHI MẤT MẠNG THẬT SỰ
        // ==========================================================
        if (serverIsDown)
        {
            string savedUser = Preferences.Get("Offline_User", "");
            string savedPass = Preferences.Get("Offline_Pass", "");
            int savedId = Preferences.Get("Offline_Id", 0);

            if (username == savedUser && password == savedPass && savedId > 0)
            {
                Preferences.Set("UserId", savedId);
                Preferences.Set("UserName", savedUser);

                await DisplayAlert("Chế độ Offline", "Máy chủ hiện không liên lạc được. Đã đăng nhập bằng dữ liệu lưu trên máy!", "Vào App");
                Application.Current.MainPage = new AppShell();
            }
            else
            {
                await DisplayAlert("Thất bại", "Tài khoản/Mật khẩu không chính xác hoặc máy chủ không hoạt động!", "Thử lại");
            }
        }
    }

    // ==========================================
    // 2. LUỒNG SỬ DỤNG ẨN DANH (KHÁCH DU LỊCH)
    // ==========================================
    private void OnAnonymousClicked(object sender, EventArgs e)
    {
        string anonymousId = Preferences.Get("AnonymousDeviceId", "");
        if (string.IsNullOrEmpty(anonymousId))
        {
            anonymousId = "Guest_" + Guid.NewGuid().ToString().Substring(0, 8);
            Preferences.Set("AnonymousDeviceId", anonymousId);
        }

        Preferences.Set("UserId", 0);
        Preferences.Set("UserName", "Khách Vãng Lai");
        Preferences.Set("Role", 0);

        _ = _apiService.TrackActionAsync("Truy cập ẩn danh");

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Application.Current.MainPage = new AppShell();
        });
    }

    // ==========================================
    // 3. LUỒNG CHUYỂN SANG TRANG ĐĂNG KÝ
    // ==========================================
    private void OnRegisterTapped(object sender, TappedEventArgs e)
    {
        Application.Current.MainPage = new RegisterPage();
    }
}