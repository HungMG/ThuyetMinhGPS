using System;
using System.Collections.Generic;
using System.IO; // 🌟 BẮT BUỘC ĐỂ LƯU FILE
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // 🌟 BẮT BUỘC CHO _env
using Microsoft.AspNetCore.Http; // 🌟 BẮT BUỘC CHO IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models;
using TourGuideAdmin.Services;

namespace TourGuideAdmin.Controllers
{
    public class ToursController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TranslationService _translationService;
        private readonly IWebHostEnvironment _env; // 🌟 ÔNG THỦ KHO

        // 🌟 KHỞI TẠO 3 CÔNG CỤ
        public ToursController(AppDbContext context, TranslationService translationService, IWebHostEnvironment env)
        {
            _context = context;
            _translationService = translationService;
            _env = env;
        }

        // GET: Tours
        // tìm kiếm
        public async Task<IActionResult> Index(string searchString)
        {
            var tours = _context.Tours.AsEnumerable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var keyword = RemoveDiacritics(searchString.ToLower());

                tours = tours.Where(t =>
                    !string.IsNullOrEmpty(t.Name_VI) &&
                    RemoveDiacritics(t.Name_VI.ToLower()).Contains(keyword)
                );
            }

            return View(tours.ToList());
        }
        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();

            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        // GET: Tours/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var tour = await _context.Tours.FirstOrDefaultAsync(m => m.Id == id);
            if (tour == null) return NotFound();

            return View(tour);
        }

        // GET: Tours/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tours/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🌟 THÊM EstimatedTime VÀ IFormFile fileHinhAnh VÀO ĐÂY
        public async Task<IActionResult> Create([Bind("Id,Name_VI,Name_EN,Name_ZH,Name_KO,Name_JA,Description_VI,Description_EN,Description_ZH,Description_KO,Description_JA,EstimatedTime")] Tour tour, IFormFile? fileHinhAnh)
        {
            if (ModelState.IsValid)
            {
                // 1. TỰ ĐỘNG DỊCH TÊN
                if (!string.IsNullOrEmpty(tour.Name_VI))
                {
                    tour.Name_EN = await _translationService.TranslateAsync(tour.Name_VI, "en");
                    tour.Name_ZH = await _translationService.TranslateAsync(tour.Name_VI, "zh-CN");
                    tour.Name_KO = await _translationService.TranslateAsync(tour.Name_VI, "ko");
                    tour.Name_JA = await _translationService.TranslateAsync(tour.Name_VI, "ja");
                }

                // 2. TỰ ĐỘNG DỊCH THUYẾT MINH
                if (!string.IsNullOrEmpty(tour.Description_VI))
                {
                    tour.Description_EN = await _translationService.TranslateAsync(tour.Description_VI, "en");
                    tour.Description_ZH = await _translationService.TranslateAsync(tour.Description_VI, "zh-CN");
                    tour.Description_KO = await _translationService.TranslateAsync(tour.Description_VI, "ko");
                    tour.Description_JA = await _translationService.TranslateAsync(tour.Description_VI, "ja");
                }

                // 3. BĂNG CHUYỀN LƯU HÌNH ẢNH
                if (fileHinhAnh != null && fileHinhAnh.Length > 0)
                {
                    string uploadFolder = Path.Combine(_env.WebRootPath, "images", "tours");
                    Directory.CreateDirectory(uploadFolder);

                    string fileName = Guid.NewGuid().ToString() + "_" + fileHinhAnh.FileName;
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileHinhAnh.CopyToAsync(fileStream);
                    }
                    tour.ImageUrl = fileName;
                }

                _context.Add(tour);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tour);
        }

        // GET: Tours/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tour = await _context.Tours.FindAsync(id);
            if (tour == null) return NotFound();

            return View(tour);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🌟 THÊM IFormFile fileHinhAnh VÀO ĐÂY ĐỂ CHO PHÉP UPLOAD
        public async Task<IActionResult> Edit(int id, Tour tourInput, IFormFile? fileHinhAnh)
        {
            if (id != tourInput.Id) return NotFound();

            // 1. DÙNG BÚA TẠ: Lôi Tour cũ ra
            var tourInDb = await _context.Tours.FindAsync(id);
            if (tourInDb == null) return NotFound();

            // 2. CHÉP ĐÈ DỮ LIỆU CƠ BẢN
            tourInDb.Name_VI = tourInput.Name_VI;
            tourInDb.Description_VI = tourInput.Description_VI;
            tourInDb.EstimatedTime = tourInput.EstimatedTime;

            // 🌟 3. BĂNG CHUYỀN LƯU HÌNH ẢNH MỚI (Xịn như hàm Create)
            if (fileHinhAnh != null && fileHinhAnh.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "images", "tours");
                Directory.CreateDirectory(uploadFolder);

                string fileName = Guid.NewGuid().ToString() + "_" + fileHinhAnh.FileName;
                string filePath = Path.Combine(uploadFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileHinhAnh.CopyToAsync(fileStream);
                }
                // Có up file mới thì đè tên file mới vào DB
                tourInDb.ImageUrl = fileName;
            }
            else if (!string.IsNullOrEmpty(tourInput.ImageUrl))
            {
                // Nếu không up file, nhưng sếp có gõ tay tên ảnh thì lấy tên gõ tay
                tourInDb.ImageUrl = tourInput.ImageUrl;
            }

            // 4. TỰ ĐỘNG DỊCH (Giữ nguyên của sếp)
            if (!string.IsNullOrEmpty(tourInput.Name_VI))
            {
                tourInDb.Name_EN = await _translationService.TranslateAsync(tourInput.Name_VI, "en");
                tourInDb.Name_ZH = await _translationService.TranslateAsync(tourInput.Name_VI, "zh-CN");
                tourInDb.Name_KO = await _translationService.TranslateAsync(tourInput.Name_VI, "ko");
                tourInDb.Name_JA = await _translationService.TranslateAsync(tourInput.Name_VI, "ja");
            }

            if (!string.IsNullOrEmpty(tourInput.Description_VI))
            {
                tourInDb.Description_EN = await _translationService.TranslateAsync(tourInput.Description_VI, "en");
                tourInDb.Description_ZH = await _translationService.TranslateAsync(tourInput.Description_VI, "zh-CN");
                tourInDb.Description_KO = await _translationService.TranslateAsync(tourInput.Description_VI, "ko");
                tourInDb.Description_JA = await _translationService.TranslateAsync(tourInput.Description_VI, "ja");
            }

            // 5. LƯU VÀO DATABASE
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TourExists(tourInput.Id)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Tours/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tour = await _context.Tours.FirstOrDefaultAsync(m => m.Id == id);
            if (tour == null) return NotFound();

            return View(tour);
        }

        // POST: Tours/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tour = await _context.Tours.FindAsync(id);

            if (tour == null)
                return NotFound();

            // 🔥 CHECK: Có POI thuộc Tour không?
            bool hasPOIs = await _context.POIs.AnyAsync(p => p.TourId == id);

            if (hasPOIs)
            {
                // ❌ Không cho xóa
                TempData["Error"] = "❌ Không thể xóa tour vì vẫn còn địa điểm (POI) bên trong!";
                return RedirectToAction(nameof(Index));
            }

            // 🔥 (optional) XÓA ẢNH TOUR
            if (!string.IsNullOrEmpty(tour.ImageUrl))
            {
                string filePath = Path.Combine(_env.WebRootPath, "images", "tours", tour.ImageUrl);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Xóa tour thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool TourExists(int id)
        {
            return _context.Tours.Any(e => e.Id == id);
        }
    }
}