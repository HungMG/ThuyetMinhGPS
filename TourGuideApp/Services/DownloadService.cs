using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace TourGuideApp.Services
{
    public class DownloadService
    {
        private readonly HttpClient _client = new HttpClient();

        // Hàm này cân được mọi loại file: Ảnh (.jpg), Audio (.mp3), Bản đồ (.mbtiles)
        public async Task<string> DownloadFileAsync(string url, string fileName)
        {
            try
            {
                var response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                // Lấy thư mục an toàn của App trên điện thoại (Không bị hệ thống xóa bậy)
                string localPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                using var fs = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fs);

                // Tải xong, trả về cái đường dẫn nằm trong máy
                return localPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi tải file {fileName}: {ex.Message}");
                return null; // Tải xịt thì trả về null
            }
        }
    }
}