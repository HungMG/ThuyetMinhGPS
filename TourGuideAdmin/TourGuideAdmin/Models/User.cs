using System.ComponentModel.DataAnnotations;

namespace TourGuideAdmin.Models
{
    public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; }

    [Required]
    [MaxLength(255)]
    public string Password { get; set; } 

    // Phân quyền: 0 = Khách du lịch (Chỉ xài App), 1 = Chủ quán/Admin (Được duyệt bài trên Web)
    public int Role { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
    }
}