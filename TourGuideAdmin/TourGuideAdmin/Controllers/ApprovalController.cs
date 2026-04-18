using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin;
using TourGuideAdmin.Models;
using TourGuideAdmin.Services;

namespace TourGuideAdmin.Controllers
{
    public class ApprovalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TranslationService _translationService;

        public ApprovalController(AppDbContext context, TranslationService translationService)
        {
            _context = context;
            _translationService = translationService;
        }

        // ==========================================================
        // 🌟 1. TRANG DANH SÁCH (CÓ HIỂN THỊ THÔNG TIN NGƯỜI ĐĂNG)
        // ==========================================================
        public async Task<IActionResult> Index(string searchString, int status = 0)
        {
            var poisQuery = _context.POIs.Where(p => p.ApprovalStatus == status);

            if (!string.IsNullOrEmpty(searchString))
            {
                var keyword = RemoveDiacritics(searchString.ToLower());
                poisQuery = poisQuery.Where(p =>
                    p.Name_VI != null && p.Name_VI.ToLower().Contains(keyword));
            }

            var pois = await poisQuery.OrderByDescending(p => p.Id).ToListAsync();

            // 🌟 BƯỚC MỚI: Lấy danh sách tất cả User liên quan để hiển thị tên
            var ownerIds = pois.Select(p => p.OwnerId).Distinct().ToList();
            var owners = await _context.Users
                .Where(u => ownerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u);

            ViewBag.Owners = owners; // Gửi danh sách chủ sở hữu ra View
            ViewBag.CountPending = _context.POIs.Count(p => p.ApprovalStatus == 0);
            ViewBag.CountApproved = _context.POIs.Count(p => p.ApprovalStatus == 1);
            ViewBag.CountRejected = _context.POIs.Count(p => p.ApprovalStatus == 2);
            ViewBag.CurrentStatus = status;

            return View(pois);
        }
        // ==========================================================
        // 2. TRANG XEM CHI TIẾT ĐỂ THẨM ĐỊNH
        // ==========================================================
        public async Task<IActionResult> Details(int id)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            var owner = await _context.Users.FindAsync(poi.OwnerId);
            ViewBag.OwnerName = owner != null ? owner.Username : "Khách Vãng Lai";

            return View(poi);
        }

        // ==========================================================
        // 🌟 3. HÀM XỬ LÝ: DUYỆT / TỪ CHỐI
        // ==========================================================
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, int status)
        {
            try
            {
                var poi = await _context.POIs.FindAsync(id);
                if (poi != null)
                {
                    poi.ApprovalStatus = status;

                    // Nếu Duyệt (1) thì mới chạy máy dịch
                    if (status == 1)
                    {
                        if (poi.TriggerRadius <= 0)
                        {
                            poi.TriggerRadius = 50;
                        }

                        try
                        {
                            if (!string.IsNullOrEmpty(poi.Name_VI))
                            {
                                poi.Name_EN = await _translationService.TranslateAsync(poi.Name_VI, "en");
                                poi.Name_ZH = await _translationService.TranslateAsync(poi.Name_VI, "zh-CN");
                                poi.Name_KO = await _translationService.TranslateAsync(poi.Name_VI, "ko");
                                poi.Name_JA = await _translationService.TranslateAsync(poi.Name_VI, "ja");
                            }
                            if (!string.IsNullOrEmpty(poi.Description_VI))
                            {
                                poi.Description_EN = await _translationService.TranslateAsync(poi.Description_VI, "en");
                                poi.Description_ZH = await _translationService.TranslateAsync(poi.Description_VI, "zh-CN");
                                poi.Description_KO = await _translationService.TranslateAsync(poi.Description_VI, "ko");
                                poi.Description_JA = await _translationService.TranslateAsync(poi.Description_VI, "ja");
                            }
                        }
                        catch (Exception transEx)
                        {
                            Console.WriteLine($"[LỖI MÁY DỊCH] {transEx.Message}");
                        }
                    }

                    _context.Update(poi);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = status == 1 ? "Đã duyệt địa điểm lên App!" : "Đã từ chối địa điểm này!";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI DATABASE] Không lưu được: {ex.Message}");
            }

            // Xử lý xong quay về Tab tương ứng (vd: duyệt xong thì quay về tab Đã duyệt)
            return RedirectToAction(nameof(Index), new { status = status });
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
    }
}