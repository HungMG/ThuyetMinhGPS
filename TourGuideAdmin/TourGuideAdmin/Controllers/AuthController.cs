using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin; // Nếu sếp để AppDbContext ở thư mục Data
using TourGuideAdmin.Models; // Hoặc thư mục chứa DbContext của sếp

namespace TourGuideAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================
        // 1. API ĐĂNG KÝ TÀI KHOẢN (REGISTER)
        // =====================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User request)
        {
            // Kiểm tra xem tên đăng nhập đã ai xài chưa
            var userExists = await _context.Users.AnyAsync(u => u.Username == request.Username);
            if (userExists)
            {
                return BadRequest(new { message = "Tên đăng nhập này đã có người sử dụng!" });
            }

            // Tạo tài khoản mới (Mặc định Role = 0 là Khách du lịch)
            var newUser = new User
            {
                Username = request.Username,
                Password = request.Password, // Demo thì lưu thẳng, thực tế nên Hash
                Role = 0 
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công!", userId = newUser.Id });
        }

        // =====================================
        // 2. API ĐĂNG NHẬP (LOGIN)
        // =====================================
        // =====================================
        // 2. API ĐĂNG NHẬP (LOGIN)
        // =====================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User request)
        {
            // Tìm ông nào có Username và Password trùng khớp trong Database
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });
            }

            // 🌟 CHỐT CHẶN QUAN TRỌNG: Kiểm tra xem tài khoản có bị khóa không
            if (user.IsLocked)
            {
                // Trả về mã lỗi 403 (Cấm truy cập) thay vì 200 Ok
                return StatusCode(403, new { message = "Tài khoản của bạn đã bị khóa!" });
            }

            // Nếu đúng và không bị khóa, trả về Id và Role 
            return Ok(new
            {
                message = "Đăng nhập thành công!",
                userId = user.Id,
                username = user.Username,
                role = user.Role
            });
        }
    }
}