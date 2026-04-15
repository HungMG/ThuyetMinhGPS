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
            // 🌟 TẠO MỐC THỜI GIAN: Lùi lại 5 phút so với hiện tại
            var fiveMinsAgo = DateTime.Now.AddMinutes(-5);

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

            // 🌟 4. ĐẾM SỐ NGƯỜI ĐANG ONLINE (Tương tác trong 5 phút qua)
            var onlineNow = await _context.UserActivities
                .Where(a => a.Timestamp >= fiveMinsAgo)
                .Select(a => a.DeviceOrUserName)
                .Distinct() // Đảm bảo 1 máy bấm 10 lần vẫn chỉ tính là 1 người online
                .CountAsync();

            var recentActivities = await _context.UserActivities
                .OrderByDescending(a => a.Timestamp).Take(15).ToListAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.ActiveGuests = activeGuests;
            ViewBag.QRToday = qrToday;

            // 🌟 GỬI SỐ ONLINE RA GIAO DIỆN
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