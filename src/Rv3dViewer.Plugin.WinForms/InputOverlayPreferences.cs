using System.Text.Json;

namespace Rv3dViewer.Plugin.WinForms;

public sealed record InputOverlayStyle(Color TextColor, float FontSize)
{
    public static InputOverlayStyle Default { get; } = new(Color.White, (SystemFonts.MessageBoxFont ?? Control.DefaultFont).Size);
}

public static class InputOverlayPreferences
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Rv3dViewer", "input-overlay.json");
    private static InputOverlayStyle _current = Load();

    public static InputOverlayStyle Current => _current;
    public static event EventHandler? Changed;

    public static bool Edit(IWin32Window? owner)
    {
        using var dialog = new InputOverlaySettingsForm(
            _current with { TextColor = ViewportColorPreferences.Get("OverlayText") });
        if (dialog.ShowDialog(owner) != DialogResult.OK) return false;
        _current = dialog.Style;
        Save(_current);
        ViewportColorPreferences.Set("OverlayText", _current.TextColor);
        Changed?.Invoke(null, EventArgs.Empty);
        return true;
    }

    private static InputOverlayStyle Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return InputOverlayStyle.Default;
            var document = JsonSerializer.Deserialize<SettingsDocument>(File.ReadAllText(SettingsPath));
            if (document is null) return InputOverlayStyle.Default;
            return new InputOverlayStyle(Color.FromArgb(document.TextColorArgb),
                Math.Clamp(document.FontSize, 6f, 48f));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return InputOverlayStyle.Default;
        }
    }

    private static void Save(InputOverlayStyle style)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new SettingsDocument
            {
                TextColorArgb = style.TextColor.ToArgb(),
                FontSize = style.FontSize
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The visual preference remains active for this session even when
            // the user profile is temporarily read-only.
        }
    }

    private sealed class SettingsDocument
    {
        public int TextColorArgb { get; set; } = Color.White.ToArgb();
        public float FontSize { get; set; } = (SystemFonts.MessageBoxFont ?? Control.DefaultFont).Size;
    }
}

public sealed class InputActivityOverlay : IDisposable
{
    private readonly Control _owner;
    private readonly System.Windows.Forms.Timer _timer;
    private string _text = string.Empty;
    private DateTime _expiresAtUtc;

    public InputActivityOverlay(Control owner)
    {
        _owner = owner;
        _timer = new System.Windows.Forms.Timer { Interval = 50 };
        _timer.Tick += Timer_Tick;
        InputOverlayPreferences.Changed += Preferences_Changed;
        ViewportColorPreferences.Changed += Preferences_Changed;
    }

    public bool Enabled { get; set; }

    public void Show(string text)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(text)) return;
        _text = text;
        _expiresAtUtc = DateTime.UtcNow.AddSeconds(1.5);
        _timer.Start();
        _owner.Invalidate();
    }

    public void Draw(Graphics graphics, Rectangle clientRectangle)
    {
        if (!Enabled || string.IsNullOrEmpty(_text) || DateTime.UtcNow >= _expiresAtUtc) return;
        var style = InputOverlayPreferences.Current;
        var baseFont = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
        using var font = new Font(baseFont.FontFamily, style.FontSize, baseFont.Style, GraphicsUnit.Point);
        var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
        var size = TextRenderer.MeasureText(_text, font, Size.Empty, flags);
        var rectangle = new Rectangle(12, Math.Max(12, clientRectangle.Height - size.Height - 24),
            size.Width + 20, size.Height + 12);
        using var background = new SolidBrush(ViewportColorPreferences.Get("OverlayBackground"));
        graphics.FillRectangle(background, rectangle);
        TextRenderer.DrawText(graphics, _text, font, new Point(rectangle.Left + 10, rectangle.Top + 6),
            ViewportColorPreferences.Get("OverlayText"), flags);
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _owner.Invalidate();
        if (DateTime.UtcNow < _expiresAtUtc) return;
        _timer.Stop();
        _text = string.Empty;
    }

    private void Preferences_Changed(object? sender, EventArgs e) => _owner.Invalidate();

    public void Dispose()
    {
        InputOverlayPreferences.Changed -= Preferences_Changed;
        ViewportColorPreferences.Changed -= Preferences_Changed;
        _timer.Dispose();
    }
}

public static class InputActivityFormatter
{
    public static string Key(Keys keyData)
    {
        var parts = new List<string>(4);
        if (keyData.HasFlag(Keys.Control)) parts.Add("Ctrl");
        if (keyData.HasFlag(Keys.Shift)) parts.Add("Shift");
        if (keyData.HasFlag(Keys.Alt)) parts.Add("Alt");
        var keyCode = keyData & Keys.KeyCode;
        if (keyCode is not (Keys.ControlKey or Keys.ShiftKey or Keys.Menu or Keys.None))
            parts.Add(keyCode switch { Keys.Escape => "Esc", Keys.Space => "Space", Keys.Return => "Enter", _ => keyCode.ToString() });
        return string.Join(" + ", parts);
    }

    public static string Mouse(string action, MouseButtons button)
    {
        var parts = Modifiers();
        var buttonName = button switch
        {
            MouseButtons.Left => "Left",
            MouseButtons.Middle => "Middle",
            MouseButtons.Right => "Right",
            MouseButtons.XButton1 => "X1",
            MouseButtons.XButton2 => "X2",
            _ => string.Empty
        };
        parts.Add(string.IsNullOrEmpty(buttonName) ? action : $"{action} ({buttonName})");
        return string.Join(" + ", parts);
    }

    public static bool ShouldShowMove(MouseButtons buttons) =>
        buttons != MouseButtons.None || Control.ModifierKeys != Keys.None;

    private static List<string> Modifiers()
    {
        var result = new List<string>(3);
        var modifiers = Control.ModifierKeys;
        if (modifiers.HasFlag(Keys.Control)) result.Add("Ctrl");
        if (modifiers.HasFlag(Keys.Shift)) result.Add("Shift");
        if (modifiers.HasFlag(Keys.Alt)) result.Add("Alt");
        return result;
    }
}

public sealed class InputOverlayPictureBox : PictureBox
{
    private readonly InputActivityOverlay _inputOverlay;

    public InputOverlayPictureBox() => _inputOverlay = new InputActivityOverlay(this);

    public bool InputOverlayEnabled { get => _inputOverlay.Enabled; set => _inputOverlay.Enabled = value; }
    public void ShowInputActivity(string text) => _inputOverlay.Show(text);

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _inputOverlay.Show(InputActivityFormatter.Mouse("MouseDn", e.Button));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (InputActivityFormatter.ShouldShowMove(e.Button))
            _inputOverlay.Show(InputActivityFormatter.Mouse(
                e.Button == MouseButtons.None ? "Mouse Move" : "Mouse Drag", e.Button));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _inputOverlay.Show(InputActivityFormatter.Mouse("MouseUp", e.Button));
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        _inputOverlay.Show(InputActivityFormatter.Mouse(
            e.Delta >= 0 ? "Mouse Wheel Up" : "Mouse Wheel Down", MouseButtons.None));
    }

    protected override void OnPaint(PaintEventArgs pe)
    {
        base.OnPaint(pe);
        _inputOverlay.Draw(pe.Graphics, ClientRectangle);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _inputOverlay.Dispose();
        base.Dispose(disposing);
    }
}
