using System;
using System.IO; // Thêm thư viện File
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // Thêm thư viện môi trường
using Microsoft.AspNetCore.Http; // Thêm thư viện Upload File
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models;
using TourGuideAdmin.Services;

namespace TourGuideAdmin.Controllers
{
    public class POIsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TranslationService _translationService;
        private readonly IWebHostEnvironment _env; // 🌟 Thêm ông thủ kho

        public POIsController(AppDbContext context, TranslationService translationService, IWebHostEnvironment env)
        {
            _context = context;
            _translationService = translationService;
            _env = env;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var pois = _context.POIs.AsEnumerable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var keyword = RemoveDiacritics(searchString.ToLower());

                pois = pois.Where(p =>
                    !string.IsNullOrEmpty(p.Name_VI) &&
                    RemoveDiacritics(p.Name_VI.ToLower()).Contains(keyword)
                );
            }

            return View(pois.ToList());
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

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var pOI = await _context.POIs.FirstOrDefaultAsync(m => m.Id == id);
            if (pOI == null) return NotFound();
            return View(pOI);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🌟 THÊM IFormFile ĐỂ UPLOAD ẢNH POI
        public async Task<IActionResult> Create([Bind("Id,TourId,Name_VI,Latitude,Longitude,TriggerRadius,Priority,Description_VI,IsFavorite")] POI pOI, IFormFile? fileHinhAnh)
        {
            // Bịt miệng ModelState cho các cột tự dịch
            ModelState.Remove("Name_EN"); ModelState.Remove("Name_ZH"); ModelState.Remove("Name_KO"); ModelState.Remove("Name_JA");
            ModelState.Remove("Description_EN"); ModelState.Remove("Description_ZH"); ModelState.Remove("Description_KO"); ModelState.Remove("Description_JA");

            if (ModelState.IsValid)
            {
                // 1. TỰ ĐỘNG DỊCH
                if (!string.IsNullOrEmpty(pOI.Name_VI))
                {
                    pOI.Name_EN = await _translationService.TranslateAsync(pOI.Name_VI, "en");
                    pOI.Name_ZH = await _translationService.TranslateAsync(pOI.Name_VI, "zh-CN");
                    pOI.Name_KO = await _translationService.TranslateAsync(pOI.Name_VI, "ko");
                    pOI.Name_JA = await _translationService.TranslateAsync(pOI.Name_VI, "ja");
                }
                if (!string.IsNullOrEmpty(pOI.Description_VI))
                {
                    pOI.Description_EN = await _translationService.TranslateAsync(pOI.Description_VI, "en");
                    pOI.Description_ZH = await _translationService.TranslateAsync(pOI.Description_VI, "zh-CN");
                    pOI.Description_KO = await _translationService.TranslateAsync(pOI.Description_VI, "ko");
                    pOI.Description_JA = await _translationService.TranslateAsync(pOI.Description_VI, "ja");
                }

                // 2. LƯU ẢNH VÀO WWWROOT/IMAGES/POIS
                if (fileHinhAnh != null && fileHinhAnh.Length > 0)
                {
                    string uploadFolder = Path.Combine(_env.WebRootPath, "images", "pois");
                    Directory.CreateDirectory(uploadFolder);
                    string fileName = Guid.NewGuid().ToString() + "_" + fileHinhAnh.FileName;
                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await fileHinhAnh.CopyToAsync(fileStream);
                    }
                    pOI.ImageUrl = fileName;
                }

                _context.Add(pOI);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pOI);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var pOI = await _context.POIs.FindAsync(id);
            if (pOI == null) return NotFound();
            return View(pOI);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // 🌟 SỬ DỤNG BÚA TẠ ĐỂ EDIT POI CHO CHẮC CÚ
        public async Task<IActionResult> Edit(int id, POI poiInput, IFormFile? fileHinhAnh)
        {
            if (id != poiInput.Id) return NotFound();

            var poiInDb = await _context.POIs.FindAsync(id);
            if (poiInDb == null) return NotFound();

            // Chép đè dữ liệu cơ bản
            poiInDb.Name_VI = poiInput.Name_VI;
            poiInDb.Description_VI = poiInput.Description_VI;
            poiInDb.Latitude = poiInput.Latitude;
            poiInDb.Longitude = poiInput.Longitude;
            poiInDb.TriggerRadius = poiInput.TriggerRadius;
            poiInDb.Priority = poiInput.Priority;
            poiInDb.TourId = poiInput.TourId;
            poiInDb.IsFavorite = poiInput.IsFavorite;

            // Lưu ảnh mới (nếu có up)
            if (fileHinhAnh != null && fileHinhAnh.Length > 0)
            {
                string uploadFolder = Path.Combine(_env.WebRootPath, "images", "pois");
                Directory.CreateDirectory(uploadFolder);
                string fileName = Guid.NewGuid().ToString() + "_" + fileHinhAnh.FileName;
                string filePath = Path.Combine(uploadFolder, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await fileHinhAnh.CopyToAsync(fileStream);
                }
                poiInDb.ImageUrl = fileName;
            }

            // Dịch lại
            if (!string.IsNullOrEmpty(poiInput.Name_VI))
            {
                poiInDb.Name_EN = await _translationService.TranslateAsync(poiInput.Name_VI, "en");
                poiInDb.Name_ZH = await _translationService.TranslateAsync(poiInput.Name_VI, "zh-CN");
                poiInDb.Name_KO = await _translationService.TranslateAsync(poiInput.Name_VI, "ko");
                poiInDb.Name_JA = await _translationService.TranslateAsync(poiInput.Name_VI, "ja");
            }
            if (!string.IsNullOrEmpty(poiInput.Description_VI))
            {
                poiInDb.Description_EN = await _translationService.TranslateAsync(poiInput.Description_VI, "en");
                poiInDb.Description_ZH = await _translationService.TranslateAsync(poiInput.Description_VI, "zh-CN");
                poiInDb.Description_KO = await _translationService.TranslateAsync(poiInput.Description_VI, "ko");
                poiInDb.Description_JA = await _translationService.TranslateAsync(poiInput.Description_VI, "ja");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

         public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pois = await _context.POIs.FirstOrDefaultAsync(m => m.Id == id);
            if (pois == null) return NotFound();

            return View(pois);
        }

        // POST: Tours/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var poi = await _context.POIs.FindAsync(id);

            if (poi != null)
            {
                // 🔥 XÓA FILE ẢNH 
                if (!string.IsNullOrEmpty(poi.ImageUrl))
                {
                    string filePath = Path.Combine(_env.WebRootPath, "images", "pois", poi.ImageUrl);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // 🔥 XÓA DB
                _context.POIs.Remove(poi);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool POIExists(int id)
        {
            return _context.POIs.Any(e => e.Id == id);
        }
    }
}