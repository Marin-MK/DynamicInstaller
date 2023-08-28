using amethyst;
using amethyst.Windows;
using odl;

namespace DynamicInstaller.src;

internal class MultilineReadOnlyTextBox : Widget
{
    public string Text => Label.Text;
    public Font Font => Label.Font;
    public DrawOptions DrawOptions => Label.DrawOptions;
    public int? LineHeight => Label.LineHeight;

    MultilineLabel Label;

    public MultilineReadOnlyTextBox(IContainer parent) : base(parent)
    {
        Sprites["box"] = new Sprite(Viewport);

        Container scrollContainer = new Container(this);
        scrollContainer.SetDocked(true);
        scrollContainer.SetPadding(5, 5, 30, 5);

        Label = new MultilineLabel(scrollContainer);
        Label.SetFont(Program.Font);
        Label.SetHDocked(true);

        VScrollBar vs = new VScrollBar(this);
        vs.SetVDocked(true);
        vs.SetRightDocked(true);
        vs.SetPadding(0, 2, 6, 2);
        scrollContainer.SetVScrollBar(vs);
        scrollContainer.VAutoScroll = true;
    }

    public void SetText(string text)
    {
        Label.SetText(text);
    }

    public void SetFont(Font font)
    {
        Label.SetFont(font);
    }

    public void SetDrawOptions(DrawOptions drawOptions)
    {
        Label.SetDrawOptions(drawOptions);
    }

    public void SetLineHeight(int? lineHeight)
    {
        Label.SetLineHeight(lineHeight);
    }

    public override void SizeChanged(BaseEventArgs e)
    {
        base.SizeChanged(e);
        Sprites["box"].Bitmap?.Dispose();
        if (Size.Width < 6 || Size.Height < 6) return;
        Sprites["box"].Bitmap = new Bitmap(Size);
        Sprites["box"].Bitmap.Unlock();
        Sprites["box"].Bitmap.DrawLine(0, 0, Size.Width - 2, 0, SystemColors.Divider);
        Sprites["box"].Bitmap.DrawLine(0, 1, 0, Size.Height - 2, SystemColors.Divider);
        Sprites["box"].Bitmap.DrawLine(0, Size.Height - 1, Size.Width - 1, Size.Height - 1, SystemColors.ControlBackground);
        Sprites["box"].Bitmap.DrawLine(Size.Width - 1, 1, Size.Width - 1, Size.Height - 2, SystemColors.ControlBackground);

        Sprites["box"].Bitmap.DrawLine(1, 1, Size.Width - 3, 1, SystemColors.DarkDivider);
        Sprites["box"].Bitmap.DrawLine(1, 2, 1, Size.Height - 3, SystemColors.DarkDivider);
        Sprites["box"].Bitmap.DrawLine(1, Size.Height - 2, Size.Width - 2, Size.Height - 2, SystemColors.DarkDividerShadow);
        Sprites["box"].Bitmap.DrawLine(Size.Width - 2, 2, Size.Width - 2, Size.Height - 3, SystemColors.DarkDividerShadow);
        Sprites["box"].Bitmap.FillRect(2, 2, Size.Width - 29, Size.Height - 4, SystemColors.ControlBackground);
        Sprites["box"].Bitmap.FillRect(Size.Width - 27, 2, 25, Size.Height - 4, SystemColors.LightBorderFiller);
        Sprites["box"].Bitmap.Lock();
    }
}
