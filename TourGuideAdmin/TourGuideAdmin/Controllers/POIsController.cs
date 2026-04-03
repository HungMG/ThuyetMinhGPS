using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly TranslationService _translationService; // 1. KHAI BÁO BỘ DỊCH

        // 2. NHÚNG BỘ DỊCH VÀO CONSTRUCTOR
        public POIsController(AppDbContext context, TranslationService translationService)
        {
            _context = context;
            _translationService = translationService;
        }

        // GET: POIs
        public async Task<IActionResult> Index()
        {
            return View(await _context.POIs.ToListAsync());
        }

        // GET: POIs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pOI = await _context.POIs.FirstOrDefaultAsync(m => m.Id == id);
            if (pOI == null) return NotFound();

            return View(pOI);
        }

        // GET: POIs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: POIs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,TourId,Name_VI,Name_EN,Name_ZH,Name_KO,Name_JA,Latitude,Longitude,TriggerRadius,Priority,Description_VI,Description_EN,Description_ZH,Description_KO,Description_JA,IsFavorite,ImageUrl,LastPlayedTime")] POI pOI)
        {
            if (ModelState.IsValid)
            {
                // 🌟 TỰ ĐỘNG DỊCH TÊN 🌟
                if (!string.IsNullOrEmpty(pOI.Name_VI))
                {
                    pOI.Name_EN = await _translationService.TranslateAsync(pOI.Name_VI, "en");
                    pOI.Name_ZH = await _translationService.TranslateAsync(pOI.Name_VI, "zh-CN");
                    pOI.Name_KO = await _translationService.TranslateAsync(pOI.Name_VI, "ko");
                    pOI.Name_JA = await _translationService.TranslateAsync(pOI.Name_VI, "ja");
                }

                // 🌟 TỰ ĐỘNG DỊCH THUYẾT MINH 🌟
                if (!string.IsNullOrEmpty(pOI.Description_VI))
                {
                    pOI.Description_EN = await _translationService.TranslateAsync(pOI.Description_VI, "en");
                    pOI.Description_ZH = await _translationService.TranslateAsync(pOI.Description_VI, "zh-CN");
                    pOI.Description_KO = await _translationService.TranslateAsync(pOI.Description_VI, "ko");
                    pOI.Description_JA = await _translationService.TranslateAsync(pOI.Description_VI, "ja");
                }

                _context.Add(pOI);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(pOI);
        }

        // GET: POIs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pOI = await _context.POIs.FindAsync(id);
            if (pOI == null) return NotFound();

            return View(pOI);
        }

        // POST: POIs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,TourId,Name_VI,Name_EN,Name_ZH,Name_KO,Name_JA,Latitude,Longitude,TriggerRadius,Priority,Description_VI,Description_EN,Description_ZH,Description_KO,Description_JA,IsFavorite,ImageUrl,LastPlayedTime")] POI pOI)
        {
            if (id != pOI.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // 🌟 TỰ ĐỘNG DỊCH KHI CHỈNH SỬA TÊN 🌟
                    if (!string.IsNullOrEmpty(pOI.Name_VI))
                    {
                        pOI.Name_EN = await _translationService.TranslateAsync(pOI.Name_VI, "en");
                        pOI.Name_ZH = await _translationService.TranslateAsync(pOI.Name_VI, "zh-CN");
                        pOI.Name_KO = await _translationService.TranslateAsync(pOI.Name_VI, "ko");
                        pOI.Name_JA = await _translationService.TranslateAsync(pOI.Name_VI, "ja");
                    }

                    // 🌟 TỰ ĐỘNG DỊCH KHI CHỈNH SỬA THUYẾT MINH 🌟
                    if (!string.IsNullOrEmpty(pOI.Description_VI))
                    {
                        pOI.Description_EN = await _translationService.TranslateAsync(pOI.Description_VI, "en");
                        pOI.Description_ZH = await _translationService.TranslateAsync(pOI.Description_VI, "zh-CN");
                        pOI.Description_KO = await _translationService.TranslateAsync(pOI.Description_VI, "ko");
                        pOI.Description_JA = await _translationService.TranslateAsync(pOI.Description_VI, "ja");
                    }

                    _context.Update(pOI);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!POIExists(pOI.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(pOI);
        }

        // GET: POIs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pOI = await _context.POIs.FirstOrDefaultAsync(m => m.Id == id);
            if (pOI == null) return NotFound();

            return View(pOI);
        }

        // POST: POIs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pOI = await _context.POIs.FindAsync(id);
            if (pOI != null)
            {
                _context.POIs.Remove(pOI);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool POIExists(int id)
        {
            return _context.POIs.Any(e => e.Id == id);
        }
    }
}