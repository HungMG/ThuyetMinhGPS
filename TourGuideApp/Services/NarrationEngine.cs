using Microsoft.Maui.Media;
using Plugin.Maui.Audio;

namespace TourGuideApp.Services;

public class NarrationEngine
{
    private CancellationTokenSource _cancelTokenSource;
    private IAudioPlayer _audioPlayer; 

 
    public async Task SpeakAsync(string text, string langCode, string audioFileName = "")
    {
        
        Stop();

        // ----------------------------------------------------
        // KỊCH BẢN A: ƯU TIÊN PHÁT FILE MP3 CHUYÊN NGHIỆP
        // ----------------------------------------------------
        if (!string.IsNullOrEmpty(audioFileName))
        {
            try
            {
                var audioManager = AudioManager.Current;
                // Mở file MP3 từ thư mục Resources/Raw của MAUI
                var stream = await FileSystem.OpenAppPackageFileAsync(audioFileName);
                _audioPlayer = audioManager.CreatePlayer(stream);
                _audioPlayer.Play();

                Console.WriteLine("Đang phát file Audio MP3...");
                return; // Đọc MP3 thành công thì thoát luôn, không dùng chị Google nữa
            }
            catch (Exception)
            {
                Console.WriteLine($"Không tìm thấy file {audioFileName}. Đang chuyển sang chế độ TTS chữa cháy...");
                // Lỗi không tìm thấy file -> Rớt xuống kịch bản B để chữa cháy
            }
        }

        // ----------------------------------------------------
        // KỊCH BẢN B: CHỮA CHÁY BẰNG CHỊ GOOGLE (TTS)
        // ----------------------------------------------------
        if (string.IsNullOrWhiteSpace(text)) return;

        _cancelTokenSource = new CancellationTokenSource();

        try
        {
            var locales = await TextToSpeech.Default.GetLocalesAsync();
            var matchedLocale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(langCode, StringComparison.OrdinalIgnoreCase));

            var options = new SpeechOptions
            {
                Pitch = 1.0f,
                Volume = 1.0f,
                Locale = matchedLocale
            };

            await TextToSpeech.Default.SpeakAsync(text, options, cancelToken: _cancelTokenSource.Token);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Đã ngắt giọng đọc TTS cũ.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi TTS: {ex.Message}");
        }
    }

    public void Stop()
    {
        // 1. Rút phích cắm chị Google
        if (_cancelTokenSource != null && !_cancelTokenSource.IsCancellationRequested)
        {
            _cancelTokenSource.Cancel();
            _cancelTokenSource.Dispose();
            _cancelTokenSource = null;
        }

        // 2. Rút phích cắm máy phát MP3
        if (_audioPlayer != null && _audioPlayer.IsPlaying)
        {
            _audioPlayer.Stop();
            _audioPlayer.Dispose();
            _audioPlayer = null;
        }
    }
}