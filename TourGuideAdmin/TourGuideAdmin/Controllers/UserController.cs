using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin; // Sửa đúng tên namespace nhà sếp
using TourGuideAdmin.Models;

namespace   TourGuideAdmin.Controllers
{
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // 🌟 Trang danh sách tài khoản
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // 🌟 Hàm phụ: Đổi quyền Admin/Khách (Dành cho sếp trảm bớt hoặc thăng chức)
        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, int newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Role = newRole;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}