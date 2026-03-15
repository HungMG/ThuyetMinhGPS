using SQLite;

namespace TourGuideApp.Models
{
    [Table("POI")]
    public class POI
    {
        [PrimaryKey] // Nếu bạn muốn DB tự tạo ID thì thêm [AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int TriggerRadius { get; set; }

        public string Description { get; set; }

        public string AudioSource { get; set; }

        public string ImagePath { get; set; }

        public bool IsVisited { get; set; }
    }
}