using SQLite;

namespace TourGuideApp.Models
{
    public class Tour
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ImageUrl { get; set; }
        public string EstimatedTime { get; set; } // Thời gian dự kiến (VD: "2 hours")

        // Dữ liệu đa ngôn ngữ
        public string Name_VI { get; set; }
        public string Name_EN { get; set; }
        public string Name_ZH { get; set; }
        public string Name_KO { get; set; }
        public string Name_JA { get; set; }

        public string Description_VI { get; set; }
        public string Description_EN { get; set; }
        // ... (Nếu rảnh bạn có thể thêm các ngôn ngữ khác cho Description)

        [Ignore]
        public string CurrentName
        {
            get
            {
                string lang = Preferences.Get("AppLanguage", "vi");
                return lang switch { "en" => Name_EN, "zh" => Name_ZH, "ko" => Name_KO, "ja" => Name_JA, _ => Name_VI };
            }
        }
    }
}