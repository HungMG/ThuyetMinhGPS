using System;
using System.ComponentModel.DataAnnotations;

namespace TourGuideAdmin.Models
{
    public class UserActivity
    {
        [Key]
        public int Id { get; set; }
        public string DeviceOrUserName { get; set; } // Lưu mã Guest_xxx hoặc tên User
        public string Action { get; set; } // Hành động: "Quét QR", "Đăng nhập"...
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}