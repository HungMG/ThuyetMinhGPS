using Microsoft.Maui.Controls;
using System;
using TourGuideApp.Services;

namespace TourGuideApp.Views;

public partial class LoginPage : ContentPage
{
    AuthService _authService = new AuthService();

    public LoginPage()
    {
        InitializeComponent();
    }

    // ==========================================
    // 1. LUỒNG ĐĂNG NHẬP CHÍNH THỨC
    // ==========================================
    private async void OnLoginClicked(object sender, EventArgs e) // 🌟 Đã sửa lỗi thiếu chữ 'p' ở đây
    {
        string username = txtUsername.Text?.Trim();
        string password = txtPassword.Text?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập tài khoản và mật khẩu!", "OK");
            return;
        }

        // 🌟 GỌI API THẬT
        var result = await _authService.LoginAsync(username, password);

        if (result != null)
        {
            // Lưu thông tin vào thẻ căn cước
            Preferences.Set("UserId", result.UserId);
            Preferences.Set("UserName", result.Username);
            Preferences.Set("UserRole", result.Role); // Lưu thêm Role để biết có phải Admin không

            await DisplayAlert("Thành công", $"Chào mừng {result.Username}!", "OK");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current.MainPage = new AppShell();
            });
        }
        else
        {
            await DisplayAlert("Thất bại", "Sai tài khoản hoặc mật khẩu, hoặc Server chưa bật!", "Đóng");
        }
    }

    // ==========================================
    // 2. LUỒNG SỬ DỤNG ẨN DANH (KHÁCH DU LỊCH)
    // ==========================================
    private void OnAnonymousClicked(object sender, EventArgs e)
    {
        Preferences.Set("UserId", 0);
        Preferences.Set("UserName", "Khách Vãng Lai");

        // 🌟 BỌC LƯỚI AN TOÀN VÀO ĐÂY: Nhờ luồng chính chuyển trang
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
        // 🌟 Chuyển sang trang Đăng ký
        Application.Current.MainPage = new RegisterPage();
    }
}