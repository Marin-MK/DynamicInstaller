using amethyst;
using amethyst.Windows;
using odl;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicInstaller.src;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
internal class EULAWidget : MainWidget
{
    public EULAWidget(IContainer parent, StepWidget stepWidget) : base(parent, stepWidget)
    {
        SetBackgroundColor(SystemColors.LightBorderFiller);
        MultilineLabel infoLabel = new MultilineLabel(this);
        infoLabel.SetFont(Font.Get("Arial", 12));
        infoLabel.SetText("Please read the following License Agreement. You must accept the terms of this agreement before continuing with the installation.");
        infoLabel.SetBlendMode(BlendMode.Blend);
        infoLabel.SetHDocked(true);
        infoLabel.SetPadding(40, 20);
        infoLabel.SetLineHeight(22);

        MultilineReadOnlyTextBox Box = new MultilineReadOnlyTextBox(this);
        Box.SetDocked(true);
        Box.SetPadding(40, 80);
        Box.SetText(VersionMetadata.ProgramEULAText.Replace("\r", ""));
        Box.SetLineHeight(22);

        RadioBox acceptBox = new RadioBox(this);
        acceptBox.SetFont(Font.Get("Arial", 12));
        acceptBox.SetText("I accept the agreement");
        acceptBox.SetBottomDocked(true);
        acceptBox.SetPadding(40, 0, 0, 48);
        acceptBox.SetBlendMode(BlendMode.Blend);
        acceptBox.MinimumSize.Height = 17;
        acceptBox.OnCheckChanged += _ =>
        {
            stepWidget.SetNextStatus(acceptBox.Checked);
        };
        RadioBox rejectBox = new RadioBox(this);
        rejectBox.SetFont(Font.Get("Arial", 12));
        rejectBox.SetText("I do not accept the agreement");
        rejectBox.SetChecked(true);
        rejectBox.SetBottomDocked(true);
        rejectBox.SetPadding(40, 0, 0, 24);
        rejectBox.SetBlendMode(BlendMode.Blend);
        rejectBox.SetHeight(15);
        rejectBox.MinimumSize.Height = 17;

        Widget divider = new Widget(this);
        divider.SetBottomDocked(true);
        divider.SetHDocked(true);
        divider.SetPadding(0, 0, 0, 1);
        divider.SetBackgroundColor(SystemColors.Divider);
        divider.SetHeight(1);
    }
}
