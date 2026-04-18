using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models;
using TourGuideAdmin;
using System.Linq;
using System.Threading.Tasks;

namespace TourGuideAdmin.Controllers
{
    public class UserStatViewModel
    {
        public User User { get; set; }
        public int TotalPoi { get; set; }
        public int ApprovedPoi { get; set; }
        public int PendingPoi { get; set; }
        public int RejectedPoi { get; set; }
        public int PublicPoi { get; set; }
        public int BusinessPoi { get; set; }
        public bool IsOnline { get; set; }
    }

    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 🌟 1. HIỂN THỊ DANH SÁCH TÀI KHOẢN (REAL-TIME 1 PHÚT)
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            var allPois = await _context.POIs.ToListAsync();

            // 🌟 ĐÃ ÉP XUỐNG CÒN 1 PHÚT NHÉ SẾP
            var oneMinAgo = DateTime.Now.AddMinutes(-1);
            var onlineUsernames = await _context.UserActivities
                .Where(a => a.Timestamp >= oneMinAgo)
                .Select(a => a.DeviceOrUserName)
                .Distinct()
                .ToListAsync();

            var userStats = users.Select(u => {
                var userPois = allPois.Where(p => p.OwnerId == u.Id).ToList();
                return new UserStatViewModel
                {
                    User = u,
                    TotalPoi = userPois.Count,
                    ApprovedPoi = userPois.Count(p => p.ApprovalStatus == 1),
                    PendingPoi = userPois.Count(p => p.ApprovalStatus == 0),
                    RejectedPoi = userPois.Count(p => p.ApprovalStatus == 2),
                    PublicPoi = userPois.Count(p => p.PoiType == 0),
                    BusinessPoi = userPois.Count(p => p.PoiType == 1),
                    IsOnline = onlineUsernames.Contains(u.Username)
                };
            }).ToList();

            return View(userStats);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var userPois = await _context.POIs
                .Where(p => p.OwnerId == id)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewBag.UserName = user.Username;
            return View(userPois);
        }

        // =========================================================
        // 🌟 2. HÀM MỚI: KHÓA / MỞ KHÓA TÀI KHOẢN
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Role == 1)
                {
                    TempData["ErrorMessage"] = "Sếp không thể tự khóa mõm chính mình được!";
                    return RedirectToAction(nameof(Index));
                }

                user.IsLocked = !user.IsLocked;
                await _context.SaveChangesAsync();

                string actionName = user.IsLocked ? "Khóa" : "Mở khóa";
                TempData["SuccessMessage"] = $"Đã {actionName} tài khoản '{user.Username}' thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 🌟 3. HÀM XÓA TÀI KHOẢN RÁC 
        // =========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Role == 1)
                {
                    TempData["ErrorMessage"] = "Cảnh báo: Sếp không thể tự xóa tài khoản Admin được!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tiễn tài khoản '{user.Username}' ra đảo thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 🌟 4. NÚT BẤM DỌN RÁC HEARTBEAT (DÀNH CHO LÚC DEMO)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> ClearHeartbeatLogs()
        {
            // Lấy toàn bộ rác trong bảng UserActivities ra
            var allActivities = await _context.UserActivities.ToListAsync();

            // Xóa sạch sẽ và lưu lại
            _context.UserActivities.RemoveRange(allActivities);
            await _context.SaveChangesAsync();

            // Báo cáo thành công
            TempData["SuccessMessage"] = "Đã dọn sạch lịch sử rác Heartbeat! Mọi người đang Offline, vui lòng đợi 45 giây để nhịp tim đập lại.";

            return RedirectToAction(nameof(Index));
        }
    }
}