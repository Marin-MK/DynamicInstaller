using amethyst;
using amethyst.Windows;
using odl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynamicInstaller;

internal class StepWidget : Widget
{
    public MainWidget? LinkedWidget { get; protected set; }

    Button cancelButton;
    Button nextButton;
    Button backButton;
    bool AutomaticUpdateMode = false;

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
        nextButton.SetPadding(0, 0, 16+110, 16);
        nextButton.SetSize(100, 30);
        nextButton.OnPressed += _ => ClickedNext();
        backButton = new Button(this);
        backButton.SetFont(Font.Get("Arial", 12));
        backButton.SetText("< Back");
        backButton.SetBlendMode(BlendMode.Blend);
        backButton.SetBottomDocked(true);
        backButton.SetRightDocked(true);
        backButton.SetPadding(0, 0, 16+110*2, 16);
        backButton.SetSize(100, 30);
        backButton.OnPressed += _ => ClickedBack();
    }

    public void LinkWidget(MainWidget linkedWidget)
    {
        this.LinkedWidget = linkedWidget;
    }

    public void SetBackStatus(bool show)
    {
        backButton.SetEnabled(show);
    }

    public void SetNextStatus(bool show)
    {
        nextButton.SetEnabled(show);
    }

    private void ClickedCancel()
    {
        if (LinkedWidget is FinishedWidget)
        {
            List<string> options = Program.Window.GetFinishOptions();
            Program.Window.ForceClose = true;
            Program.Window.Close();
            List<string> fileAssociations = options.FindAll(x => x[0] == '.').ToList();
            Program.SetFileAssociations(fileAssociations);
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
