using System.ComponentModel.DataAnnotations;

namespace TourGuideAdmin.Models
{
    public class Tour
    {
        [Key]
        public int Id { get; set; }

        public string? Name_VI { get; set; }
        public string? Name_EN { get; set; }
        public string? Name_ZH { get; set; }
        public string? Name_KO { get; set; }
        public string? Name_JA { get; set; }

        // Thêm 5 cột Thuyết minh cho Tour
        public string? Description_VI { get; set; }
        public string? Description_EN { get; set; }
        public string? Description_ZH { get; set; }
        public string? Description_KO { get; set; }
        public string? Description_JA { get; set; }

        public string? EstimatedTime { get; set; }

        public string? ImageUrl { get; set; }
    }
}