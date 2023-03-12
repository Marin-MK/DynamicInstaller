using amethyst;
using amethyst.Windows;
using odl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicInstaller;

internal class FinishedWidget : MainWidget
{
    List<CheckBox> OptionBoxes = new List<CheckBox>();

    List<(string ID, string Text)> Options = new List<(string ID, string Text)>()
    {
        ("launch", $"Launch {Config.ProgramDisplayName}"),
    };

    public FinishedWidget(IContainer parent, StepWidget stepWidget) : base(parent, stepWidget)
    {
        SetBackgroundColor(SystemColors.LightBorderFiller);
        Label infoLabel = new Label(this);
        infoLabel.SetFont(Font.Get("Arial", 12));
        infoLabel.SetText($"Click Finish to exit Setup.");
        infoLabel.SetBlendMode(BlendMode.Blend);
        infoLabel.SetHDocked(true);
        infoLabel.SetPadding(40, 20);

        foreach (string fileAssoc in Config.ProgramFileAssociations)
        {
            Options.Add((fileAssoc, $"Associate {fileAssoc} files with {Config.ProgramDisplayName}"));
        }

        for (int i = 0; i < Options.Count; i++)
        {
            CheckBox box = new CheckBox(this);
            box.SetPadding(40, 50 + i * 22);
            box.SetFont(Font.Get("Arial", 12));
            box.SetText(Options[i].Text);
            box.SetBlendMode(BlendMode.Blend);
            box.MinimumSize.Height = 20;
            OptionBoxes.Add(box);
        }

        Widget divider = new Widget(this);
        divider.SetBottomDocked(true);
        divider.SetHDocked(true);
        divider.SetPadding(0, 0, 0, 1);
        divider.SetBackgroundColor(SystemColors.Divider);
        divider.SetHeight(1);
    }

    public List<string> GetFinishOptions()
    {
        List<string> optionIDs = new List<string>();
        for (int i = 0; i < OptionBoxes.Count; i++)
        {
            if (OptionBoxes[i].Checked)
            {
                optionIDs.Add(Options[i].ID);
            }
        }
        return optionIDs;
    }
}
