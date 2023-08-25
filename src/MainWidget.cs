using amethyst;

namespace DynamicInstaller.src;

internal class MainWidget : Widget
{
    public StepWidget StepWidget { get; protected set; }

    public MainWidget(IContainer parent, StepWidget stepWidget) : base(parent)
    {
        StepWidget = stepWidget;
    }
}
