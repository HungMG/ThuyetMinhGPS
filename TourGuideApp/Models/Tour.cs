using SQLite;
using System.Text.Json.Serialization;

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

        public string? Description_VI { get; set; }
        public string? Description_EN { get; set; }
        public string? Description_ZH { get; set; }
        public string? Description_KO { get; set; }
        public string? Description_JA { get; set; }


        [Ignore]
        public string CurrentName
        {
            get
            {
                string lang = Preferences.Get("AppLanguage", "vi");
                return lang switch { "en" => Name_EN, "zh" => Name_ZH, "ko" => Name_KO, "ja" => Name_JA, _ => Name_VI };
            }
        }

        [JsonIgnore] // Khai báo này để lúc hút API nó không bị bối rối
        public string FullImageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(ImageUrl))
                    return "img_tour_default.jpg"; // Nếu không có hình thì hiện hình mặc định

                // THAY SỐ IP BÊN DƯỚI BẰNG ĐÚNG SỐ IP MÁY TÍNH CỦA BẠN NHÉ (Giống bên ApiService)
                return $"http://192.168.100.230:5136/images/tours/{ImageUrl}";
            }
        }
    }
}