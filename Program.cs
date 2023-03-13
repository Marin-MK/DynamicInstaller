global using MKUtils;

using amethyst;
using NativeLibraryLoader;
using odl;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DynamicInstaller;

public class Program
{
    internal static string? DependencyFolder;
    internal static string ProgramExecutablePath => Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.ProgramInstallPath, VersionMetadata.ProgramLaunchFile).Replace('/', '\\');
    internal static string? ExistingVersion;

    internal static InstallerWindow Window;

    public static void Main(string[] args)
    {
        bool AutomaticUpdate = args.Length == 1 && args[0] == "--automatic-update";
        try
        {
            if (!VersionMetadata.Load().Result)
            {
                Console.WriteLine("Metadata download or verification failed.");
                return;
            }
            //ExistingVersion = GetInstalledVersion();
            // Change text to say update from {old} to {new} version if an old version exists
            if (!ValidateDependencies())
            {
                Console.WriteLine("Failed to install or validate dependencies.");
                return;
            }
            Amethyst.Start(GetPathInfo(), false, false);

            Window = new InstallerWindow(800, AutomaticUpdate ? 240 : 480);
            Window.OnClosing += x =>
            {
                if (!Window.ForceClose && !Window.SkipExitPrompt)
                {
                    x.Value = true;
                    QuitWithConfirmation();
                }
            };
            Window.Setup(AutomaticUpdate);

            Window.Show();

            Amethyst.Run();

            if (!Window.Disposed) Window.Dispose();
            Amethyst.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unknown error: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    private static string? GetInstalledVersion()
    {
        string folder = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.ProgramInstallPath);
        if (!Directory.Exists(folder)) return null;
        string execFile = Path.Combine(folder, VersionMetadata.ProgramLaunchFile);
        string versionFile = Path.Combine(folder, "version.txt");
        if (File.Exists(execFile) && File.Exists(versionFile))
            return File.ReadAllText(versionFile).TrimEnd();
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
        foreach (string requiredFile in VersionMetadata.RequiredFiles[PlatformString])
        {
            if (!File.Exists(Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.CoreLibraryPath, "lib", PlatformString, requiredFile)))
            {
                return false;
            }
        }
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
        string tempFile = Path.GetTempFileName();
        if (!Downloader.DownloadFile(VersionMetadata.CoreLibraryDownloadLink[PlatformString], tempFile))
            return false;
        Archive coreLibFile = new Archive(tempFile);
        string extractURL = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.CoreLibraryPath);
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
        proc.StartInfo.UseShellExecute = false;
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
