using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models;
using System.Linq;
using System.Threading.Tasks;

namespace TourGuideWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context; // Đổi tên DataContext thành tên sếp đang xài (VD: AppDbContext)

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // =======================================================
        // 1. NÚT XEM DANH SÁCH CHỜ DUYỆT (Lấy những POI có Status = 0)
        // =======================================================
        [HttpGet("pending-pois")]
        public async Task<ActionResult<IEnumerable<POI>>> GetPendingPOIs()
        {
            var pendingList = await _context.POIs
                                            .Where(p => p.ApprovalStatus == 0)
                                            .ToListAsync();
            return Ok(pendingList);
        }

        // =======================================================
        // 2. NÚT "DUYỆT BÀI" (Đổi Status thành 1 -> Xuất hiện trên App)
        // =======================================================
        [HttpPut("approve-poi/{id}")]
        public async Task<IActionResult> ApprovePOI(int id)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound(new { message = "Không tìm thấy địa điểm này!" });

            poi.ApprovalStatus = 1; // 🌟 1 = Đã duyệt
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã duyệt thành công: {poi.Name_VI}" });
        }

        // =======================================================
        // 3. NÚT "TỪ CHỐI" (Đổi Status thành 2 -> Cho vào sọt rác)
        // =======================================================
        [HttpPut("reject-poi/{id}")]
        public async Task<IActionResult> RejectPOI(int id)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound(new { message = "Không tìm thấy địa điểm này!" });

            poi.ApprovalStatus = 2; // 🌟 2 = Bị từ chối
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã từ chối: {poi.Name_VI}" });
        }
    }
}