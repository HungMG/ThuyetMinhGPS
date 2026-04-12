namespace TourGuideApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Role { get; set; }
    }

    // Class này dùng để nhận dữ liệu trả về từ API khi đăng nhập thành công
    public class LoginResponse
    {
        public string Message { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public int Role { get; set; }
    }
}