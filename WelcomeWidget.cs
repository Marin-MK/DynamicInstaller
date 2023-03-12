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
        headerLabel.SetText($"This will install {Config.ProgramDisplayName} {Config.ProgramVersion} on your computer for all users.\n\nIt is recommended that you close all other applications before continuing.\n\nClick Next to continue, or Cancel to exit Setup.");
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
