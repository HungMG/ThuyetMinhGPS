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
    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = txtUsername.Text?.Trim();
        string password = txtPassword.Text?.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Thông báo", "Vui lòng nhập tài khoản và mật khẩu!", "OK");
            return;
        }

        // 🌟 BIẾN KIỂM TRA: CÓ GỌI ĐƯỢC SERVER KHÔNG?
        bool serverIsDown = false;

        try
        {
            // 1. THỬ ĐĂNG NHẬP ONLINE TRƯỚC
            ApiService apiService = new ApiService();
            var loginResult = await apiService.LoginAsync(username, password);

            if (loginResult != null && loginResult.UserId > 0)
            {
                // Đăng nhập Online thành công -> Cập nhật thẻ căn cước Offline mới nhất
                Preferences.Set("UserId", loginResult.UserId);
                Preferences.Set("UserName", loginResult.Username);
                Preferences.Set("Offline_User", username);
                Preferences.Set("Offline_Pass", password);
                Preferences.Set("Offline_Id", loginResult.UserId);

                Application.Current.MainPage = new AppShell();
                return; // Xong việc, thoát hàm
            }
            else if (loginResult == null)
            {
                // Nếu Server trả về null, có thể là sai Pass HOẶC Server đang chết
                // Anh em mình gắn cờ để tí kiểm tra Offline
                serverIsDown = true;
            }
        }
        catch (Exception)
        {
            // Nếu bị văng lỗi kết nối (Server sập)
            serverIsDown = true;
        }

        // ==========================================================
        // 🌟 2. LUỒNG CỨU HỘ: NẾU SAI PASS HOẶC SERVER SẬP -> CHECK OFFLINE
        // ==========================================================
        if (serverIsDown)
        {
            string savedUser = Preferences.Get("Offline_User", "");
            string savedPass = Preferences.Get("Offline_Pass", "");
            int savedId = Preferences.Get("Offline_Id", 0);

            // Kiểm tra xem thông tin nhập vào có khớp với "kỷ niệm" trong máy không
            if (username == savedUser && password == savedPass && savedId > 0)
            {
                Preferences.Set("UserId", savedId);
                Preferences.Set("UserName", savedUser);

                await DisplayAlert("Chế độ Offline", "Máy chủ hiện không liên lạc được. Đã đăng nhập bằng dữ liệu lưu trên máy!", "Vào App");
                Application.Current.MainPage = new AppShell();
            }
            else
            {
                // Nếu cả Online lẫn Offline đều không xong thì mới báo lỗi thật
                await DisplayAlert("Thất bại", "Tài khoản không chính xác hoặc máy chủ không hoạt động!", "Thử lại");
            }
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