using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAdmin.Models;
using QRCoder;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace TourGuideAdmin.Controllers
{
    public class QRController : Controller
    {
        private readonly AppDbContext _context;

        public QRController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /QR
        public async Task<IActionResult> Index(string? searchString, int? tourId)
        {
            var query = _context.POIs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var kw = searchString.ToLower();
                query = query.Where(p =>
                    (p.Name_VI != null && p.Name_VI.ToLower().Contains(kw)) ||
                    (p.Name_EN != null && p.Name_EN.ToLower().Contains(kw))
                );
            }

            if (tourId.HasValue && tourId > 0)
                query = query.Where(p => p.TourId == tourId);

            var pois = await query.OrderBy(p => p.TourId).ThenBy(p => p.Name_VI).ToListAsync();
            var tours = await _context.Tours.OrderBy(t => t.Name_VI).ToListAsync();

            ViewBag.Tours = tours;
            ViewBag.SelectedTourId = tourId;
            ViewBag.SearchString = searchString;
            ViewBag.TotalCount = await _context.POIs.CountAsync();

            return View(pois);
        }

        // GET: /QR/Image/{id}?size=5
        public async Task<IActionResult> Image(int id, int size = 6)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            // ✅ FIX: Encode chuỗi đã chuẩn hóa (poi_cho_ben_thanh), không phải tên gốc
            string qrContent = GenerateQrCode(poi.Name_VI ?? poi.Name_EN ?? $"poi_{poi.Id}");
            byte[] pngBytes = GenerateQRPng(qrContent, size);

            return File(pngBytes, "image/png");
        }

        // GET: /QR/Download/{id}
        public async Task<IActionResult> Download(int id)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound();

            string qrContent = GenerateQrCode(poi.Name_VI ?? poi.Name_EN ?? $"poi_{poi.Id}");
            byte[] pngBytes = GenerateQRPng(qrContent, 10);

            string safeName = SanitizeFileName(poi.Name_VI ?? $"POI_{poi.Id}");
            return File(pngBytes, "image/png", $"QR_{safeName}.png");
        }

        // GET: /QR/DownloadAll?tourId=1
        public async Task<IActionResult> DownloadAll(int? tourId)
        {
            var query = _context.POIs.AsQueryable();
            if (tourId.HasValue && tourId > 0)
                query = query.Where(p => p.TourId == tourId);

            var pois = await query.OrderBy(p => p.TourId).ThenBy(p => p.Name_VI).ToListAsync();

            if (!pois.Any())
                return RedirectToAction(nameof(Index));

            using var zipStream = new MemoryStream();
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var poi in pois)
                {
                    string qrContent = GenerateQrCode(poi.Name_VI ?? poi.Name_EN ?? $"poi_{poi.Id}");
                    byte[] pngBytes = GenerateQRPng(qrContent, 10);

                    string safeName = SanitizeFileName(poi.Name_VI ?? $"POI_{poi.Id}");
                    string entryName = tourId.HasValue
                        ? $"{safeName}.png"
                        : $"Tour{poi.TourId}/{safeName}.png";

                    var entry = zip.CreateEntry(entryName, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(pngBytes);
                }
            }

            zipStream.Position = 0;
            string zipName = tourId.HasValue ? $"QR_Tour{tourId}.zip" : "QR_TatCaDiaDiem.zip";
            return File(zipStream.ToArray(), "application/zip", zipName);
        }

        // GET: /QR/Print/{id}
        public async Task<IActionResult> Print(int id)
        {
            var poi = await _context.POIs.FindAsync(id);
            if (poi == null) return NotFound();
            return View(poi);
        }

        // GET: /QR/PrintAll?tourId=1
        public async Task<IActionResult> PrintAll(int? tourId)
        {
            var query = _context.POIs.AsQueryable();
            if (tourId.HasValue && tourId > 0)
                query = query.Where(p => p.TourId == tourId);

            var pois = await query.OrderBy(p => p.TourId).ThenBy(p => p.Name_VI).ToListAsync();
            var tours = await _context.Tours.ToListAsync();

            ViewBag.Tours = tours;
            ViewBag.SelectedTourId = tourId;
            return View(pois);
        }

        // =========================================================
        // 🌟 TẠO MÃ QR TỪ TÊN POI (DÙNG CHUNG VỚI APP VÀ RAZOR)
        // =========================================================
        // "Chợ Bến Thành" → "poi_cho_ben_thanh"
        // ⚠️ PHẢI giống hệt GenerateQrCode() bên App (QRCodePage.xaml.cs)
        public static string GenerateQrCode(string poiName)
        {
            if (string.IsNullOrWhiteSpace(poiName)) return "poi_unknown";

            string result = poiName.ToLower().Trim();
            result = RemoveDiacritics(result);
            result = Regex.Replace(result, @"\s+", "_");
            result = Regex.Replace(result, @"[^a-z0-9_]", "");
            return $"poi_{result}";
        }

        private static string RemoveDiacritics(string text)
        {
            var map = new Dictionary<string, string>
            {
                {"à","a"},{"á","a"},{"â","a"},{"ã","a"},{"ä","a"},{"å","a"},
                {"ă","a"},{"ắ","a"},{"ặ","a"},{"ằ","a"},{"ẳ","a"},{"ẵ","a"},
                {"ấ","a"},{"ầ","a"},{"ẩ","a"},{"ẫ","a"},{"ậ","a"},
                {"è","e"},{"é","e"},{"ê","e"},{"ë","e"},
                {"ế","e"},{"ề","e"},{"ể","e"},{"ễ","e"},{"ệ","e"},
                {"ì","i"},{"í","i"},{"î","i"},{"ï","i"},{"ị","i"},{"ỉ","i"},{"ĩ","i"},
                {"ò","o"},{"ó","o"},{"ô","o"},{"õ","o"},{"ö","o"},{"ø","o"},
                {"ố","o"},{"ồ","o"},{"ổ","o"},{"ỗ","o"},{"ộ","o"},
                {"ơ","o"},{"ớ","o"},{"ờ","o"},{"ở","o"},{"ỡ","o"},{"ợ","o"},
                {"ù","u"},{"ú","u"},{"û","u"},{"ü","u"},
                {"ư","u"},{"ứ","u"},{"ừ","u"},{"ử","u"},{"ữ","u"},{"ự","u"},{"ụ","u"},
                {"ỳ","y"},{"ý","y"},{"ỷ","y"},{"ỹ","y"},{"ỵ","y"},
                {"đ","d"},{"ñ","n"},{"ç","c"},
            };
            string r = text;
            foreach (var kvp in map) r = r.Replace(kvp.Key, kvp.Value);
            return r;
        }

        private static byte[] GenerateQRPng(string content, int pixelsPerModule = 6)
        {
            using var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            using var code = new PngByteQRCode(data);
            return code.GetGraphic(pixelsPerModule);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Length > 60 ? name[..60] : name;
        }
    }
}