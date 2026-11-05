using System;
using System.Drawing;
using System.Windows.Forms;

namespace Winbox.Guest.Overlay;

/// <summary>A borderless, click-through, never-focused window that shows one translucent image.</summary>
internal sealed class SpriteWindow : Form
{
    private bool _isBlank;

    public SpriteWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= LayeredWindow.ExtendedStyles;
            return parameters;
        }
    }

    public void Present(Bitmap image, System.Drawing.Point topLeft)
    {
        LayeredWindow.Present(Handle, image, topLeft);
        _isBlank = false;
    }

    /// <summary>Paints a single transparent pixel once, so an idle sprite costs nothing per frame.</summary>
    public void Blank()
    {
        if (_isBlank) return;
        using var empty = new Bitmap(1, 1);
        LayeredWindow.Present(Handle, empty, System.Drawing.Point.Empty);
        _isBlank = true;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        LayeredWindow.HideFromCapture(Handle);
    }
}
