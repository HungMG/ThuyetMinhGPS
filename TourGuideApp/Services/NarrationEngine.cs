using System;
using System.Linq; // Thêm thư viện này để dùng LINQ tìm kiếm
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Media;

namespace TourGuideApp.Services
{
    public class NarrationEngine
    {
        private CancellationTokenSource _cts;
        public bool IsSpeaking { get; private set; } = false;

        // Thêm tham số languageCode (Mặc định là tiếng Việt "vi")
        public async Task SpeakAsync(string textToRead, string languageCode = "vi")
        {
            StopSpeaking();
            _cts = new CancellationTokenSource();
            IsSpeaking = true;

            try
            {
                // Bước 1: Xin điện thoại danh sách tất cả các giọng đọc nó đang có
                var locales = await TextToSpeech.Default.GetLocalesAsync();

                // Bước 2: Lọc ra cái giọng khớp với mã ngôn ngữ mình muốn (vi, en, zh, ja, ko)
                var selectedLocale = locales.FirstOrDefault(l => l.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase));

                // Bước 3: Nạp giọng đọc đó vào Options
                var options = new SpeechOptions
                {
                    Volume = 1.0f,
                    Pitch = 1.0f,
                    Locale = selectedLocale // Cốt lõi nằm ở dòng này!
                };

                System.Diagnostics.Debug.WriteLine($"🔊 Đang đọc ({languageCode}): {textToRead}");
                await TextToSpeech.Default.SpeakAsync(textToRead, options, cancelToken: _cts.Token);
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("🔇 Đã ngắt giọng đọc audio.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Lỗi TTS: {ex.Message}");
            }
            finally
            {
                IsSpeaking = false;
            }
        }

        public void StopSpeaking()
        {
            if (_cts?.IsCancellationRequested == false)
            {
                _cts.Cancel();
            }
        }
    }
}