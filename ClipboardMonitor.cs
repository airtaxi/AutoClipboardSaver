using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace AutoClipboardSaver;

public sealed partial class ClipboardMonitor : IDisposable
{
    public bool IsRecording { get; set; } = true;

    private int _isProcessing;
    private readonly ConcurrentDictionary<string, DateTime> _expirationRegistry = new();
    private Timer _expirationTimer;

    public void Start()
    {
        Clipboard.ContentChanged += OnClipboardContentChanged;
        InitializeExpirationTracking();
    }

    public void Dispose()
    {
        Clipboard.ContentChanged -= OnClipboardContentChanged;
        _expirationTimer?.Dispose();
    }

    private void InitializeExpirationTracking()
    {
        var saveDirectoryPath = Configuration.SaveDirectoryPath;
        if (Directory.Exists(saveDirectoryPath))
        {
            foreach (var filePath in Directory.GetFiles(saveDirectoryPath, "clipboard_*.jpg"))
                _expirationRegistry[filePath] = new FileInfo(filePath).CreationTime;
        }

        _expirationTimer = new Timer(OnExpirationTimerTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void OnExpirationTimerTick(object state)
    {
        var expirationMinutes = Configuration.ExpirationMinutes;

        foreach (var entry in _expirationRegistry)
        {
            if (!File.Exists(entry.Key))
            {
                _expirationRegistry.TryRemove(entry.Key, out _);
                continue;
            }

            if (expirationMinutes <= 0) continue;

            if (DateTime.Now - entry.Value >= TimeSpan.FromMinutes(expirationMinutes))
            {
                try { File.Delete(entry.Key); }
                catch { }
                _expirationRegistry.TryRemove(entry.Key, out _);
            }
        }
    }

    private async void OnClipboardContentChanged(object sender, object args)
    {
        if (!IsRecording) return;
        else if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;

        try
        {
            await Task.Delay(100);
            var savedFilePath = await SaveClipboardImageAsync();
            if (savedFilePath != null)
                _expirationRegistry[savedFilePath] = DateTime.Now;
        }
        catch { } // Silently ignore errors (clipboard locked, unsupported format, etc.)
        finally { Interlocked.Exchange(ref _isProcessing, 0); }
    }

    private static async Task<string> SaveClipboardImageAsync()
    {
        var content = Clipboard.GetContent();
        if (!content.Contains(StandardDataFormats.Bitmap)) return null;

        var bitmapReference = await content.GetBitmapAsync();
        using var bitmapStream = await bitmapReference.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(bitmapStream);
        using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        var saveDirectoryPath = Configuration.SaveDirectoryPath;
        if (!Directory.Exists(saveDirectoryPath))
            Directory.CreateDirectory(saveDirectoryPath);

        string fileName = Configuration.SaveWithTimestamp
            ? $"clipboard_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.jpg"
            : "clipboard.jpg";
        string filePath = Path.Combine(saveDirectoryPath, fileName);

        // Encode to JPEG in memory
        using var outputStream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outputStream);
        encoder.SetSoftwareBitmap(softwareBitmap);
        await encoder.FlushAsync();

        // Read the encoded bytes and write to file
        outputStream.Seek(0);
        var readBuffer = new Windows.Storage.Streams.Buffer((uint)outputStream.Size);
        await outputStream.ReadAsync(readBuffer, (uint)outputStream.Size, InputStreamOptions.None);
        var imageBytes = new byte[readBuffer.Length];
        DataReader.FromBuffer(readBuffer).ReadBytes(imageBytes);
        await File.WriteAllBytesAsync(filePath, imageBytes);

        if (Configuration.SaveWithTimestamp) EnforceMaximumImageCount(saveDirectoryPath);

        return Configuration.SaveWithTimestamp ? filePath : null;
    }

    private static void EnforceMaximumImageCount(string directoryPath)
    {
        var maximumImages = Configuration.MaxImages;
        if (maximumImages < 0) return;

        var imageFiles = Directory.GetFiles(directoryPath, "clipboard_*.jpg")
            .Select(filePath => new FileInfo(filePath))
            .OrderByDescending(fileInfo => fileInfo.CreationTime)
            .ToList();

        for (int index = maximumImages; index < imageFiles.Count; index++) imageFiles[index].Delete();
    }
}
