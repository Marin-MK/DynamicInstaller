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
    FileDownloader fileDownloader;

    public InstallationWidget(IContainer parent, StepWidget stepWidget) : base(parent, stepWidget)
    {
        SetBackgroundColor(SystemColors.LightBorderFiller);
        Label statusLabel = new Label(this);
        statusLabel.SetFont(Font.Get("Arial", 12));
        statusLabel.SetBlendMode(BlendMode.Blend);
        statusLabel.SetPadding(40, 20);
        statusLabel.SetText("Connecting to server...");

        Label progressLabel = new Label(this);
        progressLabel.SetFont(Font.Get("Arial", 12));
        progressLabel.SetBlendMode(BlendMode.Blend);
        progressLabel.SetPadding(40, 42);
        progressLabel.SetVisible(false);

        ProgressBar progressBar = new ProgressBar(this);
        progressBar.SetHDocked(true);
        progressBar.SetPadding(40, 74);
        progressBar.SetHeight(32);

        Widget divider = new Widget(this);
        divider.SetBottomDocked(true);
        divider.SetHDocked(true);
        divider.SetPadding(0, 0, 0, 1);
        divider.SetBackgroundColor(SystemColors.Divider);
        divider.SetHeight(1);

        string tempFile = Path.GetTempFileName();
        fileDownloader = new FileDownloader(Config.ProgramDownloadLink, tempFile);
        bool updatedLabel = false;
        fileDownloader.OnProgress += x =>
        {
            if (!updatedLabel)
            {
                statusLabel.SetText("Downloading files...");
                updatedLabel = true;
            }
            progressBar.SetProgress(x.Factor);
            progressLabel.SetText($"Downloaded {x.ReadBytesToString()} of {x.TotalBytesToString()}");
            progressLabel.SetVisible(true);
            Graphics.Update();
        };
        fileDownloader.OnError += x =>
        {
            Console.WriteLine(x.Message);
            Console.WriteLine(x.StackTrace);
            statusLabel.SetText("Download failed. Try again later.");
            progressLabel.SetVisible(true);
            progressLabel.SetText($"Make sure you are connected to the internet and allow the program past anti-virus software.");
            progressBar.SetVisible(false);
            Program.Window.ForceClose = true;
        };
        Graphics.Schedule(() => fileDownloader.Download(TimeSpan.FromSeconds(10), null, 100));
        fileDownloader.OnFinished += () =>
        {
            try 
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                int updateFrequency = 10; // Update the screen every x ms
                Archive archive = new Archive(tempFile);
                int len = archive.Files.Count;
                string destFolder = Path.GetFullPath(Path.Combine(Program.ProgramFilesFolder!, Config.ProgramInstallPath)).Replace('\\', '/');
                for (int i = 0; i < len; i++)
                {
                    var file = archive.Files[i];
                    if (stopwatch.ElapsedMilliseconds > updateFrequency)
                    {
                        stopwatch.Restart();
                        progressLabel.SetText($"Extracting {Path.Combine(destFolder, file.Filename).Replace('\\', '/')}");
                        Graphics.Update();
                    }
                    file.Extract(destFolder, true);
                    progressBar.SetProgress((float) (i + 1) / len);
                }
                archive.Dispose();
                File.Delete(tempFile);
                Program.Window.MarkInstallationComplete();
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
                Console.WriteLine(x.StackTrace);
                statusLabel.SetText("Extraction failed. Try again later.");
                progressLabel.SetVisible(false);
                progressBar.SetVisible(false);
                Program.Window.ForceClose = true;
            }
        };
    }
}
