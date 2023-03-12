using amethyst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicInstaller;

internal class MainWidget : Widget
{
    public StepWidget StepWidget { get; protected set; }

    public MainWidget(IContainer parent, StepWidget stepWidget) : base(parent)
    {
        this.StepWidget = stepWidget;
    }
}
