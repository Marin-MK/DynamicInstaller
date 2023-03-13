using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Channels;

namespace DynamicInstaller;

public class Downloader
{
    public string URL { get; protected set; }
    public string? Filename { get; protected set; }
    public Stream Stream { get; protected set; }
    public bool Done { get; protected set; }
    public bool Cancelled { get; protected set; }
    public bool HadError { get; protected set; }
    public float Progress { get; protected set; }
    public bool CloseStream { get; set; } = false;

    public Action? OnFinished;
    public Action? OnCancelled;
    public Action<DownloadProgress>? OnProgress;
    public Action<Exception>? OnError;

    private CancellationTokenSource CancellationTokenSource;

    public static string DownloadString(string url, TimeSpan? timeout = null, Encoding? encoding = null)
    {
        MemoryStream stream = new MemoryStream();
        Downloader fd = new Downloader(url, stream);
        fd.Download(timeout ?? TimeSpan.FromSeconds(10));
        string text = (encoding ?? Encoding.Default).GetString(stream.ToArray());
        stream.Dispose();
        return text;
    }

    public static bool DownloadFile(string url, string Filename, TimeSpan? timeout = null)
    {
        Downloader fd = new Downloader(url, Filename);
        fd.Download(timeout ?? TimeSpan.FromSeconds(10));
        return !fd.HadError;
    }

    public Downloader(string url, string filename) : this(url, new FileStream(filename, FileMode.Create, FileAccess.Write), new CancellationTokenSource())
    {
        this.CloseStream = true;
    }

    public Downloader(string url, string filename, CancellationTokenSource cancellationTokenSource) : this(url, new FileStream(filename, FileMode.Create, FileAccess.Write), cancellationTokenSource)
    {
        this.CloseStream = true;
    }

    public Downloader(string url, Stream stream) : this(url, stream, new CancellationTokenSource()) { }

    public Downloader(string url, Stream stream, CancellationTokenSource cancellationTokenSource)
    {
        this.URL = url;
        this.Stream = stream;
        this.CancellationTokenSource = cancellationTokenSource;
    }

    public void Download()
    {
        Task.WaitAll(DownloadAsync());
    }

    public void Download(TimeSpan timeout)
    {
        Task.WaitAll(DownloadAsync(timeout));
    }

    public void Download(TimeSpan timeout, TimeSpan? progressReportCooldown, int? fixedReportCount = null)
    {
        Task.WaitAll(DownloadAsync(timeout, progressReportCooldown, fixedReportCount));
    }

    public async Task DownloadAsync()
    {
        await DownloadAsync(TimeSpan.FromSeconds(5), null);
    }

    public async Task DownloadAsync(TimeSpan timeout)
    {
        await DownloadAsync(timeout, null);
    }

    public async Task DownloadAsync(TimeSpan timeout, TimeSpan? progressReportCooldown, int? fixedReportCount = null)
    {
        var client = new HttpClient();
        HttpResponseMessage response = null!;
        try
        {
            client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            response = await client.GetAsync(this.URL, HttpCompletionOption.ResponseContentRead, CancellationTokenSource.Token);
            if (CancellationTokenSource.IsCancellationRequested)
            {
                OnCancelled?.Invoke();
                return;
            }
            response.EnsureSuccessStatusCode();

            using var content = await response.Content.ReadAsStreamAsync();
            if (CancellationTokenSource.IsCancellationRequested)
            {
                OnCancelled?.Invoke();
                return;
            }

            if (!string.IsNullOrEmpty(Filename))
            {
                string dirName = Path.GetDirectoryName(Filename)!;
                if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);
            }
            
            long totalBytes = (long) response.Content.Headers.ContentLength!;
            long totalRead = 0;
            byte[] buffer = new byte[8192];
            bool reported1 = false;
            DownloadProgress progressObject = new DownloadProgress(0, totalBytes);

            if (fixedReportCount != null)
            {
                long fixedReportSize = fixedReportCount == 0 ? long.MaxValue : totalBytes / (long) fixedReportCount;
                int reportCount = 0;
                while (totalRead < totalBytes)
                {
                    if (Cancelled || CancellationTokenSource.IsCancellationRequested) break;
                    var bytesRead = await content.ReadAsync(buffer, CancellationTokenSource.Token);
                    if (Cancelled || CancellationTokenSource.IsCancellationRequested) break;
                    totalRead += bytesRead;
                    if (bytesRead == 0) break;
                    await this.Stream.WriteAsync(buffer, 0, bytesRead, CancellationTokenSource.Token);
                    if (fixedReportSize * reportCount < totalRead)
                    {
                        progressObject.BytesRead = totalRead;
                        OnProgress?.Invoke(progressObject);
                        reportCount++;
                        if (bytesRead == totalBytes) reported1 = true;
                    }
                }
            }
            else
            {
                Stopwatch timeSinceLastReport = Stopwatch.StartNew();
                bool firstTime = true;
                while (totalRead < totalBytes)
                {
                    if (Cancelled || CancellationTokenSource.IsCancellationRequested) break;
                    var bytesRead = await content.ReadAsync(buffer, CancellationTokenSource.Token);
                    if (Cancelled || CancellationTokenSource.IsCancellationRequested) break;
                    totalRead += bytesRead;
                    if (bytesRead == 0) break;
                    await this.Stream.WriteAsync(buffer, 0, bytesRead, CancellationTokenSource.Token);
                    if (firstTime || !progressReportCooldown.HasValue || timeSinceLastReport.ElapsedTicks >= progressReportCooldown.Value.Ticks)
                    {
                        progressObject.BytesRead = totalRead;
                        OnProgress?.Invoke(progressObject);
                        firstTime = false;
                        timeSinceLastReport.Restart();
                        if (bytesRead == totalBytes) reported1 = true;
                    }
                }
            }

            if (Cancelled || CancellationTokenSource.IsCancellationRequested)
            {
                OnCancelled?.Invoke();
                if (CloseStream)
                {
                    Stream?.Dispose();
                    Stream = null;
                }
            }
            else
            {
                response?.Dispose();
                response = null;
                if (CloseStream)
                {
                    Stream?.Dispose();
                    Stream = null;
                }
                Done = true;
                if (!reported1) OnProgress?.Invoke(new DownloadProgress(totalBytes, totalBytes));
                OnFinished?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            OnCancelled?.Invoke();
            if (CloseStream)
            {
                Stream?.Dispose();
                Stream = null;
            }
            HadError = true;
        }
        catch (Exception ex) when (ex is InvalidOperationException || ex is HttpRequestException || ex is TaskCanceledException || ex is UriFormatException || ex is NotSupportedException)
        {
            Console.WriteLine("Error downloading: " + ex.Message + "\n" + ex.StackTrace);
            if (CloseStream)
            {
                Stream?.Dispose();
                Stream = null;
            }
            OnError?.Invoke(ex);
            HadError = true;
        }
        finally
        {
            client.Dispose();
            response?.Dispose();
            if (CloseStream)
            {
                Stream?.Dispose();
                Stream = null;
            }
        }
    }

    /// <summary>
    /// Stops the downloader and deletes the downloaded file if it was not fully downloaded.
    /// </summary>
    public void Cancel()
    {
        if (Cancelled) throw new Exception("Downloader already cancelled.");
        CancellationTokenSource.Cancel();
    }
}

public struct DownloadProgress
{
    internal static readonly (string Name, long Unit)[] ByteMagnitudes =
    {
        ("B", (long) Math.Pow(1024, 0)),
        ("KiB", (long) Math.Pow(1024, 1)),
        ("MiB", (long) Math.Pow(1024, 2)),
        ("GiB", (long) Math.Pow(1024, 3)),
        ("TiB", (long) Math.Pow(1024, 4)),
        ("PiB", (long) Math.Pow(1024, 5)),
        ("EiB", (long) Math.Pow(1024, 6))
    };

    public long BytesRead;
    public long TotalBytes;
    public long BytesLeft => TotalBytes - BytesRead;
    public float Factor => (float) BytesRead / TotalBytes;
    public float Percentage => Factor * 100;

    public DownloadProgress(long bytesRead, long totalBytes)
    {
        BytesRead = bytesRead;
        TotalBytes = totalBytes;
    }

    public string ReadBytesToString()
    {
        return BytesToString(BytesRead);
    }

    public string TotalBytesToString()
    {
        return BytesToString(TotalBytes);
    }

    public static string BytesToString(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        for (int i = ByteMagnitudes.Length - 1; i >= 0; i--)
        {
            if (bytes >= ByteMagnitudes[i].Unit)
            {
                long units = bytes / ByteMagnitudes[i].Unit;
                return $"{units}.{double.Truncate(Math.Round((double) (bytes - units * ByteMagnitudes[i].Unit) / ByteMagnitudes[i].Unit, 2) * 100).ToString().TrimEnd('0').PadLeft(1, '0')} {ByteMagnitudes[i].Name}";
            }
        }
        return $"{bytes} B";
    }
}