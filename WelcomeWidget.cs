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
        if (!string.IsNullOrEmpty(Program.ExistingVersion))
        {
            int cmp = VersionMetadata.CompareVersions(Program.ExistingVersion, VersionMetadata.ProgramVersion);
			if (cmp == -1) // Existing version is older than latest
                text = $"This will upgrade {VersionMetadata.ProgramDisplayName} from {Program.ExistingVersion} to {VersionMetadata.ProgramVersion} for all users." +
					"\n\nIt is recommended that you close all other applications before continuing.\n\nClick Next to continue, or Cancel to exit Setup.";
            else
            {
                if (cmp == 0) text = $"{VersionMetadata.ProgramDisplayName} is already up-to-date. Installed version {Program.ExistingVersion} is the latest version.";
                else text = $"{VersionMetadata.ProgramDisplayName} is ahead of the latest version. Installed version {Program.ExistingVersion} is newer than {VersionMetadata.ProgramVersion}.";
                stepWidget.SetUnable(true);
				Program.Window.ForceClose = true;
				text += "\n\nClick Cancel to exit Setup.";
			}
        }
        else text = $"This will install {VersionMetadata.ProgramDisplayName} {VersionMetadata.ProgramVersion} on your computer for all users." +
				"\n\nIt is recommended that you close all other applications before continuing.\n\nClick Next to continue, or Cancel to exit Setup.";
        headerLabel.SetText(text);
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
