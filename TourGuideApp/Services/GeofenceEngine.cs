using System;
using System.Collections.Generic;
using System.Linq;
using TourGuideApp.Models;

namespace TourGuideApp.Services
{
    public class GeofenceEngine
    {
        // Bán kính Trái Đất tính bằng mét
        private const double EarthRadiusInMeters = 6371000;

        // 1. HÀM TOÁN HỌC: Thuật toán Haversine
        public double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);

            lat1 = DegreesToRadians(lat1);
            lat2 = DegreesToRadians(lat2);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusInMeters * c; // Trả về con số mét chuẩn xác
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        // 2. HÀM XỬ LÝ LOGIC: Tìm điểm POI phù hợp nhất để phát Audio
        // Hàm này bọc gọn toàn bộ logic của slide: Bán kính -> Ưu tiên -> Chống lặp
        public POI GetBestPOIToTrigger(double currentLat, double currentLon, List<POI> allPOIs, int cooldownMinutes = 5)
        {
            var validPOIs = new List<POI>();

            foreach (var poi in allPOIs)
            {
                // Bước A: Đo khoảng cách bằng Haversine
                double distance = CalculateHaversineDistance(currentLat, currentLon, poi.Latitude, poi.Longitude);

                // Bước B: Kiểm tra xem có nằm trong bán kính không?
                if (distance <= poi.TriggerRadius)
                {
                    // Bước C: Kiểm tra chống lặp (Narration Engine)
                    // Nếu chưa từng phát, HOẶC thời gian phát lần cuối đã vượt qua số phút Cooldown
                    bool canPlay = !poi.LastPlayedTime.HasValue ||
                                   (DateTime.Now - poi.LastPlayedTime.Value).TotalMinutes >= cooldownMinutes;

                    if (canPlay)
                    {
                        validPOIs.Add(poi);
                    }
                }
            }

            // Bước D: Nếu có nhiều điểm cùng thỏa mãn, lọc theo Độ Ưu Tiên (Priority)
            // Lấy điểm có Priority nhỏ nhất (Ví dụ 1 ưu tiên hơn 2)
            if (validPOIs.Any())
            {
                return validPOIs.OrderBy(p => p.Priority).First();
            }

            return null; // Không có điểm nào thỏa điều kiện
        }
    }
}