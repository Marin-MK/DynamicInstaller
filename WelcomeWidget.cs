using amethyst;
using amethyst.Windows;
using odl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicInstaller;

internal class WelcomeWidget : MainWidget
{
    public WelcomeWidget(IContainer parent, StepWidget stepWidget) : base(parent, stepWidget)
    {
        SetBackgroundColor(SystemColors.LightBorderFiller);
        MultilineLabel headerLabel = new MultilineLabel(this);
        headerLabel.SetFont(Font.Get("Arial", 12));
        string text = "";
        if (!string.IsNullOrEmpty(Program.ExistingVersion) && VersionMetadata.CompareVersions(Program.ExistingVersion, VersionMetadata.ProgramVersion) == -1)
            text = $"This will upgrade {VersionMetadata.ProgramDisplayName} from {Program.ExistingVersion} to {VersionMetadata.ProgramVersion} for all users.";
        else text = $"This will install {VersionMetadata.ProgramDisplayName} {VersionMetadata.ProgramVersion} on your computer for all users.";
        headerLabel.SetText(text + "\n\nIt is recommended that you close all other applications before continuing.\n\nClick Next to continue, or Cancel to exit Setup.");
        headerLabel.SetBlendMode(BlendMode.Blend);
        headerLabel.SetHDocked(true);
        headerLabel.SetPadding(40, 20);
        headerLabel.SetLineHeight(26);

        Widget divider = new Widget(this);
        divider.SetBottomDocked(true);
        divider.SetHDocked(true);
        divider.SetPadding(0, 0, 0, 1);
        divider.SetBackgroundColor(SystemColors.Divider);
        divider.SetHeight(1);
    }
}
