using amethyst;
using amethyst.Windows;
using odl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicInstaller;

internal class HeaderWidget : Widget
{
    Label HeaderLabel;
    Label DescriptionLabel;

    public HeaderWidget(IContainer Parent) : base(Parent)
    {
        SetBackgroundColor(SystemColors.ControlBackground);

        HeaderLabel = new Label(this);
        HeaderLabel.SetFont(Font.Get("arialbd", 12));
        HeaderLabel.SetText($"{Config.ProgramDisplayName} {Config.ProgramVersion} installer");
        HeaderLabel.SetPosition(20, 20);
        HeaderLabel.SetBlendMode(BlendMode.Blend);

        DescriptionLabel = new Label(this);
        DescriptionLabel.SetFont(Font.Get("arial", 12));
        DescriptionLabel.SetText($"Welcome to the installer for {Config.ProgramDisplayName}");
        DescriptionLabel.SetPosition(40, 50);
        DescriptionLabel.SetBlendMode(BlendMode.Blend);

        ImageBox imgBox = new ImageBox(this);
        imgBox.SetBitmap(Logo.Bitmap);
        imgBox.SetRightDocked(true);
        imgBox.SetPadding(0, 16, 16, 0);

        Widget divider = new Widget(this);
        divider.SetHDocked(true);
        divider.SetBottomDocked(true);
        divider.SetPadding(0, 0, 0, 1);
        divider.SetHeight(1);
        divider.SetBackgroundColor(SystemColors.Divider);
    }

    public void SetHeaderText(string headerText)
    {
        HeaderLabel.SetText(headerText);
    }

    public void SetDescriptionText(string descriptionText)
    {
        DescriptionLabel.SetText(descriptionText);
    }
}
