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
        public int Priority { get; set; } = 1; // Độ ưu tiên (số nhỏ hơn = ưu tiên hơn)
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

        // 🌟 BỘ ĐÔI KIỂM DUYỆT CỦA ADMIN 🌟

        // 1. Trỏ về ông chủ quán nào đã tạo ra điểm này (Cho phép null 'int?' vì có thể sếp là người tự tạo)
        public int? OwnerId { get; set; }

        // 2. Trạng thái kiểm duyệt
        // Quy ước: 0 = Đang chờ duyệt | 1 = Đã duyệt (Được lên App) | 2 = Bị từ chối
        public int ApprovalStatus { get; set; } = 0;

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
                return $"http://192.168.1.229/images/pois/{ImageUrl}";
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

        // 🌟 NÂNG CẤP: Tự động quy đổi Km sang Mét cho đẹp mắt
        [Ignore]
        public string DistanceDisplay
        {
            get
            {
                // Nếu chưa lấy được GPS (gán số 9999) thì hiện chữ
                if (DistanceFromUser >= 9999 || DistanceFromUser <= 0)
                    return "Đang dò GPS...";

                // Nếu dưới 1km -> Đổi ra mét (Nhân 1000, bỏ số thập phân)
                if (DistanceFromUser < 1)
                {
                    return $"{Math.Round(DistanceFromUser * 1000)} m";
                }

                // Nếu từ 1km trở lên -> Giữ nguyên km, lấy 1 chữ số thập phân
                return $"{Math.Round(DistanceFromUser, 1)} km";
            }
        }
        [Ignore]
        [JsonIgnore]
        public string ApprovalStatusText => ApprovalStatus switch
        {
            0 => "⏳ Đang chờ duyệt",
            1 => "✅ Đã lên App",
            2 => "❌ Bị từ chối",
            _ => "Không xác định"
        };

        [Ignore]
        [JsonIgnore]
        public Color ApprovalStatusColor => ApprovalStatus switch
        {
            0 => Color.FromArgb("#F39C12"), // Màu cam
            1 => Color.FromArgb("#27AE60"), // Màu xanh lá
            2 => Color.FromArgb("#E74C3C"), // Màu đỏ
            _ => Colors.Gray
        };
    }
}