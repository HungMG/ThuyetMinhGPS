using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models; // Hoặc tên namespace chứa Models của sếp
using TourGuideAdmin;   // Hoặc tên namespace chứa AppDbContext của sếp
using System.Linq;
using System.Threading.Tasks;

namespace TourGuideAdmin.Controllers
{
    // 🌟 1. TẠO CÁI "GIỎ" CHỨA USER VÀ SỐ LIỆU ĐẾM KÈM THEO
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
        // 🌟 1. HIỂN THỊ DANH SÁCH TÀI KHOẢN KÈM THỐNG KÊ CHI TIẾT
        // =========================================================
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            var allPois = await _context.POIs.ToListAsync();

            // 🌟 BƯỚC 1: DÙNG RA-ĐA QUÉT TÌM CÁC TÀI KHOẢN HOẠT ĐỘNG TRONG 5 PHÚT QUA
            var fiveMinsAgo = DateTime.Now.AddMinutes(-5);
            var onlineUsernames = await _context.UserActivities
                .Where(a => a.Timestamp >= fiveMinsAgo)
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
                    RejectedPoi = userPois.Count(p => p.ApprovalStatus == 2), // Số 2 là đúng chuẩn bài sếp rồi nhé
                    PublicPoi = userPois.Count(p => p.PoiType == 0),
                    BusinessPoi = userPois.Count(p => p.PoiType == 1),

                    // 🌟 BƯỚC 2: KIỂM TRA TÊN TÀI KHOẢN NÀY CÓ NẰM TRONG DANH SÁCH ONLINE KHÔNG
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

                // Đảo ngược trạng thái: Đang khóa thì thành Mở, đang Mở thì thành Khóa
                user.IsLocked = !user.IsLocked;
                await _context.SaveChangesAsync();

                string actionName = user.IsLocked ? "Khóa" : "Mở khóa";
                TempData["SuccessMessage"] = $"Đã {actionName} tài khoản '{user.Username}' thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // 🌟 3. HÀM XÓA TÀI KHOẢN RÁC (Giữ nguyên của sếp)
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
    }
}