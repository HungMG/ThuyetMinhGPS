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

        // 🌟 DÁN THÊM CÁI NÀY VÀO ĐỂ LỌC TOUR THEO MÔ TẢ ĐA NGÔN NGỮ
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

        [Ignore]
        [JsonIgnore]
        public string FullImageUrl
        {
            get
            {
                // 0. KHÔNG CÓ ẢNH -> Dùng ảnh mặc định
                if (string.IsNullOrEmpty(ImageUrl))
                    return "img_poi_default.jpg";

                // 1. NẾU LÀ FILE OFFLINE (Đã tải về nằm trong máy)
                // Đường dẫn trong máy Android/iOS thường bắt đầu bằng "/" hoặc "file://" hoặc "C:\" (Windows)
                if (ImageUrl.StartsWith("/") || ImageUrl.StartsWith("file://") || ImageUrl.StartsWith("C:\\") || ImageUrl.StartsWith("D:\\"))
                {
                    return ImageUrl;
                }

                // 2. NẾU LÀ LINK WEB HOÀN CHỈNH (Có sẵn chữ http)
                if (ImageUrl.StartsWith("http"))
                {
                    return ImageUrl;
                }

                // 3. NẾU MỚI CHỈ CÓ TÊN FILE VÀ ĐANG CÓ MẠNG (Lấy từ Server của sếp)
                return $"http://192.168.1.151:5136/images/tours/{ImageUrl}";
            }
        }
        // 🌟 BẢO BỐI TẢI ẢNH OFFLINE CHO TOUR
        [Ignore]
        [JsonIgnore]
        public string LocalImageSource
        {
            get
            {
                if (string.IsNullOrEmpty(ImageUrl)) return "img_default_tour.png";

                string localPath = Path.Combine(FileSystem.AppDataDirectory, ImageUrl);

                if (File.Exists(localPath))
                    return localPath;
                else
                    return FullImageUrl;
            }
        }
    }
}