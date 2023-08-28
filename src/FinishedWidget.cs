using amethyst;
using amethyst.Windows;
using odl;
using System.Diagnostics.CodeAnalysis;

namespace DynamicInstaller.src;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
internal class FinishedWidget : MainWidget
{
    List<CheckBox> OptionBoxes = new List<CheckBox>();

    List<(string ID, string Text, bool IsChecked, Func<bool>? Condition)> Options = new List<(string ID, string Text, bool Checked, Func<bool>? Condition)>()
    {
        ("launch", $"Launch {VersionMetadata.ProgramDisplayName}", true, null),
        ("shortcut", $"Create Desktop shortcut", true, () => Graphics.Platform == odl.Platform.Windows),
        ("startmenu", $"Create Start Menu shortcut", true, () => Graphics.Platform == odl.Platform.Windows),
        ("openfolder", $"Open program folder", false, () => Graphics.Platform == odl.Platform.Linux)
    };

    public FinishedWidget(IContainer parent, StepWidget stepWidget) : base(parent, stepWidget)
    {
        SetBackgroundColor(SystemColors.LightBorderFiller);
        Label infoLabel = new Label(this);
        infoLabel.SetFont(Program.Font);
        infoLabel.SetText($"Click Finish to exit Setup.");
        infoLabel.SetBlendMode(BlendMode.Blend);
        infoLabel.SetHDocked(true);
        infoLabel.SetPadding(40, 20);

        foreach (string fileAssoc in VersionMetadata.ProgramFileAssociations)
        {
            Options.Add((fileAssoc, $"Associate {fileAssoc} files with {VersionMetadata.ProgramDisplayName}", false, () => Graphics.Platform == odl.Platform.Windows));
        }

        // Remove all options that do not meet their conditions
        Options.RemoveAll(o => o.Condition is not null && !o.Condition());

        for (int i = 0; i < Options.Count; i++)
        {
            CheckBox box = new CheckBox(this);
            box.SetPadding(40, 50 + i * 25);
            box.SetFont(Program.Font);
            box.SetText(Options[i].Text);
            box.SetBlendMode(BlendMode.Blend);
            box.SetChecked(Options[i].IsChecked);
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
