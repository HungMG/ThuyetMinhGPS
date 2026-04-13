using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin;
using TourGuideAdmin.Models;
using TourGuideAdmin.Services; // 🌟 Gọi thêm thư viện Services để xài máy dịch

namespace TourGuideAdmin.Controllers
{
    public class ApprovalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly TranslationService _translationService; // 🌟 Thêm biến máy dịch

        public ApprovalController(AppDbContext context, TranslationService translationService)
        {
            _context = context;
            _translationService = translationService;
        }

        // 1. TRANG DANH SÁCH CHỜ DUYỆT
        public async Task<IActionResult> Index(string searchString)
        {
            var pois = _context.POIs.Where(p => p.ApprovalStatus == 0).AsEnumerable();

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

        // 2. TRANG XEM CHI TIẾT ĐỂ THẨM ĐỊNH
        public async Task<IActionResult> Details(int id)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            // Lấy tên ông khách ra xem
            var owner = await _context.Users.FindAsync(poi.OwnerId);
            ViewBag.OwnerName = owner != null ? owner.Username : "Khách Vãng Lai";

            return View(poi);
        }

        // 🌟 3. HÀM XỬ LÝ: VỪA DUYỆT VỪA DỊCH VÀ CHỐT CHẶN BÁN KÍNH
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(int id, int status)
        {
            try
            {
                var poi = await _context.POIs.FindAsync(id);
                if (poi != null)
                {
                    // 🌟 1. ÉP NGAY TRẠNG THÁI MỚI (1 = Duyệt, 2 = Từ chối)
                    poi.ApprovalStatus = status;

                    // 🌟 2. NẾU LÀ DUYỆT THÌ THỰC HIỆN CÁC BƯỚC CHUẨN HÓA
                    if (status == 1)
                    {
                        // 🛡️ CHỐT CHẶN BÁN KÍNH: Ép về 50m nếu đang là 0
                        if (poi.TriggerRadius <= 0)
                        {
                            poi.TriggerRadius = 50;
                        }

                        // Gọi máy dịch
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
                            Console.WriteLine($"[LỖI MÁY DỊCH] Kệ nó, vẫn cho duyệt qua! Lỗi: {transEx.Message}");
                        }
                    }

                    // 🌟 3. BƯỚC QUAN TRỌNG NHẤT: LƯU VÀO DATABASE
                    _context.Update(poi);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LỖI DATABASE] Không lưu được: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        // 🌟 HÀM HỖ TRỢ XÓA DẤU TIẾNG VIỆT
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
    }
}