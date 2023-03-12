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

    public InstallerWindow() : base(false, false)
    {
        Initialize();
        SetSize(800, 480);
        SetMinimumSize(650, 400);
        InitializeUI();
        Windows.Add(this);
        OnClosed += _ => Windows.Remove(this);
        SetText($"Setup - {Config.ProgramDisplayName}");
        Logo.Initialize();
        SetIcon(Logo.Bitmap);
        SetBackgroundColor(SystemColors.WindowBackground);
    }

    public void Setup()
    {
        mainGrid = new Grid(UI);
        mainGrid.SetRows(
            new GridSize(96, Unit.Pixels),
            new GridSize(1),
            new GridSize(64, Unit.Pixels)
        );
        headerWidget = new HeaderWidget(mainGrid);
        headerWidget.SetGridRow(0);
        stepWidget = new StepWidget(mainGrid);
        stepWidget.SetGridRow(2);
        stepWidget.SetBackStatus(false);
        mainWidget = new WelcomeWidget(mainGrid, stepWidget);
        mainWidget.SetGridRow(1);
        stepWidget.LinkWidget(mainWidget);
    }

    public void SetMainWidget<T>() where T : MainWidget
    {
        mainWidget.Dispose();
        mainWidget = (MainWidget) Activator.CreateInstance(typeof(T), new object[] { mainGrid, stepWidget })!;
        mainWidget.SetGridRow(1);
        stepWidget.LinkWidget(mainWidget);
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
