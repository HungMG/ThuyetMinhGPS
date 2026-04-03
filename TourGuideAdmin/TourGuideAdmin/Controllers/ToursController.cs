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
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tours.ToListAsync());
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

        // POST: Tours/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🌟 NHỚ THÊM EstimatedTime VÀO ĐÂY CHO HÀM EDIT NỮA NHA
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name_VI,Name_EN,Name_ZH,Name_KO,Name_JA,Description_VI,Description_EN,Description_ZH,Description_KO,Description_JA,EstimatedTime,ImageUrl")] Tour tour)
        {
            if (id != tour.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
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

                    _context.Update(tour);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TourExists(tour.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tour);
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
            if (tour != null)
            {
                // Code nâng cao (tùy chọn): Bạn có thể thêm code xóa file hình trong wwwroot ở đây để đỡ nặng server
                _context.Tours.Remove(tour);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TourExists(int id)
        {
            return _context.Tours.Any(e => e.Id == id);
        }
    }
}