using System;
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
    private int _isProcessing;

    public void Start() => Clipboard.ContentChanged += OnClipboardContentChanged;

    public void Dispose() => Clipboard.ContentChanged -= OnClipboardContentChanged;

    private async void OnClipboardContentChanged(object sender, object args)
    {
        if (Interlocked.Exchange(ref _isProcessing, 1) == 1) return;

        try
        {
            await Task.Delay(100);
            await SaveClipboardImageAsync();
        }
        catch { } // Silently ignore errors (clipboard locked, unsupported format, etc.)
        finally { Interlocked.Exchange(ref _isProcessing, 0); }
    }

    private static async Task SaveClipboardImageAsync()
    {
        var content = Clipboard.GetContent();
        if (!content.Contains(StandardDataFormats.Bitmap)) return;

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
