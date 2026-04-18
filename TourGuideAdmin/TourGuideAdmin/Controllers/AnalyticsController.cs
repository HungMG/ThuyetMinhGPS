using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models; // Chỉnh lại theo đúng namespace của sếp
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TourGuideAdmin.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly AppDbContext _context;

        // Tiêm Database vào Controller
        public AnalyticsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. TRANG DASHBOARD (Dành cho Admin xem)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var oneMinAgo = DateTime.Now.AddMinutes(-1);

            // 1. Đếm TẤT CẢ thiết bị từ trước tới nay
            var totalUsers = await _context.UserActivities
                .Select(a => a.DeviceOrUserName).Distinct().CountAsync();

            // 2. Đếm khách vãng lai (Guest)
            var activeGuests = await _context.UserActivities
                .Where(a => a.DeviceOrUserName.StartsWith("Guest_"))
                .Select(a => a.DeviceOrUserName).Distinct().CountAsync();

            // 3. Đếm lượt Quét QR hôm nay
            var qrToday = await _context.UserActivities
                .Where(a => a.Action.Contains("Quét QR") && a.Timestamp >= today)
                .CountAsync();

            // 4. ĐẾM SỐ NGƯỜI ĐANG ONLINE 
            var onlineNow = await _context.UserActivities
                .Where(a => a.Timestamp >= oneMinAgo)
                .Select(a => a.DeviceOrUserName)
                .Distinct()
                .CountAsync();

            // =========================================================
            // 🌟 5. MÁY LỌC RÁC: GOM NHÓM CÁC HÀNH ĐỘNG BỊ TRÙNG LẶP
            // =========================================================
            // Bước A: Kéo 200 hành động mới nhất từ Database lên
            var rawActivities = await _context.UserActivities
                .OrderByDescending(a => a.Timestamp)
                .Take(200)
                .ToListAsync();

            // Bước B: Dùng LINQ để gom nhóm và lọc
            var recentActivities = rawActivities
                .GroupBy(a => new { a.DeviceOrUserName, a.Action }) // Gom những đứa trùng Tên + Trùng Hành động vào 1 cục
                .Select(g => g.First()) // Ở mỗi cục, chỉ bốc đúng 1 thằng đầu tiên (mới nhất) ra xem
                .Take(15) // Lấy đúng 15 dòng gọn gàng lên bảng
                .ToList();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveGuests = activeGuests;
            ViewBag.QRToday = qrToday;
            ViewBag.OnlineNow = onlineNow;

            return View(recentActivities);
        }

        // ==========================================
        // 2. API: HỨNG DỮ LIỆU TỪ MOBILE APP (CÓ CHỐT CHẶN KHÓA ACC)
        // ==========================================
        [HttpPost("api/Analytics/track")]
        [IgnoreAntiforgeryToken] // Bắt buộc để App Mobile gọi được API mà không bị chặn
        public async Task<IActionResult> TrackActivity([FromBody] ActivityDto data)
        {
            if (data == null || string.IsNullOrEmpty(data.Identifier))
                return BadRequest("Dữ liệu không hợp lệ");

            // 🌟 BƯỚC 1: KIỂM TRA TÀI KHOẢN CÓ BỊ KHÓA KHÔNG
            // Tìm xem cái Identifier (Tên đăng nhập) này có tồn tại trong bảng Users không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == data.Identifier);

            // Nếu tìm thấy tài khoản VÀ tài khoản đó đang bị khóa (IsLocked = true)
            if (user != null && user.IsLocked)
            {
                // Lập tức ném ra mã 403 (Forbidden). 
                // Bên Mobile App (ApiService) hứng được mã này sẽ gọi hàm ForceLogout() để đá văng App!
                return StatusCode(403, new { message = "Tài khoản của bạn đã bị khóa bởi Admin!" });
            }

            // 🌟 BƯỚC 2: NẾU AN TOÀN THÌ TIẾP TỤC GHI LOG
            var activity = new UserActivity
            {
                DeviceOrUserName = data.Identifier,
                Action = data.ActionName,
                Timestamp = DateTime.Now
            };

            _context.UserActivities.Add(activity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã lưu lịch sử thành công!" });
        }
    }

        // Lớp trung gian để hứng JSON từ điện thoại gửi lên
        public class ActivityDto
    {
        public string Identifier { get; set; }
        public string ActionName { get; set; }
    }
}