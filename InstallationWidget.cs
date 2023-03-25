using amethyst;
using amethyst.Windows;
using odl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicInstaller;

internal class InstallationWidget : MainWidget
{
    Downloader fileDownloader;
    Label statusLabel;
    Label progressLabel;
    ProgressBar progressBar;

    public InstallationWidget(IContainer parent, StepWidget stepWidget) : base(parent, stepWidget)
    {
        SetBackgroundColor(SystemColors.LightBorderFiller);
        statusLabel = new Label(this);
        statusLabel.SetFont(Font.Get("Arial", 12));
        statusLabel.SetBlendMode(BlendMode.Blend);
        statusLabel.SetPadding(40, 20);
        statusLabel.SetText("Connecting to server...");

        progressLabel = new Label(this);
        progressLabel.SetFont(Font.Get("Arial", 12));
        progressLabel.SetBlendMode(BlendMode.Blend);
        progressLabel.SetPadding(40, 42);
        progressLabel.SetVisible(false);

        progressBar = new ProgressBar(this);
        progressBar.SetHDocked(true);
        progressBar.SetPadding(40, 74);
        progressBar.SetHeight(32);

        Widget divider = new Widget(this);
        divider.SetBottomDocked(true);
        divider.SetHDocked(true);
        divider.SetPadding(0, 0, 0, 1);
        divider.SetBackgroundColor(SystemColors.Divider);
        divider.SetHeight(1);
    }

    public void Download()
    {
        string tempFile = Path.GetTempFileName();
        bool updatedLabel = false;
        DynamicCallbackManager<DownloadProgress> dcm = new DynamicCallbackManager<DownloadProgress>(100, x => Graphics.Schedule(() =>
        {
            if (Program.Window.Disposed) return;
            if (!updatedLabel)
            {
                statusLabel.SetText("Downloading files...");
                updatedLabel = true;
            }
            progressBar.SetProgress((float)x.Factor);
            progressLabel.SetText($"Downloaded {x.ReadBytesToString()} of {x.TotalBytesToString()}");
            progressLabel.SetVisible(true);
            Graphics.Update();
        }), () => Graphics.Update());
        bool success = false;
        try
        {
            Logger.WriteLine("Downloading program files...");
            success = Downloader.DownloadFile(VersionMetadata.ProgramDownloadLink, tempFile, null, dcm);
        }
        catch (Exception ex)
        {
            Logger.Error("Downloader failed: " + ex.Message + "\n" + ex.StackTrace);
            statusLabel.SetText("Download failed. Try again later.");
            progressLabel.SetVisible(true);
            progressLabel.SetText($"Make sure you are connected to the internet and allow the program past anti-virus software.");
            progressBar.SetVisible(false);
            Program.Window.ForceClose = true;
        }
        if (success)
        {
            try
            {
                statusLabel.SetText("Extracting files...");
                Stopwatch stopwatch = Stopwatch.StartNew();
                int updateFrequency = 16; // Update the screen every x ms
                string destFolder = Path.GetFullPath(Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.ProgramInstallPath)).Replace('\\', '/');
                if (Directory.Exists(destFolder)) Directory.Delete(destFolder, true);
                Logger.WriteLine("Initializing program files zip archive...");
                Archive archive = new Archive(tempFile);
                int len = archive.Files.Count;
                for (int i = 0; i < len; i++)
                {
                    if (Program.Window.Disposed) break;
                    var file = archive.Files[i];
                    if (stopwatch.ElapsedMilliseconds > updateFrequency)
                    {
                        stopwatch.Restart();
                        progressLabel.SetText($"Extracting {file.Filename.Replace('\\', '/')}");
                        Graphics.Update();
                    }
                    file.Extract(destFolder, true);
                    progressBar.SetProgress((float) (i + 1) / len);
                }
                archive.Dispose();
                File.Delete(tempFile);
                if (Program.Window.Disposed) return;
                Logger.WriteLine("Successfully extracted all files.");
                string arg1 = Process.GetCurrentProcess().MainModule!.FileName; // This executable's filename
                string arg2 = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.CoreLibraryPath, "updater.exe").Replace('/', '\\'); // .../Program Files/MK/Core/updater.exe
                if (File.Exists(arg2) && arg1 != arg2) File.Delete(arg2);
                if (arg1 != arg2)
                {
                    Logger.WriteLine($"Copying installer from '{arg1}' to '{arg2}'.");
                    File.Copy(arg1, arg2);
                }
                Program.Window.MarkInstallationComplete();
            }
            catch (Exception ex)
            {
                Logger.Error("Archive extractor failed: " + ex.Message + "\n" + ex.StackTrace);
                statusLabel.SetText("Extraction failed. Try again later.");
                progressLabel.SetVisible(false);
                progressBar.SetVisible(false);
                Program.Window.ForceClose = true;
            }
        }
    }
}
