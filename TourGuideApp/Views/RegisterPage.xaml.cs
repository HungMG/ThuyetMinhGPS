using System;
using Microsoft.Maui.Controls;
using TourGuideApp.Services;

namespace TourGuideApp.Views;

public partial class RegisterPage : ContentPage
{
    AuthService _authService = new AuthService();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // 🌟 TRẠM GÁC: KIỂM TRA MẠNG TRƯỚC KHI ĐĂNG KÝ
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await DisplayAlert("Mất kết nối", "Sếp ơi, phải có Internet thì mới kiểm tra và tạo tài khoản mới được nhé!", "Đã hiểu");
            return; // Chặn đứng tại đây, không cho chạy code gọi API ở dưới nữa
        }
        string username = txtUsername.Text?.Trim();
        string password = txtPassword.Text?.Trim();
        string confirmPassword = txtConfirmPassword.Text?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Lỗi", "Vui lòng điền đủ thông tin!", "OK");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Lỗi", "Mật khẩu nhập lại không khớp!", "OK");
            return;
        }

        // Gọi API lên Server
        bool isSuccess = await _authService.RegisterAsync(username, password);

        if (isSuccess)
        {
            await DisplayAlert("Thành công", "Tạo tài khoản xong rồi sếp ơi! Giờ đăng nhập thôi.", "Tuyệt");
            Application.Current.MainPage = new LoginPage(); // Quay về trang Đăng nhập
        }
        else
        {
            await DisplayAlert("Thất bại", "Tài khoản này có người xài rồi hoặc Server đang ngủ!", "Đóng");
        }
    }

    private void OnBackToLoginClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new LoginPage();
    }
}