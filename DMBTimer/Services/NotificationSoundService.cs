using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace DMBTimer.Services;

public sealed class NotificationSoundService : IDisposable
{
    private const string ImportedFileName = "notification.wav";
    private readonly MediaPlayer _player = new();

    public async Task<string> ImportWavAsync(StorageFile source)
    {
        if (!string.Equals(source.FileType, ".wav", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only WAV files are supported.");

        var copy = await source.CopyAsync(
            ApplicationData.Current.LocalFolder,
            ImportedFileName,
            NameCollisionOption.ReplaceExisting);
        return copy.Path;
    }

    public async Task PlayAsync(string? customPath)
    {
        _player.Pause();

        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(customPath);
                _player.Source = MediaSource.CreateFromStorageFile(file);
                _player.Play();
                return;
            }
            catch
            {
                // Fall back to the packaged sound below.
            }
        }

        _player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/dembel_fanfare.wav"));
        _player.Play();
    }

    public void Dispose() => _player.Dispose();
}
