using Newtonsoft.Json.Linq;

namespace TourGuideAdmin.Services
{
    public class TranslationService
    {
        private readonly HttpClient _httpClient;

        public TranslationService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            try
            {
                // Sử dụng API Google Translate miễn phí (dành cho mục đích học tập)
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl={targetLanguage}&dt=t&q={Uri.EscapeDataString(text)}";

                var response = await _httpClient.GetStringAsync(url);
                var json = JArray.Parse(response);

                // Trích xuất kết quả dịch từ mảng JSON
                return json[0][0][0].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🚨 Lỗi dịch thuật: {ex.Message}");
                return text; // Trả về text gốc nếu lỗi
            }
        }
    }
}