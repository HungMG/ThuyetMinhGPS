using Microsoft.Maui.Media;

namespace TourGuideApp.Services;

public class NarrationEngine
{
    // CancellationToken dùng để "rút phích cắm" ép nó nín ngay lập tức nếu khách chuyển bài
    private CancellationTokenSource _cancelTokenSource;

    public async Task SpeakAsync(string text, string langCode)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        // 1. Dừng ngay câu đang đọc (nếu có)
        Stop();
        _cancelTokenSource = new CancellationTokenSource();

        try
        {
            // 2. Lấy danh sách toàn bộ giọng đọc đang có trên điện thoại
            var locales = await TextToSpeech.Default.GetLocalesAsync();

            // 3. Truy tìm giọng đọc phù hợp với ngôn ngữ khách chọn
            // Dùng StartsWith để bao quát (VD: "zh" sẽ tóm được cả "zh-CN", "zh-TW")
            var matchedLocale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(langCode, StringComparison.OrdinalIgnoreCase));

            // Cấu hình giọng đọc
            var options = new SpeechOptions
            {
                Pitch = 1.0f,   // Độ thanh/trầm (1.0 là mặc định)
                Volume = 1.0f,  // Âm lượng tối đa
                Locale = matchedLocale // Gắn giọng đúng nước đó vào
            };

            // 4. Bắt đầu đọc (và cho phép bị ngắt ngang bởi _cancelTokenSource)
            await TextToSpeech.Default.SpeakAsync(text, options, cancelToken: _cancelTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            // Lỗi này văng ra khi mình chủ động ép nó ngắt âm thanh -> Bình thường, bỏ qua!
            Console.WriteLine("Đã ngắt giọng đọc cũ.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi TTS: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
        {
            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
            _cancelTokenSource = null;
        }
    }
}