using SQLite;

namespace TourGuideApp.Models
{
    [Table("POI")]
    public class POI
    {
        [PrimaryKey]
        [AutoIncrement] // CHÍNH LÀ DÒNG NÀY ĐÂY! Giúp Id tự động nhảy 1, 2, 3...
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