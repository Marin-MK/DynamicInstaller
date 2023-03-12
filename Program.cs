using amethyst;
using NativeLibraryLoader;
using odl;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace DynamicInstaller;

public class Program
{
    internal const string CoreLibraryDownloadLink = "https://www.dropbox.com/s/1hmpo9ozu0rt8qm/MK-Core-Libraries.zip?dl=1";
    internal const string CoreLibraryPath = "MK/Core";

    internal static readonly string[] RequiredFiles =
    {
        "libfreetype-6.dll",
        "libpng16-16.dll",
        "SDL2.dll",
        "SDL2_image.dll",
        "SDL2_ttf.dll",
        "zlib1.dll"
    };

    internal static string? ProgramFilesFolder;
    internal static string? DependencyFolder;
    internal static string ProgramExecutablePath => Path.Combine(ProgramFilesFolder!, Config.ProgramInstallPath, Config.ProgramLaunchFile).Replace('/', '\\');

    internal static InstallerWindow Window;

    public static void Main(string[] args)
    {
        if (!Setup()) return;
        Amethyst.Start(GetPathInfo(), false, false);

        Window = new InstallerWindow();
        Window.OnClosing += x =>
        {
            if (!Window.ForceClose && !Window.SkipExitPrompt)
            {
                x.Value = false;
                QuitWithConfirmation();
            }
        };
        Window.Setup();

        Window.Show();

        Amethyst.Run();

        if (!Window.Disposed) Window.Dispose();
        Amethyst.Stop();
    }

    private static bool Setup()
    {
        ProgramFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);//, Environment.SpecialFolderOption.Create);
        if (string.IsNullOrEmpty(ProgramFilesFolder))
        {
            if (Directory.Exists("/usr/local")) ProgramFilesFolder = "/usr/local";
            else
            {
                ProgramFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                if (string.IsNullOrEmpty(ProgramFilesFolder))
                {
                    ProgramFilesFolder = "./";
                }
            }
        }
        //ProgramFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // TODO DEBUG: USE FOLDER THAT DOES NOT REQUIRE ELEVATED PERMISSIONS
        if (!HasAllDependencies())
        {
            if (!InstallDependencies()) return false;
        }
        DependencyFolder = Path.Combine(ProgramFilesFolder, CoreLibraryPath, "lib", "windows");
        return true;
    }

    private static PathInfo GetPathInfo()
    {
        PathPlatformInfo windows = new PathPlatformInfo(NativeLibraryLoader.Platform.Windows);
        windows.AddPath("libsdl2", DependencyFolder + "/SDL2.dll");
        windows.AddPath("libz", DependencyFolder + "/zlib1.dll");
        windows.AddPath("libsdl2_image", DependencyFolder + "/SDL2_image.dll");
        windows.AddPath("libpng", DependencyFolder + "/libpng16-16.dll");
        windows.AddPath("libsdl2_ttf", DependencyFolder + "/SDL2_ttf.dll");
        windows.AddPath("libfreetype", DependencyFolder + "/libfreetype-6.dll");
        return PathInfo.Create(windows);
    }

    private static bool HasAllDependencies()
    {
        foreach (string requiredFile in RequiredFiles)
        {
            if (!File.Exists(Path.Combine(ProgramFilesFolder!, CoreLibraryPath, "lib", "windows", requiredFile)))
            {
                return false;
            }
        }
        return true;
    }

    private static bool InstallDependencies()
    {
        string tempFile = Path.GetTempFileName();
        FileDownloader downloader = new FileDownloader(CoreLibraryDownloadLink, tempFile);
        downloader.OnError += x => Console.WriteLine($"Error downloading core libraries: {x}");
        downloader.Download(TimeSpan.FromSeconds(10));
        if (downloader.HadError) return false;
        Archive coreLibFile = new Archive(tempFile);
        string extractURL = Path.Combine(ProgramFilesFolder!, CoreLibraryPath);
        coreLibFile.Extract(extractURL);
        coreLibFile.Dispose();
        File.Delete(tempFile);
        return HasAllDependencies();
    }

    internal static void SetFileAssociations(List<string> fileAssocs)
    {
        fileAssocs.ForEach(x => SetFileAssociation(x));
    }

    internal static bool SetFileAssociation(string fileAssoc)
    {
        if (Graphics.Platform != odl.Platform.Windows) return false;
        string assocOutput = GetCommandOutput($"assoc {fileAssoc}");
        Match assocMatch = Regex.Match(assocOutput, $@"{fileAssoc.Replace(".", "\\.")}=(.+)$");
        string ftypeName = "";
        if (string.IsNullOrEmpty(assocOutput) || !assocMatch.Success)
        {
            // Create new file association
            ftypeName = fileAssoc.Substring(1) + "file";
            string arg = fileAssoc + "=" + ftypeName;
            string newAssocOutput = GetCommandOutput($"assoc {arg}");
            if (!newAssocOutput.Contains(arg)) return false;
        }
        else
        {
            // Change existing ftype
            ftypeName = assocMatch.Groups[1].Value.Replace("\r", "");
        }
        // We have an ftype, so now we associate this ftype with the program.
        string ftypeOutput = GetCommandOutput($"ftype {ftypeName}=\"{ProgramExecutablePath}\" %1");
        return ftypeOutput.StartsWith(ftypeName + "=");
    }

    internal static string GetCommandOutput(string command)
    {
        if (Graphics.Platform != odl.Platform.Windows) throw new PlatformNotSupportedException();
        ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd", "/c " + command);
        procStartInfo.RedirectStandardOutput = true;
        procStartInfo.UseShellExecute = false;
        procStartInfo.CreateNoWindow = true;
        Process proc = new Process();
        proc.StartInfo = procStartInfo;
        proc.Start();
        return proc.StandardOutput.ReadToEnd();
    }

    internal static void RunExecutable()
    {
        Process proc = new Process();
        proc.StartInfo = new ProcessStartInfo(ProgramExecutablePath);
        proc.StartInfo.UseShellExecute = true;
        proc.Start();
    }

    internal static void QuitWithConfirmation()
    {
        Popup popup = new Popup("Exit Setup", "Setup is not complete. If you exit now, the program will not be installed.\n\nYou may run Setup again at another time to complete the installation.\n\nExit Setup?", PopupType.Information, new List<string>() { "Yes", "No" });
        int result = popup.Show();
        Console.WriteLine(result);
        if (result == 0) // Yes
        {
            Program.Window.Dispose();
        }
    }
}
