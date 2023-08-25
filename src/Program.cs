global using MKUtils;

using amethyst;
using NativeLibraryLoader;
using odl;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DynamicInstaller.src;

public class Program
{
    internal static string? DependencyFolder;
    internal static string ProgramExecutablePath => Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.ProgramInstallPath, VersionMetadata.ProgramLaunchFile).Replace('/', '\\');
    internal static string? ExistingVersion;

    internal static InstallerWindow Window;

    public static void Main(string[] args)
    {
        string appDataFolder = Path.Combine(MKUtils.MKUtils.AppDataFolder, "RPG Studio MK");
        if (!Directory.Exists(appDataFolder)) Directory.CreateDirectory(appDataFolder);
        Logger.Start(Path.Combine(appDataFolder, "updater-log.txt"));

        bool AutomaticUpdate = args.Length == 1 && args[0] == "--automatic-update";
        Logger.WriteLine("Process Path: {0}", Environment.ProcessPath);
        string installerVersion = FileVersionInfo.GetVersionInfo(Environment.ProcessPath).ProductVersion;
        Logger.WriteLine("Dynamic Installer v{0}", MKUtils.MKUtils.TrimTrailingZeroes(installerVersion));
        Logger.WriteLine($"Automatic Update flag is {AutomaticUpdate}");
        try
        {
            if (!VersionMetadata.Load())
            {
                Logger.WriteLine("Metadata download or verification failed.");
                return;
            }

            // Copy this installer to Program Files
            string arg1 = Process.GetCurrentProcess().MainModule!.FileName; // This executable's filename
            string arg2 = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.InstallerInstallPath, VersionMetadata.InstallerInstallFilename).Replace('/', '\\');
            if (File.Exists(arg2) && arg1 != arg2) File.Delete(arg2);
            if (arg1 != arg2)
            {
                Logger.WriteLine($"Copying installer from '{arg1}' to '{arg2}'.");
                File.Copy(arg1, arg2);
            }

            ExistingVersion = GetInstalledVersion();
            // Change text to say update from {old} to {new} version if an old version exists
            if (!ValidateDependencies())
            {
                Logger.WriteLine("Failed to install or validate dependencies.");
                return;
            }
            Logger.WriteLine("Starting Amethyst...");
            Amethyst.Start(GetPathInfo(), false, false);

            Logger.WriteLine("Creating window...");
            Window = new InstallerWindow(800, AutomaticUpdate ? 240 : 480);
            Window.OnClosing += x =>
            {
                if (!Window.ForceClose && !Window.SkipExitPrompt)
                {
                    x.Value = true;
                    QuitWithConfirmation();
                }
            };
            Logger.WriteLine("Initializing main widgets...");
            Window.Setup(AutomaticUpdate);

            Logger.WriteLine("Showing window...");
            Window.Show();

            Window.StartDownloadIfAutomatic();

            Logger.WriteLine("Entering Amethyst UI loop");
            Amethyst.Run();

            Logger.WriteLine("Stopping Amethyst...");
            if (!Window.Disposed) Window.Dispose();
            Amethyst.Stop();
        }
        catch (Exception ex)
        {
            Logger.Error("Unknown error: " + ex.Message + "\n" + ex.StackTrace);
        }
        Logger.Stop();
    }

    private static string? GetInstalledVersion()
    {
        Logger.WriteLine("Locating existing installation...");
        string folder = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.ProgramInstallPath);
        if (!Directory.Exists(folder)) return null;
        string execFile = Path.Combine(folder, VersionMetadata.ProgramLaunchFile);
        if (File.Exists(execFile))
        {
            execFile = execFile.Replace('\\', '/');
            Logger.WriteLine("Found program executable at {0}", execFile);
            string currentProgramVersion = FileVersionInfo.GetVersionInfo(execFile).ProductVersion;
            currentProgramVersion = MKUtils.MKUtils.TrimTrailingZeroes(currentProgramVersion);
            Logger.WriteLine("Found program version {0}", currentProgramVersion);
            return currentProgramVersion;
        }
        Logger.WriteLine("No existing installation found.");
        return null;
    }

    private static bool ValidateDependencies()
    {
        if (!HasAllDependencies())
        {
            if (!InstallDependencies()) return false;
        }
        DependencyFolder = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.CoreLibraryPath, "lib", "windows");
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
        Logger.WriteLine("Detecting whether all graphical dependencies are installed...");
        foreach (string requiredFile in VersionMetadata.RequiredFiles[PlatformString])
        {
            if (!File.Exists(Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.CoreLibraryPath, "lib", PlatformString, requiredFile)))
            {
                Logger.WriteLine("MISSING DEPENDENCY: " + requiredFile);
                return false;
            }
        }
        Logger.WriteLine("All dependencies are installed.");
        return true;
    }

    private static string PlatformString => Graphics.Platform switch
    {
        odl.Platform.Windows => "windows",
        odl.Platform.Linux => "linux",
        _ => "unknown"
    };

    private static bool InstallDependencies()
    {
        Logger.WriteLine("Downloading core dependencies...");
        string tempFile = Path.GetTempFileName();
        if (!Downloader.DownloadFile(VersionMetadata.CoreLibraryDownloadLink[PlatformString], tempFile))
            return false;
        Logger.WriteLine("Initializing core dependencies archive...");
        Archive coreLibFile = new Archive(tempFile);
        string extractURL = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.CoreLibraryPath);
        Logger.WriteLine("Extracting core dependencies...");
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
        Logger.WriteLine($"Setting file association for '{fileAssoc}'");
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
        Logger.WriteLine($"Getting command output for '{command}'...");
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
        Logger.WriteLine("Launching program...");
        Process proc = new Process();
        proc.StartInfo = new ProcessStartInfo(ProgramExecutablePath);
        proc.StartInfo.UseShellExecute = false;
        proc.Start();
    }

    internal static void QuitWithConfirmation()
    {
        Popup popup = new Popup("Exit Setup", "Setup is not complete. If you exit now, the program will not be installed.\n\nYou may run Setup again at another time to complete the installation.\n\nExit Setup?", PopupType.Information, new List<string>() { "Yes", "No" });
        int result = popup.Show();
        if (result == 0) // Yes
        {
            Logger.WriteLine("Closing window...");
            Window.Dispose();
        }
    }
}
