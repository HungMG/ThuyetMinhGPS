using SQLite;

namespace TourGuideApp.Models
{
    public class POI
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Tọa độ và Hình ảnh
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; }

        // 👇 ĐÃ TRẢ LẠI: Các biến cũ dùng để thiết lập Audio và Yêu thích 👇
        public int TriggerRadius { get; set; }
        public int Priority { get; set; }
        public DateTime? LastPlayedTime { get; set; }
        public bool IsFavorite { get; set; }

        public int TourId { get; set; }

        // 👇 CÁC CỘT DỮ LIỆU ĐA NGÔN NGỮ 👇
        public string Name_VI { get; set; }
        public string Name_EN { get; set; }
        public string Name_ZH { get; set; } // Tiếng Trung
        public string Name_KO { get; set; } // Tiếng Hàn
        public string Name_JA { get; set; } // Tiếng Nhật

        public string Description_VI { get; set; }
        public string Description_EN { get; set; }
        public string Description_ZH { get; set; }
        public string Description_KO { get; set; }
        public string Description_JA { get; set; }

        // 👇 TUYỆT CHIÊU TỰ ĐỘNG CHỌN NGÔN NGỮ (Dùng switch cho gọn) 👇
        [Ignore]
        public string CurrentName
        {
            get
            {
                string lang = Preferences.Get("AppLanguage", "vi");
                return lang switch
                {
                    "en" => Name_EN,
                    "zh" => Name_ZH,
                    "ko" => Name_KO,
                    "ja" => Name_JA,
                    _ => Name_VI // Mặc định tiếng Việt
                };
            }
        }

        [Ignore]
        public string CurrentDescription
        {
            get
            {
                string lang = Preferences.Get("AppLanguage", "vi");
                return lang switch
                {
                    "en" => Description_EN,
                    "zh" => Description_ZH,
                    "ko" => Description_KO,
                    "ja" => Description_JA,
                    _ => Description_VI
                };
            }
        }
    }
}