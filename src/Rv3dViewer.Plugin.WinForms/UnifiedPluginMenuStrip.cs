using System.Diagnostics;

namespace Rv3dViewer.Plugin.WinForms;

public enum PluginModelDisplayMode { Points, Wireframe, Solid }

public sealed record PluginMenuCommand(string Text, EventHandler Handler, Func<bool>? CanExecute = null);

public sealed record PluginDisplayToggleCommand(string Text, bool Checked, Action<bool> Changed);

public sealed class PluginDisplayChangedEventArgs : EventArgs
{
    public bool ShowCamera { get; init; }
    public bool ShowLights { get; init; }
    public bool ShowTextures { get; init; }
    public PluginModelDisplayMode ModelMode { get; init; }
}

public sealed class UnifiedPluginMenuStrip : MenuStrip
{
    private readonly ToolStripMenuItem _readItem = new("讀取");
    private readonly ToolStripMenuItem _saveItem = new("儲存");
    private readonly ToolStripMenuItem _undoItem = new("Undo");
    private readonly ToolStripMenuItem _redoItem = new("Redo");
    private readonly ToolStripMenuItem _editItem = new("編輯");
    private readonly ToolStripMenuItem _cameraItem = new("Camera") { CheckOnClick = true, Checked = true };
    private readonly ToolStripMenuItem _lightsItem = new("燈光") { CheckOnClick = true, Checked = true };
    private readonly ToolStripMenuItem _texturesItem = new("貼圖") { CheckOnClick = true, Checked = true };
    private readonly ToolStripMenuItem _pointsItem = new("點雲") { CheckOnClick = true };
    private readonly ToolStripMenuItem _wireframeItem = new("線框") { CheckOnClick = true };
    private readonly ToolStripMenuItem _solidItem = new("實體") { CheckOnClick = true, Checked = true };
    private readonly ToolStripMenuItem _modelItem = new("模型");
    private readonly ToolStripMenuItem _displayItem = new("顯示");
    private readonly ToolStripMenuItem _inputOverlayItem = new("滑鼠鍵盤資訊") { CheckOnClick = true };
    private readonly ToolStripMenuItem _inputOverlaySettingsItem = new("Overlay 設定…");
    private readonly ToolStripMenuItem _viewportColorSettingsItem = new("ViewPort 顏色設定…");
    private readonly ToolStripMenuItem _tutorialItem = new("教學");
    private string _pluginName = "PlugIn";
    private string _pluginDescription = "Rv3d Viewer PlugIn";
    private string? _pluginDirectory;
    private bool _updating;
    private Form? _keyboardForm;
    private readonly List<ToolStripItem> _additionalDisplayItems = [];
    private ToolStripMenuItem? _inputItem;

    public UnifiedPluginMenuStrip()
    {
        RichColorEditor.Register();
        var file = new ToolStripMenuItem("檔案");
        file.DropDownItems.AddRange([_readItem, _saveItem]);
        _editItem.DropDownItems.AddRange([_undoItem, _redoItem]);
        _modelItem.DropDownItems.AddRange([_pointsItem, _wireframeItem, _solidItem]);
        _displayItem.DropDownItems.AddRange([_cameraItem, _lightsItem, _texturesItem, _modelItem,
            new ToolStripSeparator(), _inputOverlayItem, _inputOverlaySettingsItem, _viewportColorSettingsItem]);
        var help = new ToolStripMenuItem("說明");
        var about = new ToolStripMenuItem("關於");
        help.DropDownItems.AddRange([about, _tutorialItem]);
        Items.AddRange([file, _editItem, _displayItem, help]);
        file.DropDownOpening += (_, _) => RefreshCommandStates();
        _editItem.DropDownOpening += (_, _) => RefreshCommandStates();

        _readItem.Click += (_, _) => { if (!_readItem.HasDropDownItems) ReadRequested?.Invoke(this, EventArgs.Empty); };
        _saveItem.Click += (_, _) => { if (!_saveItem.HasDropDownItems) SaveRequested?.Invoke(this, EventArgs.Empty); };
        _undoItem.Click += (_, _) => UndoRequested?.Invoke(this, EventArgs.Empty);
        _redoItem.Click += (_, _) => RedoRequested?.Invoke(this, EventArgs.Empty);
        about.Click += (_, _) => new PluginAboutForm(_pluginName, _pluginDescription).ShowDialog(FindForm());
        _tutorialItem.DropDownOpening += (_, _) => RebuildTutorialMenu();
        foreach (var item in new[] { _cameraItem, _lightsItem, _texturesItem })
            item.CheckedChanged += (_, _) => RaiseDisplayChanged();
        _pointsItem.Click += (_, _) => SetModelMode(PluginModelDisplayMode.Points, true);
        _wireframeItem.Click += (_, _) => SetModelMode(PluginModelDisplayMode.Wireframe, true);
        _solidItem.Click += (_, _) => SetModelMode(PluginModelDisplayMode.Solid, true);
        _inputOverlayItem.CheckedChanged += (_, _) =>
            InputOverlayEnabledChanged?.Invoke(this, EventArgs.Empty);
        _inputOverlaySettingsItem.Click += (_, _) => InputOverlayPreferences.Edit(FindForm());
        _viewportColorSettingsItem.Click += (_, _) =>
        {
            using var dialog = new ViewportColorSettingsForm(
                ViewportColorPreferences.ForPlugin(_pluginName),
                $"{_pluginName}－ViewPort 顏色設定");
            dialog.ShowDialog(FindForm());
        };
    }

    public event EventHandler? ReadRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? UndoRequested;
    public event EventHandler? RedoRequested;
    public event EventHandler<PluginDisplayChangedEventArgs>? DisplayChanged;
    public event EventHandler? InputOverlayEnabledChanged;
    public event EventHandler<string>? InputActivity;

    public bool InputOverlayEnabled => _inputOverlayItem.Checked;

    public void SetInputOverlayEnabled(bool enabled) => _inputOverlayItem.Checked = enabled;

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        _keyboardForm = FindForm();
        if (_keyboardForm is null) return;
        _keyboardForm.KeyPreview = true;
        _keyboardForm.KeyDown += KeyboardForm_KeyDown;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _keyboardForm is not null)
            _keyboardForm.KeyDown -= KeyboardForm_KeyDown;
        base.Dispose(disposing);
    }

    private void KeyboardForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_inputOverlayItem.Checked) return;
        var text = InputActivityFormatter.Key(e.KeyData);
        if (!string.IsNullOrWhiteSpace(text)) InputActivity?.Invoke(this, text);
    }

    public void Configure(string pluginName, string pluginDescription, string? pluginDirectory = null)
    {
        _pluginName = pluginName;
        _pluginDescription = pluginDescription;
        _pluginDirectory = pluginDirectory;
    }

    public void SetCapabilities(bool canRead, bool canSave, bool canUndo, bool canRedo,
        bool camera, bool lights, bool textures, bool model)
    {
        _readItem.Enabled = canRead; _saveItem.Enabled = canSave;
        _undoItem.Enabled = canUndo; _redoItem.Enabled = canRedo;
        _cameraItem.Enabled = camera; _lightsItem.Enabled = lights; _texturesItem.Enabled = textures;
        _modelItem.Enabled = model;
    }

    public void SetEditAvailability(bool canUndo, bool canRedo)
    {
        _undoItem.Enabled = canUndo; _redoItem.Enabled = canRedo;
    }

    public void SetFileCommands(IEnumerable<PluginMenuCommand> readCommands, IEnumerable<PluginMenuCommand> saveCommands)
    {
        SetCommands(_readItem, readCommands);
        SetCommands(_saveItem, saveCommands);
    }

    public void SetInputCommands(IEnumerable<PluginMenuCommand> commands)
    {
        var file = (ToolStripMenuItem)Items[0];
        if (_inputItem is not null) { file.DropDownItems.Remove(_inputItem); _inputItem.Dispose(); }
        _inputItem = new ToolStripMenuItem("輸入");
        AddCommands(_inputItem.DropDownItems, commands.ToArray());
        file.DropDownItems.Insert(0, _inputItem);
    }

    public void SetAdditionalEditCommands(IEnumerable<PluginMenuCommand> commands)
    {
        while (_editItem.DropDownItems.Count > 2) _editItem.DropDownItems.RemoveAt(2);
        var commandArray = commands.ToArray();
        if (commandArray.Length == 0) return;
        _editItem.DropDownItems.Add(new ToolStripSeparator());
        AddCommands(_editItem.DropDownItems, commandArray);
    }

    public void SetAdditionalDisplayToggles(IEnumerable<PluginDisplayToggleCommand> commands)
    {
        foreach (var item in _additionalDisplayItems)
        {
            _displayItem.DropDownItems.Remove(item);
            item.Dispose();
        }
        _additionalDisplayItems.Clear();
        var commandArray = commands.ToArray();
        if (commandArray.Length == 0)
            return;

        var separator = new ToolStripSeparator();
        _displayItem.DropDownItems.Add(separator);
        _additionalDisplayItems.Add(separator);
        foreach (var command in commandArray)
        {
            var item = new ToolStripMenuItem(command.Text)
            {
                CheckOnClick = true,
                Checked = command.Checked
            };
            item.CheckedChanged += (_, _) => command.Changed(item.Checked);
            _displayItem.DropDownItems.Add(item);
            _additionalDisplayItems.Add(item);
        }
    }

    public void SetAdditionalDisplayToggleState(string text, bool isChecked)
    {
        var item = _additionalDisplayItems.OfType<ToolStripMenuItem>()
            .FirstOrDefault(candidate => candidate.Text == text);
        if (item is not null)
            item.Checked = isChecked;
    }

    public void RefreshCommandStates()
    {
        foreach (var item in Descendants(Items).OfType<ToolStripMenuItem>())
            if (item.Tag is Func<bool> canExecute) item.Enabled = canExecute();
    }

    private static void SetCommands(ToolStripMenuItem parent, IEnumerable<PluginMenuCommand> commands)
    {
        parent.DropDownItems.Clear();
        AddCommands(parent.DropDownItems, commands.ToArray());
        parent.Enabled = parent.DropDownItems.Count > 0;
    }

    private static void AddCommands(ToolStripItemCollection items, IReadOnlyList<PluginMenuCommand> commands)
    {
        foreach (var command in commands)
        {
            var item = new ToolStripMenuItem(command.Text);
            item.Click += command.Handler;
            if (command.CanExecute is not null)
            {
                item.Tag = command.CanExecute;
                item.Enabled = command.CanExecute();
            }
            items.Add(item);
        }
    }

    private static IEnumerable<ToolStripItem> Descendants(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            yield return item;
            if (item is ToolStripMenuItem menu)
                foreach (var child in Descendants(menu.DropDownItems)) yield return child;
        }
    }

    public void SetDisplayState(bool camera, bool lights, bool textures, PluginModelDisplayMode mode)
    {
        _updating = true;
        _cameraItem.Checked = camera; _lightsItem.Checked = lights; _texturesItem.Checked = textures;
        SetModelMode(mode, false);
        _updating = false;
    }

    private void SetModelMode(PluginModelDisplayMode mode, bool notify)
    {
        _updating = true;
        _pointsItem.Checked = mode == PluginModelDisplayMode.Points;
        _wireframeItem.Checked = mode == PluginModelDisplayMode.Wireframe;
        _solidItem.Checked = mode == PluginModelDisplayMode.Solid;
        _updating = false;
        if (notify) RaiseDisplayChanged();
    }

    private void RaiseDisplayChanged()
    {
        if (_updating) return;
        DisplayChanged?.Invoke(this, new PluginDisplayChangedEventArgs
        {
            ShowCamera = _cameraItem.Checked, ShowLights = _lightsItem.Checked,
            ShowTextures = _texturesItem.Checked,
            ModelMode = _pointsItem.Checked ? PluginModelDisplayMode.Points :
                _wireframeItem.Checked ? PluginModelDisplayMode.Wireframe : PluginModelDisplayMode.Solid
        });
    }

    private void RebuildTutorialMenu()
    {
        _tutorialItem.DropDownItems.Clear();
        var directory = Path.Combine(_pluginDirectory ?? AppContext.BaseDirectory, "Tutorials");
        if (!Directory.Exists(directory))
        {
            _tutorialItem.DropDownItems.Add(new ToolStripMenuItem("（沒有教學內容）") { Enabled = false });
            return;
        }
        AddDirectory(_tutorialItem.DropDownItems, directory);
        if (_tutorialItem.DropDownItems.Count == 0)
            _tutorialItem.DropDownItems.Add(new ToolStripMenuItem("（沒有教學內容）") { Enabled = false });
    }

    private static void AddDirectory(ToolStripItemCollection items, string directory)
    {
        foreach (var subdirectory in Directory.GetDirectories(directory).OrderBy(Path.GetFileName))
        {
            var child = new ToolStripMenuItem(Path.GetFileName(subdirectory));
            AddDirectory(child.DropDownItems, subdirectory);
            if (child.DropDownItems.Count == 0)
                child.DropDownItems.Add(new ToolStripMenuItem("（空白）") { Enabled = false });
            items.Add(child);
        }
        foreach (var file in Directory.GetFiles(directory).OrderBy(Path.GetFileName))
        {
            var item = new ToolStripMenuItem(Path.GetFileName(file)) { Tag = file };
            item.Click += OpenTutorial;
            items.Add(item);
        }
    }

    private static void OpenTutorial(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: string path } || !File.Exists(path)) return;
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        { MessageBox.Show(ex.Message, "開啟教學失敗", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
