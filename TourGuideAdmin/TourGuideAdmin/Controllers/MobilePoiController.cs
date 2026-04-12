using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using TourGuideAdmin; // 🌟 Sửa lại đúng tên nhà sếp
using TourGuideAdmin.Models;

namespace TourGuideAdmin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MobilePoiController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MobilePoiController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitPoi([FromForm] POI newPoi, IFormFile imageFile)
        {
            try
            {
                // 1. NẾU CÓ ẢNH: Lưu file ảnh vào thư mục wwwroot/images/pois
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "pois");
                    Directory.CreateDirectory(uploadsFolder); // Tự động tạo folder nếu chưa có
                    
                    // Tạo tên file độc nhất để không bị trùng (vd: 8a9b-1c2d_hinh_anh.jpg)
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    
                    // Chỉ lưu cái TÊN FILE vào Database thôi
                    newPoi.ImageUrl = uniqueFileName; 
                }

                // 2. LƯU THÔNG TIN VÀO DATABASE (Trạng thái chờ duyệt = 0)
                _context.POIs.Add(newPoi);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Gửi thành công, chờ duyệt!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        // 🌟 LẤY DANH SÁCH ĐỊA ĐIỂM CỦA RIÊNG MÌNH
        [HttpGet("my-pois/{userId}")]
        public async Task<IActionResult> GetMyPois(int userId)
        {
            try
            {
                // Tìm tất cả POI do user này tạo ra, sắp xếp bài mới nhất lên đầu
                var myPois = await _context.POIs
                                           .Where(p => p.OwnerId == userId)
                                           .OrderByDescending(p => p.Id)
                                           .ToListAsync();
                return Ok(myPois);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // 🌟 API CẬP NHẬT ĐỊA ĐIỂM (EDIT)
        [HttpPost("edit/{id}")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> EditPoi(int id, [FromForm] POI updatedPoi, IFormFile? imageFile)
        {
            try
            {
                var poiInDb = await _context.POIs.FindAsync(id);
                if (poiInDb == null) return NotFound(new { message = "Không tìm thấy địa điểm!" });

                // 1. Chép đè thông tin chữ
                poiInDb.Name_VI = updatedPoi.Name_VI;
                poiInDb.Description_VI = updatedPoi.Description_VI;
                poiInDb.Latitude = updatedPoi.Latitude;
                poiInDb.Longitude = updatedPoi.Longitude;

                // 2. Nếu khách có chọn ảnh mới thì lưu ảnh mới (không thì giữ nguyên ảnh cũ)
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "pois");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    poiInDb.ImageUrl = uniqueFileName;
                }

                // 🌟 3. QUAN TRỌNG: Sửa xong thì tước thẻ bài, bắt quay về trạng thái Chờ duyệt (0)
                poiInDb.ApprovalStatus = 0;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã cập nhật, chờ Admin duyệt lại!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}