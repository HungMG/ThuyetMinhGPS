using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models; // Hoặc tên namespace chứa Models của sếp
using TourGuideAdmin;   // Hoặc tên namespace chứa AppDbContext của sếp

namespace TourGuideAdmin.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // 🌟 1. HIỂN THỊ DANH SÁCH TÀI KHOẢN
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // 🌟 2. HÀM XÓA TÀI KHOẢN RÁC/LÂU KHÔNG DÙNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                // 🛡️ BẢO VỆ: Không cho phép xóa tài khoản Admin (Role = 1)
                if (user.Role == 1)
                {
                    TempData["ErrorMessage"] = "Cảnh báo: Sếp không thể tự xóa tài khoản Admin được!";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa User khỏi Database
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã tiễn tài khoản '{user.Username}' ra đảo thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}