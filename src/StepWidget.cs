using amethyst;
using amethyst.Windows;
using odl;
using System.Diagnostics;

namespace DynamicInstaller.src;

internal class StepWidget : Widget
{
    public MainWidget? LinkedWidget { get; protected set; }

    Button cancelButton;
    Button nextButton;
    Button backButton;
    bool AutomaticUpdateMode = false;
    bool Unable = false;

    public StepWidget(IContainer parent) : base(parent)
    {
        SetBackgroundColor(SystemColors.LightBorderFiller);
        cancelButton = new Button(this);
        cancelButton.SetFont(Font.Get("Arial", 12));
        cancelButton.SetText("Cancel");
        cancelButton.SetBlendMode(BlendMode.Blend);
        cancelButton.SetBottomDocked(true);
        cancelButton.SetRightDocked(true);
        cancelButton.SetPadding(0, 0, 16, 16);
        cancelButton.SetSize(100, 30);
        cancelButton.OnPressed += _ => ClickedCancel();
        nextButton = new Button(this);
        nextButton.SetFont(Font.Get("Arial", 12));
        nextButton.SetText("Next >");
        nextButton.SetBlendMode(BlendMode.Blend);
        nextButton.SetBottomDocked(true);
        nextButton.SetRightDocked(true);
        nextButton.SetPadding(0, 0, 16 + 110, 16);
        nextButton.SetSize(100, 30);
        nextButton.OnPressed += _ => ClickedNext();
        backButton = new Button(this);
        backButton.SetFont(Font.Get("Arial", 12));
        backButton.SetText("< Back");
        backButton.SetBlendMode(BlendMode.Blend);
        backButton.SetBottomDocked(true);
        backButton.SetRightDocked(true);
        backButton.SetPadding(0, 0, 16 + 110 * 2, 16);
        backButton.SetSize(100, 30);
        backButton.OnPressed += _ => ClickedBack();
    }

    public void LinkWidget(MainWidget linkedWidget)
    {
        LinkedWidget = linkedWidget;
    }

    public void SetBackStatus(bool show)
    {
        backButton.SetEnabled(show);
    }

    public void SetNextStatus(bool show)
    {
        nextButton.SetEnabled(show);
    }

    public void SetUnable(bool Unable)
    {
        this.Unable = Unable;
        backButton.SetVisible(false);
        nextButton.SetVisible(false);
    }

    private void ClickedCancel()
    {
        if (Unable)
        {
            Program.Window.Close();
        }
        else if (LinkedWidget is FinishedWidget)
        {
            List<string> options = Program.Window.GetFinishOptions();
            Program.Window.ForceClose = true;
            Program.Window.Close();
            List<string> fileAssociations = options.FindAll(x => x[0] == '.').ToList();
            Program.SetFileAssociations(fileAssociations);
            if (options.Contains("shortcut"))
            {
                string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = deskDir + "\\" + VersionMetadata.ProgramDisplayName + ".url";
                CreateShortcut(shortcutPath);
            }
            if (options.Contains("startmenu") && Graphics.Platform == Platform.Windows)
            {
                string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                string shortcutPath = Path.Combine(startMenuPath, "Programs", VersionMetadata.ProgramDisplayName + ".lnk").Replace('/', '\\');
                string app = Program.ProgramExecutablePath.Replace('/', '\\');
                Logger.WriteLine("Creating start menu shortcut at {0} pointing to {1}", shortcutPath, app);
                Process p = new Process();
                p.StartInfo = new ProcessStartInfo();
                p.StartInfo.FileName = @"powershell.exe";
                p.StartInfo.Arguments = $"\"$s=(New-Object -COM WScript.Shell).CreateShortcut(\\\"{shortcutPath}\\\"); $s.TargetPath=\\\"{app}\\\"; $s.Save(); echo 'Success';\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
            }
            if (options.Contains("launch"))
            {
                Program.RunExecutable();
            }
        }
        else if (!Program.Window.ForceClose && !Program.Window.SkipExitPrompt)
        {
            Program.QuitWithConfirmation();
        }
        else
        {
            Program.Window.ForceClose = true;
            Program.Window.Close();
        }
    }

    private void CreateShortcut(string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            string app = Program.ProgramExecutablePath.Replace('\\', '/');
            Logger.WriteLine("Creating shortcut at {0} pointing to {1}", path, app);
            writer.WriteLine("[InternetShortcut]");
            writer.WriteLine("URL=file:///" + app);
            writer.WriteLine("IconIndex=0");
            writer.WriteLine("IconFile=" + app);
        }
    }

    private void ClickedNext()
    {
        if (LinkedWidget is WelcomeWidget)
        {
            Program.Window.SetMainWidget<EULAWidget>();
            SetNextStatus(false);
            nextButton.SetText("Install >");
            SetBackStatus(true);
        }
        else if (LinkedWidget is EULAWidget)
        {
            Program.Window.SetHeaderText("Installing");
            Program.Window.SetDescriptionText($"Please wait while Setup installs {VersionMetadata.ProgramDisplayName} on your computer.");
            backButton.Dispose();
            nextButton.Dispose();
            Graphics.Update();
            Program.Window.SetMainWidget<InstallationWidget>();
        }
        else if (LinkedWidget is InstallationWidget)
        {
            Program.Window.SetMainWidget<FinishedWidget>();
            Program.Window.SetHeaderText($"Completing the {VersionMetadata.ProgramDisplayName} Setup Wizard");
            Program.Window.SetDescriptionText($"Setup has finished installing {VersionMetadata.ProgramDisplayName} on your computer.");
            cancelButton.SetText("Finish");
        }
    }

    public void SetAutomaticUpdateMode()
    {
        AutomaticUpdateMode = true;
        backButton.Dispose();
        nextButton.Dispose();
        cancelButton.Dispose();
    }

    private void ClickedBack()
    {
        if (LinkedWidget is EULAWidget)
        {
            Program.Window.SetMainWidget<WelcomeWidget>();
            SetBackStatus(false);
            nextButton.SetText("Next >");
        }
        SetNextStatus(true);
    }

    public void MarkInstallationComplete()
    {
        if (AutomaticUpdateMode)
        {
            Program.Window.ForceClose = true;
            Program.Window.Close();
            Program.RunExecutable();
        }
        else ClickedNext();
    }
}
