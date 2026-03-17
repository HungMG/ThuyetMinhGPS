using SQLite;
using System; 

namespace TourGuideApp.Models
{
    [Table("POI")]
    public class POI
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int TriggerRadius { get; set; }
        public string Description { get; set; }
        public string AudioSource { get; set; }
        public string ImagePath { get; set; }

        public int Priority { get; set; }
        public DateTime? LastPlayedTime { get; set;}
    }
}