using System;
using Microsoft.Maui.Controls;
using TourGuideApp.Services;
using Microsoft.Maui.Networking;
using System.Text.RegularExpressions; // 🌟 BẮT BUỘC THÊM THƯ VIỆN NÀY ĐỂ CHECK KÝ TỰ

namespace TourGuideApp.Views;

public partial class RegisterPage : ContentPage
{
    // Lưu ý: Tùy code của sếp mà chỗ này dùng AuthService hoặc ApiService nhé
    ApiService _apiService = new ApiService();

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        // 🌟 TRẠM GÁC 0: KIỂM TRA MẠNG
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            await DisplayAlert("Mất kết nối", "Sếp ơi, phải có Internet thì mới kiểm tra và tạo tài khoản mới được nhé!", "Đã hiểu");
            return;
        }

        string username = txtUsername.Text?.Trim();
        string password = txtPassword.Text?.Trim();
        string confirmPassword = txtConfirmPassword.Text?.Trim();

        // 🌟 TRẠM GÁC 1: KHÔNG ĐƯỢC ĐỂ TRỐNG
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Lỗi", "Vui lòng điền đủ thông tin!", "OK");
            return;
        }

        // 🌟 TRẠM GÁC 2: LUẬT CHO TÊN ĐĂNG NHẬP
        if (username.Length < 5)
        {
            await DisplayAlert("Lỗi", "Tên đăng nhập quá ngắn! Phải có ít nhất 5 ký tự.", "Sửa lại");
            return;
        }

        // Dùng Regex cấm khoảng trắng và ký tự đặc biệt (Chỉ cho phép chữ và số)
        if (!Regex.IsMatch(username, @"^[a-zA-Z0-9]+$"))
        {
            await DisplayAlert("Lỗi", "Tên đăng nhập không được chứa khoảng trắng, dấu tiếng Việt hoặc ký tự đặc biệt (như @, #, $...)!", "Sửa lại");
            return;
        }

        // 🌟 TRẠM GÁC 3: LUẬT CHO MẬT KHẨU
        if (password.Length < 6)
        {
            await DisplayAlert("Lỗi", "Mật khẩu quá yếu! Vui lòng nhập ít nhất 6 ký tự.", "Sửa lại");
            return;
        }

        if (password != confirmPassword)
        {
            await DisplayAlert("Lỗi", "Mật khẩu nhập lại không khớp!", "OK");
            return;
        }

        // ==========================================
        // VƯỢT QUA HẾT CÁC TRẠM GÁC THÌ MỚI GỌI API
        // ==========================================
        bool isSuccess = await _apiService.RegisterAsync(username, password);

        if (isSuccess)
        {
            await DisplayAlert("Thành công", "Tạo tài khoản xong rồi sếp ơi! Giờ đăng nhập thôi.", "Tuyệt");
            Application.Current.MainPage = new LoginPage(); // Quay về trang Đăng nhập
        }
        else
        {
            // Tách biệt thông báo rõ ràng để vỗ mặt mấy đứa xài trùng tên
            await DisplayAlert("Thất bại", "Tên đăng nhập này đã có người xài rồi, sếp chọn tên khác cho đỡ đụng hàng nhé!", "Đổi tên");
        }
    }

    private void OnBackToLoginClicked(object sender, EventArgs e)
    {
        Application.Current.MainPage = new LoginPage();
    }
}