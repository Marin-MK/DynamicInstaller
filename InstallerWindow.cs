using amethyst;
using amethyst.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicInstaller;

internal class InstallerWindow : UIWindow
{
    internal bool ForceClose = false;

    Grid mainGrid;
    HeaderWidget headerWidget;
    MainWidget mainWidget;
    StepWidget stepWidget;

    public InstallerWindow(int width, int height) : base(false, false)
    {
        Initialize();
        SetSize(width, height);
        SetResizable(false);
        InitializeUI();
        Windows.Add(this);
        OnClosed += _ => Windows.Remove(this);
        SetText($"Setup - {VersionMetadata.ProgramDisplayName}");
        Logo.Initialize();
        SetIcon(Logo.Bitmap);
        SetBackgroundColor(SystemColors.WindowBackground);
    }

    public void Setup(bool automaticUpdate)
    {
        mainGrid = new Grid(UI);
        mainGrid.SetRows(
            new GridSize(96, Unit.Pixels),
            new GridSize(1),
            automaticUpdate ? new GridSize(0) : new GridSize(64, Unit.Pixels)
        );
        headerWidget = new HeaderWidget(mainGrid, automaticUpdate);
        headerWidget.SetGridRow(0);
        stepWidget = new StepWidget(mainGrid);
        stepWidget.SetGridRow(2);
        if (automaticUpdate) mainWidget = new InstallationWidget(mainGrid, stepWidget);
        else mainWidget = new WelcomeWidget(mainGrid, stepWidget);
        mainWidget.SetGridRow(1);
        stepWidget.LinkWidget(mainWidget);
        if (automaticUpdate)
        {
            stepWidget.SetAutomaticUpdateMode();
        }
        else stepWidget.SetBackStatus(false);
    }

    public void StartDownloadIfAutomatic()
    {
        if (mainWidget is InstallationWidget) ((InstallationWidget) mainWidget).Download();
    }

    public void SetMainWidget<T>() where T : MainWidget
    {
        mainWidget.Dispose();
        mainWidget = (MainWidget) Activator.CreateInstance(typeof(T), new object[] { mainGrid, stepWidget })!;
        mainWidget.SetGridRow(1);
        stepWidget.LinkWidget(mainWidget);
        if (mainWidget is InstallationWidget) ((InstallationWidget) mainWidget).Download();
    }

    public void SetHeaderText(string text)
    {
        headerWidget.SetHeaderText(text);
    }

    public void SetDescriptionText(string text)
    {
        headerWidget.SetDescriptionText(text);
    }

    public void MarkInstallationComplete()
    {
        stepWidget.MarkInstallationComplete();
    }

    public List<string> GetFinishOptions()
    {
        return ((FinishedWidget) mainWidget).GetFinishOptions();
    }

    public bool SkipExitPrompt => mainWidget is FinishedWidget;
}
