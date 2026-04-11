using SQLite;
using System.Text.Json.Serialization; // 🌟 BẮT BUỘC THÊM CÁI NÀY VÀO TRÊN CÙNG

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
        public int TriggerRadius { get; set; } = 50; // Bán kính kích hoạt (mét)
        public int Priority { get; set; }=1; // Độ ưu tiên (số nhỏ hơn = ưu tiên hơn)
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

        // 🌟 ĐÃ CẬP NHẬT IP MỚI VÀ THÊM JSON IGNORE
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
                return $"http://192.168.1.151:5136/images/pois/{ImageUrl}";
            }
        }
        
        // 🌟 BẢO BỐI TẢI ẢNH OFFLINE CHO POI
        [Ignore]
        [JsonIgnore]
        public string LocalImageSource
        {
            get
            {
                if (string.IsNullOrEmpty(ImageUrl)) return "img_default_poi.png";

                // Móc vào bộ nhớ trong của điện thoại xem có tải về chưa
                string localPath = Path.Combine(FileSystem.AppDataDirectory, ImageUrl);

                // Nếu có ảnh trong máy -> Lấy ra xài. Nếu chưa có -> Lấy link Web của sếp
                if (File.Exists(localPath))
                    return localPath;
                else
                    return FullImageUrl; // Gọi lại cái biến sếp viết ở trên
            }
        }
        // 👇 THÊM 2 BIẾN NÀY ĐỂ TÍNH KHOẢNG CÁCH KM (KHÔNG LƯU VÀO DB) 👇
        [Ignore]
        public double DistanceFromUser { get; set; }

        [Ignore]
        public string DistanceDisplay => DistanceFromUser > 0 ? $"{DistanceFromUser:F1} km" : "";
    }
}