using SQLite;
using System.Text.Json.Serialization; // 🌟 BẮT BUỘC THÊM CÁI NÀY VÀO TRÊN CÙNG
using System.IO;

namespace TourGuideApp.Models
{
    public class POI
    {
        [PrimaryKey, AutoIncrement] // POI thì cứ để AutoIncrement cũng được nếu sếp có tính năng tạo offline
        [JsonPropertyName("id")]    // 🌟 THÊM DÒNG NÀY VÀO
        public int Id { get; set; }

        // Tọa độ và Hình ảnh
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; }

        // Các biến thiết lập
        public int TriggerRadius { get; set; } = 50;
        public int Priority { get; set; } = 1;
        public DateTime? LastPlayedTime { get; set; }
        public bool IsFavorite { get; set; }

        // 🌟 THÊM DÒNG NÀY VÀO ĐỂ BẮT ĐÚNG TÊN TỪ WEB GỬI VỀ
        [JsonPropertyName("tourId")]
        public int TourId { get; set; }

        // 👇 CÁC CỘT DỮ LIỆU ĐA NGÔN NGỮ 👇
        public string Name_VI { get; set; }
        public string Name_EN { get; set; }
        public string Name_ZH { get; set; }
        public string Name_KO { get; set; }
        public string Name_JA { get; set; }

        public string Description_VI { get; set; }
        public string Description_EN { get; set; }
        public string Description_ZH { get; set; }
        public string Description_KO { get; set; }
        public string Description_JA { get; set; }

        // =======================================================
        // 🎙️ QUẢN LÝ AUDIO THUYẾT MINH (5 NGÔN NGỮ) - VỪA THÊM
        // =======================================================
        public string AudioFile_VI { get; set; }
        public string AudioFile_EN { get; set; }
        public string AudioFile_ZH { get; set; }
        public string AudioFile_KO { get; set; }
        public string AudioFile_JA { get; set; }

        public int AudioType_VI { get; set; }
        public int AudioType_EN { get; set; }
        public int AudioType_ZH { get; set; }
        public int AudioType_KO { get; set; }
        public int AudioType_JA { get; set; }

        // 🌟 NÂNG CẤP V2: XÁC THỰC DOANH NGHIỆP
        public int PoiType { get; set; }
        public string ProofImageUrl { get; set; }

        // 🌟 BỘ ĐÔI KIỂM DUYỆT CỦA ADMIN 🌟
        public int OwnerId { get; set; }
        public int ApprovalStatus { get; set; }

        // =======================================================
        // 👇 TUYỆT CHIÊU TỰ ĐỘNG CHỌN NGÔN NGỮ DÙNG CHO APP 👇
        // =======================================================
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
                    _ => Name_VI
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

        // 🌟 THÊM MỚI: TỰ ĐỘNG CHỌN ĐÚNG FILE AUDIO THEO NGÔN NGỮ ĐANG MỞ
        [Ignore]
        [JsonIgnore]
        public string CurrentAudioFile
        {
            get
            {
                string lang = Preferences.Get("AppLanguage", "vi");
                return lang switch
                {
                    "en" => AudioFile_EN,
                    "zh" => AudioFile_ZH,
                    "ko" => AudioFile_KO,
                    "ja" => AudioFile_JA,
                    _ => AudioFile_VI
                };
            }
        }

        [Ignore]
        [JsonIgnore]
        public int CurrentAudioType
        {
            get
            {
                string lang = Preferences.Get("AppLanguage", "vi");
                return lang switch
                {
                    "en" => AudioType_EN,
                    "zh" => AudioType_ZH,
                    "ko" => AudioType_KO,
                    "ja" => AudioType_JA,
                    _ => AudioType_VI
                };
            }
        }

        // 🌟 THÊM MỚI: TỰ ĐỘNG TÌM ĐƯỜNG DẪN MP3 (Offline hoặc Online)
        [Ignore]
        [JsonIgnore]
        public string CurrentAudioUrl
        {
            get
            {
                var file = CurrentAudioFile;
                if (string.IsNullOrEmpty(file)) return null;

                // Check máy có tải sẵn chưa (Offline)
                string localPath = Path.Combine(FileSystem.AppDataDirectory, file);
                if (File.Exists(localPath)) return localPath;

                // Chưa có thì nghe Online từ Web Admin
                return $"https://stauroscopically-unlethargical-merideth.ngrok-free.dev/audio/pois/{file}";
            }
        }

        // =======================================================
        // 👇 XỬ LÝ HÌNH ẢNH VÀ HIỂN THỊ 👇
        // =======================================================
        [Ignore]
        [JsonIgnore]
        public string FullImageUrl
        {
            get
            {
                if (string.IsNullOrEmpty(ImageUrl)) return "img_poi_default.jpg";
                if (ImageUrl.StartsWith("/") || ImageUrl.StartsWith("file://") || ImageUrl.StartsWith("C:\\") || ImageUrl.StartsWith("D:\\")) return ImageUrl;
                if (ImageUrl.StartsWith("http")) return ImageUrl;

                return $"https://stauroscopically-unlethargical-merideth.ngrok-free.dev/images/pois/{ImageUrl}";
            }
        }

        [Ignore]
        [JsonIgnore]
        public string LocalImageSource
        {
            get
            {
                if (string.IsNullOrEmpty(ImageUrl)) return "img_default_poi.png";
                string localPath = Path.Combine(FileSystem.AppDataDirectory, ImageUrl);
                if (File.Exists(localPath)) return localPath;
                else return FullImageUrl;
            }
        }

        [Ignore]
        public double DistanceFromUser { get; set; }

        [Ignore]
        public string DistanceDisplay
        {
            get
            {
                if (DistanceFromUser >= 9999 || DistanceFromUser <= 0) return "Đang dò GPS...";
                if (DistanceFromUser < 1) return $"{Math.Round(DistanceFromUser * 1000)} m";
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
            0 => Color.FromArgb("#F39C12"),
            1 => Color.FromArgb("#27AE60"),
            2 => Color.FromArgb("#E74C3C"),
            _ => Colors.Gray
        };
    }
}