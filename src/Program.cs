global using MKUtils;
using amethyst;
using NativeLibraryLoader;
using odl;

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DynamicInstaller.src;

public class Program
{
    internal static string? DependencyFolder;
    internal static string ProgramLaunchFile => VersionMetadata.ProgramLaunchFile[ODL.Platform switch
    {
        odl.Platform.Windows => "windows",
        odl.Platform.Linux => "linux",
        odl.Platform.MacOS => "macos",
        _ => throw new NotImplementedException(),
    }];
    internal static string ProgramExecutablePath => Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.ProgramInstallPath, ProgramLaunchFile).Replace('/', '\\');
    internal static string InstallerInstallFilename => VersionMetadata.InstallerInstallFilename[ODL.Platform switch
    {
        odl.Platform.Windows => "windows",
        odl.Platform.Linux => "linux",
        odl.Platform.MacOS => "macos",
        _ => throw new NotImplementedException()
    }];
    internal static string? ExistingVersion;

    internal static InstallerWindow Window;
    internal static Font Font;
    internal static Font BoldFont;

    private delegate uint GetEUID();
    private static GetEUID geteuid;

	public static void Main(string[] args)
    {
    	if (ODL.Platform == odl.Platform.Linux)
        {
            NativeLibrary libc = NativeLibrary.Load("libc.so.6");
            geteuid = libc.GetFunction<GetEUID>("geteuid");
            if (!IsLinuxAdmin())
            {
                Console.WriteLine("ERROR: The installer requires administrator permissions. Please re-run the program with 'sudo'.");
                return;
            }
        }

        
        string appDataFolder = Path.Combine(MKUtils.MKUtils.AppDataFolder, ODL.OnLinux ? ".rpg-studio-mk" : "RPG Studio MK");
        MakeFolderAsNonRoot(appDataFolder);

#if DEBUG
        Logger.Start();
#else
        string updaterLogPath = Path.Combine(appDataFolder, "updater-log.txt").Replace('\\', '/');
        Logger.Start(updaterLogPath);
#endif
        ODL.Logger = Logger.Instance;

		Logger.WriteLine("AppDataFolder: {0}", MKUtils.MKUtils.AppDataFolder);

        bool AutomaticUpdate = args.Length == 1 && args[0] == "--automatic-update";
        Logger.WriteLine("Process Path: {0}", Environment.ProcessPath);
        string installerVersion = null;
        if (ODL.OnWindows)
        {
            installerVersion = FileVersionInfo.GetVersionInfo(Environment.ProcessPath)?.ProductVersion ?? "0";
        }
        else if (ODL.OnLinux || ODL.OnMacOS)
        {
            string exeParent = Path.GetDirectoryName(Environment.ProcessPath);
            string versionFile = Path.Combine(exeParent, "VERSION").Replace('\\', '/');
            if (File.Exists(versionFile)) installerVersion = File.ReadAllText(versionFile);
            else installerVersion = "0";
        }
        Logger.WriteLine("Dynamic Installer v{0}", MKUtils.MKUtils.TrimVersion(installerVersion));
        Logger.WriteLine($"Automatic Update flag is {AutomaticUpdate}");
        try
        {
            MKUtils.Logger.Instance = Logger.Instance;
            if (!VersionMetadata.Load())
            {
                Logger.WriteLine("Metadata download or verification failed.");
                return;
            }

            // Copy this installer to Program Files
            string arg1 = Process.GetCurrentProcess().MainModule!.FileName.Replace('\\', '/'); // This executable's filename
            string arg2Parent = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.InstallerInstallPath).Replace('\\', '/');
            string arg2 = Path.Combine(arg2Parent, InstallerInstallFilename).Replace('\\', '/');
            if (File.Exists(arg2) && arg1 != arg2) File.Delete(arg2);
            if (arg1 != arg2)
            {
                Logger.WriteLine($"Copying installer from '{arg1}' to '{arg2}'.");
                if (!Directory.Exists(arg2Parent)) Directory.CreateDirectory(arg2Parent);
                File.Copy(arg1, arg2);
                if (ODL.OnLinux || ODL.OnMacOS)
                {
                    // Write version file
                    // We make a big assumption here; that the installer being run is the latest version of the installer.
                    // There is no way to verify this, though.
                    // The alternative is not writing a version file, but that would mean the editor would be required
                    // to download the installer on initial load, which is rather intrusive.
                    File.WriteAllText(Path.Combine(arg2Parent, "VERSION"), VersionMetadata.InstallerVersion);
                }
            }

            ExistingVersion = GetInstalledVersion();
            if (!ValidateDependencies())
            {
                Logger.WriteLine("Failed to install or validate dependencies.");
                return;
            }
            Logger.WriteLine("Starting Amethyst...");
            Amethyst.Start(GetPathInfo(), false, false);
            string fontName = null;
            string boldFontName = null;
            switch (ODL.Platform)
            {
                case odl.Platform.Windows:
                    fontName = "Arial";
                    boldFontName = "arialbd";
                    break;
                case odl.Platform.Linux:
                    fontName = "Ubuntu-R";
                    boldFontName = "Ubuntu-B";
                    break;
                case odl.Platform.MacOS:
                    fontName = ODL.FontResolver.ResolveFilenames("Georgia", "Verdana", "Comic Sans MS")!;
                    boldFontName = ODL.FontResolver.ResolveFilenames("Georgia Bold", "Verdana Bold", "Comic Sans MS Bold")!;
                    break;
                default:
                    throw new NotImplementedException();
            }
            Program.Font = FontCache.GetOrCreate(fontName, 12);
            Program.BoldFont = FontCache.GetOrCreate(boldFontName, 12);

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

    public static void MakeFolderAsNonRoot(string folder)
    {
        if (Directory.Exists(folder)) return;
        if (ODL.OnLinux && IsLinuxAdmin())
        {
            Process prcs = new Process();
            prcs.StartInfo = new ProcessStartInfo("runuser");
            string user = Environment.GetEnvironmentVariable("SUDO_USER");
            prcs.StartInfo.Arguments = $"-l {user} -c \"mkdir '{folder}'\"";
            prcs.Start();
            prcs.WaitForExit();
        }
        else Directory.CreateDirectory(folder);
	}

	public static bool IsLinuxAdmin()
	{
		return geteuid() == 0;
	}

	private static string? GetInstalledVersion()
    {
        Logger.WriteLine("Locating existing installation...");
        string folder = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.ProgramInstallPath);
        Logger.WriteLine("No existing installation found, program folder does not exist yet at {0}", folder);
        if (!Directory.Exists(folder)) return null;
        if (ODL.OnWindows)
        {
            // Read ProductVersion from the assembly
            string execFile = Path.Combine(folder, ProgramLaunchFile).Replace('\\', '/');
            Logger.WriteLine("Searching for executable at {0}...", execFile);
            if (File.Exists(execFile))
            {
                Logger.WriteLine("Found program executable at {0}", execFile);
                string currentProgramVersion = FileVersionInfo.GetVersionInfo(execFile).ProductVersion;
                currentProgramVersion = MKUtils.MKUtils.TrimVersion(currentProgramVersion);
                Logger.WriteLine("Found program version {0}", currentProgramVersion);
                return currentProgramVersion;
            }
        }
        else if (ODL.OnLinux || ODL.OnMacOS)
        {
            // Linux/macOS cannot read ProductVersion from assemblies, so we use a version file instead
            string versionFile = Path.Combine(folder, "VERSION").Replace('\\', '/');
            Logger.WriteLine("Searching for version file at {0}...", versionFile);
            if (File.Exists(versionFile))
            {
                string currentProgramVersion = File.ReadAllText(versionFile);
                Logger.WriteLine("Found version file at {0}", versionFile);
                if (!string.IsNullOrEmpty(currentProgramVersion))
                    currentProgramVersion = MKUtils.MKUtils.TrimVersion(currentProgramVersion);
                if (string.IsNullOrEmpty(currentProgramVersion)) currentProgramVersion = "0";
                Logger.WriteLine("Found program version {0}", currentProgramVersion);
                return currentProgramVersion;
            }
        }
        else
        {
            throw new NotImplementedException();
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
        DependencyFolder = Path.Combine(MKUtils.MKUtils.ProgramFilesPath, VersionMetadata.CoreLibraryPath, "lib", ODL.Platform switch
        {
            odl.Platform.Windows => "windows",
            odl.Platform.Linux => "linux",
            odl.Platform.MacOS => "macos",
            _ => throw new NotImplementedException()
        }).Replace('\\', '/');
        return true;
    }

    private static PathInfo GetPathInfo()
    {
        PathPlatformInfo windows = new PathPlatformInfo(NativeLibraryLoader.Platform.Windows);
        windows.AddPath("libsdl2", DependencyFolder + "/SDL2.dll");
        windows.AddPath("libz", DependencyFolder + "/zlib1.dll");
        windows.AddPath("libpng", DependencyFolder + "/libpng16-16.dll");
        windows.AddPath("libsdl2_ttf", DependencyFolder + "/SDL2_ttf.dll");
        windows.AddPath("libfreetype", DependencyFolder + "/libfreetype-6.dll");

        PathPlatformInfo linux = new PathPlatformInfo(NativeLibraryLoader.Platform.Linux);
        linux.AddPath("libsdl2", DependencyFolder + "/SDL2.so");
        linux.AddPath("libz", DependencyFolder + "/libz.so");
        linux.AddPath("libpng", DependencyFolder + "/libpng16-16.so");
        linux.AddPath("libsdl2_ttf", DependencyFolder + "/SDL2_ttf.so");
        linux.AddPath("libfreetype", DependencyFolder + "/libfreetype-6.so");

        PathPlatformInfo macos = new PathPlatformInfo(NativeLibraryLoader.Platform.MacOS);
        macos.AddPath("libsdl2", DependencyFolder + "/SDL2.dylib");
        macos.AddPath("libz", DependencyFolder + "/libz.dylib");
        macos.AddPath("libpng", DependencyFolder + "/libpng.dylib");
        macos.AddPath("libsdl2_ttf", DependencyFolder + "/SDL2_ttf.dylib");
        macos.AddPath("libfreetype", DependencyFolder + "/libfreetype.dylib");

        return PathInfo.Create(windows, linux, macos);
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

    private static string PlatformString => ODL.Platform switch
    {
        odl.Platform.Windows => "windows",
        odl.Platform.Linux => "linux",
        odl.Platform.MacOS => "macos",
        _ => throw new NotImplementedException()
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
        if (!ODL.OnWindows) return false;
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
        if (!ODL.OnWindows) throw new PlatformNotSupportedException();
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
        string path = ODL.Platform switch
        {
        	odl.Platform.Windows => ProgramExecutablePath.Replace('/', '\\'),
        	odl.Platform.Linux or odl.Platform.MacOS => ProgramExecutablePath.Replace('\\', '/'),
        	_ => throw new NotImplementedException()
        };
        if (ODL.OnWindows || ODL.OnMacOS)
        {
            Process proc = new Process();
            proc.StartInfo = new ProcessStartInfo(path);
            proc.StartInfo.UseShellExecute = false;
            proc.Start();
        }
        else if (ODL.OnLinux)
        {
			Process prcs = new Process();
			prcs.StartInfo = new ProcessStartInfo("/bin/bash");
            string user = Environment.GetEnvironmentVariable("SUDO_USER")!;
			prcs.StartInfo.Arguments = $"-c \"su {user} -c \\\"'{path}'\\\"\"";
            Logger.WriteLine("Launching the program with `bash {0}", prcs.StartInfo.Arguments + "`.");
			prcs.Start();
			prcs.WaitForExit();
		}
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
