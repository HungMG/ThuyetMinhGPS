using System.ComponentModel.DataAnnotations;

namespace TourGuideAdmin.Models
{
    public class POI
    {
        [Key] // Đánh dấu đây là Khóa chính (giống [PrimaryKey] bên MAUI)
        public int Id { get; set; }

        public int TourId { get; set; }

        public string? Name_VI { get; set; }
        public string? Name_EN { get; set; }
        public string? Name_ZH { get; set; }
        public string? Name_KO { get; set; }
        public string? Name_JA { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int TriggerRadius { get; set; }
        public int Priority { get; set; }

        public string? Description_VI { get; set; }
        public string? Description_EN { get; set; }
        public string? Description_ZH { get; set; }
        public string? Description_KO { get; set; }
        public string? Description_JA { get; set; }

        public bool IsFavorite { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? LastPlayedTime { get; set; }
    }
}