namespace TourGuideApp; // Nhớ sửa lại tên này nếu project bạn tên khác nhé

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Đã xóa dòng MainPage = ... ở đây để không bị xung đột nữa
    }

    // Sử dụng duy nhất hàm này để gọi AppShell (Thanh Tab Bar)
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}