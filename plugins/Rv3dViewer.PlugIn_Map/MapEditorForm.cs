using System.Numerics;
using System.Runtime.InteropServices;
using Rv3dViewer.Core;
using Rv3dViewer.Plugin.Abstractions;
using Rv3dViewer.Plugin.WinForms;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.MapPlugin;

internal sealed partial class MapEditorForm : Form
{
    private readonly IPluginContext _context;
    private readonly PbrMaterial _sourceMaterial;
    private readonly PbrMaterial _originalMaterial;
    private PbrMaterial _workingMaterial;
    private readonly ViewerProject _previewProject;
    private readonly OpenGlRenderer _previewRenderer;
    private readonly Stack<PbrMaterial> _undo = [];
    private readonly Stack<PbrMaterial> _redo = [];
    private readonly TextureAssetStore _assetStore;
    private PbrMaterial _lastSnapshot;
    private bool _rebuilding;
    private bool _applied;
    private bool _accepted;

    public MapEditorForm(IPluginContext context, SceneModel model, int meshIndex, PbrMaterial material)
    {
        _context = context;
        _sourceMaterial = material;
        _originalMaterial = material.Clone();
        _workingMaterial = material.Clone();
        _workingMaterial.Validate();
        _assetStore = new TextureAssetStore(context.Project.ProjectFilePath);
        _lastSnapshot = _workingMaterial.Clone();
        InitializeComponent();
        targetLabel.Text = $"{model.Name} / {model.Meshes[meshIndex].Name}　材質：{material.Name}";
        semanticComboBox.Items.AddRange(Enum.GetValues<TextureSemantic>().Cast<object>().ToArray());
        semanticComboBox.SelectedItem = TextureSemantic.BaseColor;
        _previewProject = CreatePreviewProject(context.Project, model, meshIndex, _workingMaterial);
        _previewRenderer = new OpenGlRenderer(previewGlControl);
        _previewRenderer.SetProject(_previewProject);
        _previewRenderer.SetPreviewMode(ViewportPreviewMode.Preview2);
        _previewRenderer.SetPreviewOverlaysEnabled(false);
        InitializeUnifiedMenu();
        _previewRenderer.RendererError += PreviewRenderer_RendererError;
        FitPreviewCamera();
        RebuildLayerList();
        UpdateCommands();
    }

    private TextureSemantic CurrentSemantic => semanticComboBox.SelectedItem is TextureSemantic semantic
        ? semantic : TextureSemantic.BaseColor;
    private TextureStack CurrentStack => _workingMaterial.GetOrCreateTextureStack(CurrentSemantic);
    private TextureLayer? CurrentLayer => layerListBox.SelectedItem is LayerListItem item ? item.Layer : null;

    private static ViewerProject CreatePreviewProject(
        ViewerProject sourceProject,
        SceneModel sourceModel,
        int meshIndex,
        PbrMaterial material)
    {
        var source = sourceModel.Meshes[meshIndex];
        var mesh = new MeshData
        {
            Name = source.Name,
            Positions = source.Positions.ToArray(), Normals = source.Normals.ToArray(),
            TextureCoordinates = source.TextureCoordinates.ToArray(), Tangents = source.Tangents.ToArray(),
            Indices = source.Indices.ToArray(), MaterialIndex = 0
        };
        var instances = SceneTraversal.GetMeshInstances(sourceModel)
            .Where(instance => instance.MeshIndex == meshIndex)
            .ToArray();
        List<SceneNode> nodes = instances.Length == 0
            ? [new SceneNode { Name = source.Name, MeshIndices = [0] }]
            : instances.Select((instance, index) => new SceneNode
            {
                Name = instances.Length == 1 ? source.Name : $"{source.Name} {index + 1}",
                LocalTransform = instance.NodeTransform,
                MeshIndices = [0]
            }).ToList();
        var model = new SceneModel
        {
            Name = source.Name, IsProcedural = true, Meshes = [mesh], EmbeddedMeshes = [mesh],
            Materials = [material], SourceMeshIndices = [0], SourceMeshIndicesCaptured = true,
            Nodes = nodes,
            Transform = CloneTransform(sourceModel.Transform)
        };
        if (sourceModel.MeshTransforms.TryGetValue(meshIndex, out var meshTransform))
            model.MeshTransforms[0] = CloneTransform(meshTransform);
        model.EmbeddedNodes = model.Nodes;
        return new ViewerProject
        {
            Name = "Map Preview",
            ProjectFilePath = sourceProject.ProjectFilePath,
            Models = [model],
            Lights = sourceProject.Lights.Select(CloneLight).ToList(),
            RenderSettings = CloneRenderSettings(sourceProject.RenderSettings),
            Environment = CloneEnvironment(sourceProject.Environment)
        };
    }

    private static TransformState CloneTransform(TransformState source) => new()
    {
        Position = source.Position,
        RotationDegrees = source.RotationDegrees,
        Scale = source.Scale
    };

    private static SceneLight CloneLight(SceneLight source) => new()
    {
        Name = source.Name,
        Enabled = source.Enabled,
        Type = source.Type,
        Position = source.Position,
        Direction = source.Direction,
        Color = source.Color,
        Intensity = source.Intensity,
        Range = source.Range,
        FallInDegrees = source.FallInDegrees,
        FallOffDegrees = source.FallOffDegrees
    };

    private static RenderSettings CloneRenderSettings(RenderSettings source) => new()
    {
        BackgroundColor = source.BackgroundColor,
        ShowTextures = source.ShowTextures,
        ShowGrid = false,
        ShowWorldAxes = false,
        ShowSelectionHighlight = false,
        Wireframe = source.Wireframe,
        ModelDisplayMode = source.ModelDisplayMode,
        ShowLightGizmos = false,
        LightGizmoSizePixels = source.LightGizmoSizePixels
    };

    private static EnvironmentSettings CloneEnvironment(EnvironmentSettings source) => new()
    {
        Enabled = source.Enabled,
        Path = source.Path,
        Intensity = source.Intensity,
        RotationDegrees = source.RotationDegrees,
        ShowBackground = source.ShowBackground,
        BackgroundBlur = source.BackgroundBlur
    };

    private void FitPreviewCamera()
    {
        if (!SceneTraversal.TryCalculateBounds(_previewProject.Models[0], out var bounds)) return;
        var center = bounds.Center;
        var radius = Math.Max(bounds.Radius, 0.01f);
        _previewProject.Camera.To = center;
        _previewProject.Camera.From = center + Vector3.Normalize(new Vector3(1.2f, 0.75f, 1.4f)) * radius * 2.8f;
        _previewProject.Camera.Up = Vector3.UnitY;
        _previewProject.Camera.FieldOfViewDegrees = 45f;
        _previewProject.Camera.NearPlane = Math.Max(radius / 1000f, 0.0001f);
        _previewProject.Camera.FarPlane = Math.Max(radius * 100f, 100f);
        _previewRenderer?.InvalidateScene();
    }

    private void SemanticComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_rebuilding) return;
        RebuildLayerList();
    }

    private void StackEnabledCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_rebuilding) return;
        Change(() => CurrentStack.Enabled = stackEnabledCheckBox.Checked);
    }

    private void LayerListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        layerPropertyGrid.SelectedObject = CurrentLayer;
        UpdateCommands();
    }

    private void AddImageButton_Click(object? sender, EventArgs e)
    {
        imageOpenFileDialog.Title = $"新增 {CurrentSemantic} 圖層";
        if (imageOpenFileDialog.ShowDialog(this) != DialogResult.OK) return;
        if (!TryStoreAsset(imageOpenFileDialog.FileName, out var storedPath)) return;
        AddLayer(new TextureLayer { Name = Path.GetFileNameWithoutExtension(imageOpenFileDialog.FileName), Path = storedPath });
    }

    private void AddColorButton_Click(object? sender, EventArgs e) => AddLayer(new TextureLayer { Name = "純色", Kind = TextureLayerKind.SolidColor });
    private void AddCheckerButton_Click(object? sender, EventArgs e) => AddLayer(new TextureLayer { Name = "Checker", Kind = TextureLayerKind.Checker });
    private void AddNoiseButton_Click(object? sender, EventArgs e) => AddLayer(new TextureLayer { Name = "Noise", Kind = TextureLayerKind.Noise });

    private void LoadPbrSetButton_Click(object? sender, EventArgs e)
    {
        imageOpenFileDialog.Title = "選擇 PBR 貼圖組中的任一圖片";
        if (imageOpenFileDialog.ShowDialog(this) != DialogResult.OK) return;
        var matches = PbrTextureSetDiscovery.Discover(imageOpenFileDialog.FileName);
        if (matches.Count == 0)
        {
            MessageBox.Show(this, "找不到可辨識的 PBR 檔名。請使用 BaseColor、Roughness、Metallic、Normal、Height、AO、Emissive 或 Opacity 等名稱。",
                "貼圖編輯器", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var storedMatches = new List<(PbrTextureMatch Match, string Path)>();
        foreach (var match in matches)
        {
            if (!TryStoreAsset(match.Path, out var storedPath)) return;
            storedMatches.Add((match, storedPath));
        }
        Change(() =>
        {
            foreach (var storedMatch in storedMatches)
            {
                var match = storedMatch.Match;
                var stack = _workingMaterial.GetOrCreateTextureStack(match.Semantic);
                if (stack.Layers.Count >= 8) continue;
                stack.Layers.Add(new TextureLayer
                {
                    Name = Path.GetFileNameWithoutExtension(match.Path), Path = storedMatch.Path,
                    Channels = new TextureChannelSettings
                    {
                        Source = match.Channel, ColorSpace = match.ColorSpace
                    }
                });
            }
        });
        RebuildLayerList();
        _context.SetStatus($"已載入 {matches.Count} 種 PBR 貼圖");
    }

    private void AddLayer(TextureLayer layer)
    {
        if (CurrentStack.Layers.Count >= 8)
        {
            MessageBox.Show(this, "每種貼圖最多可使用 8 個圖層。", "貼圖編輯器", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Change(() => CurrentStack.Layers.Add(layer));
        RebuildLayerList(layer.Id);
    }

    private void DuplicateLayerButton_Click(object? sender, EventArgs e)
    {
        if (CurrentLayer is not { } layer || CurrentStack.Layers.Count >= 8) return;
        var clone = layer.Clone(); clone.Name += " 副本";
        var index = CurrentStack.Layers.IndexOf(layer) + 1;
        Change(() => CurrentStack.Layers.Insert(index, clone));
        RebuildLayerList(clone.Id);
    }

    private void RemoveLayerButton_Click(object? sender, EventArgs e)
    {
        if (CurrentLayer is not { } layer) return;
        var index = CurrentStack.Layers.IndexOf(layer);
        Change(() => CurrentStack.Layers.Remove(layer));
        RebuildLayerList(index > 0 ? CurrentStack.Layers.ElementAtOrDefault(index - 1)?.Id : CurrentStack.Layers.FirstOrDefault()?.Id);
    }

    private void MoveLayerUpButton_Click(object? sender, EventArgs e) => MoveLayer(-1);
    private void MoveLayerDownButton_Click(object? sender, EventArgs e) => MoveLayer(1);

    private void MoveLayer(int offset)
    {
        if (CurrentLayer is not { } layer) return;
        var from = CurrentStack.Layers.IndexOf(layer); var to = from + offset;
        if ((uint)to >= (uint)CurrentStack.Layers.Count) return;
        Change(() => { CurrentStack.Layers.RemoveAt(from); CurrentStack.Layers.Insert(to, layer); });
        RebuildLayerList(layer.Id);
    }

    private void LayerPropertyGrid_PropertyValueChanged(object? sender, PropertyValueChangedEventArgs e)
    {
        _undo.Push(_lastSnapshot);
        _redo.Clear();
        _workingMaterial.Validate();
        _lastSnapshot = _workingMaterial.Clone();
        RefreshPreview();
        RebuildLayerList(CurrentLayer?.Id);
    }

    private void ChooseImageButton_Click(object? sender, EventArgs e)
    {
        if (CurrentLayer is not { } layer) return;
        imageOpenFileDialog.Title = "選擇圖層圖片";
        if (imageOpenFileDialog.ShowDialog(this) != DialogResult.OK) return;
        if (!TryStoreAsset(imageOpenFileDialog.FileName, out var storedPath)) return;
        Change(() => { layer.Kind = TextureLayerKind.Image; layer.Path = storedPath; layer.Name = Path.GetFileNameWithoutExtension(imageOpenFileDialog.FileName); });
        RebuildLayerList(layer.Id);
    }

    private void ChooseColorButton_Click(object? sender, EventArgs e)
    {
        if (CurrentLayer is not { } layer) return;
        var currentColor = Color.FromArgb(ToByte(layer.Color.W), ToByte(layer.Color.X),
            ToByte(layer.Color.Y), ToByte(layer.Color.Z));
        using var dialog = new RichColorPickerForm(currentColor);
        var committedValue = layer.Color;
        static Vector4 ToVector(Color color) =>
            new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        void Preview(Color color)
        {
            layer.Color = ToVector(color);
            _workingMaterial.Validate();
            layerPropertyGrid.Refresh();
            RefreshPreview();
        }
        void Commit(Color color)
        {
            var value = ToVector(color);
            layer.Color = committedValue;
            Change(() => layer.Color = value);
            committedValue = value;
        }
        dialog.PreviewColorChanged += (_, _) => Preview(dialog.SelectedColor);
        dialog.ApplyRequested += (_, _) => Commit(dialog.SelectedColor);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            Commit(dialog.SelectedColor);
        else
        {
            layer.Color = committedValue;
            _workingMaterial.Validate();
            RefreshPreview();
        }
        layerPropertyGrid.Refresh();
    }

    private void ChooseMaskButton_Click(object? sender, EventArgs e)
    {
        if (CurrentLayer is not { } layer) return;
        imageOpenFileDialog.Title = "選擇圖層 Mask";
        if (imageOpenFileDialog.ShowDialog(this) != DialogResult.OK) return;
        if (!TryStoreAsset(imageOpenFileDialog.FileName, out var storedPath)) return;
        Change(() => { layer.Mask.Path = storedPath; layer.Mask.Enabled = true; });
        layerPropertyGrid.Refresh();
    }

    private void ClearMaskButton_Click(object? sender, EventArgs e)
    {
        if (CurrentLayer is not { } layer) return;
        Change(() => { layer.Mask.Path = string.Empty; layer.Mask.Enabled = false; });
        layerPropertyGrid.Refresh();
    }

    private void BakeButton_Click(object? sender, EventArgs e)
    {
        var unavailable = TextureAssetStore.FindUnavailableAssets(CurrentStack, ResolveAssetPath);
        if (unavailable.Count > 0)
        {
            MessageBox.Show(this, $"下列貼圖無法讀取，請先更換或移除：\n{string.Join("\n", unavailable)}",
                "貼圖編輯器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        bakeSaveFileDialog.FileName = $"{_workingMaterial.Name}_{CurrentSemantic}.png";
        if (bakeSaveFileDialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            using var bitmap = TextureStackComposer.Compose(CurrentStack, ResolveAssetPath, 1024);
            bitmap.Save(bakeSaveFileDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            _context.SetStatus($"已烘焙貼圖：{Path.GetFileName(bakeSaveFileDialog.FileName)}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ExternalException)
        {
            MessageBox.Show(this, $"無法儲存烘焙貼圖：{exception.Message}",
                "貼圖編輯器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void FitMeshButton_Click(object? sender, EventArgs e) => FitPreviewCamera();
    private void UndoButton_Click(object? sender, EventArgs e)
    {
        if (_undo.Count == 0) return;
        _redo.Push(_workingMaterial.Clone());
        ReplaceWorkingMaterial(_undo.Pop());
    }
    private void RedoButton_Click(object? sender, EventArgs e)
    {
        if (_redo.Count == 0) return;
        _undo.Push(_workingMaterial.Clone());
        ReplaceWorkingMaterial(_redo.Pop());
    }

    private void ResetStackButton_Click(object? sender, EventArgs e)
    {
        var semantic = CurrentSemantic;
        Change(() => _workingMaterial.TextureStacks[semantic] = new TextureStack { Semantic = semantic });
        RebuildLayerList();
    }

    private void ApplyButton_Click(object? sender, EventArgs e) => ApplyToSource();
    private void OkButton_Click(object? sender, EventArgs e) { ApplyToSource(); _accepted = true; }

    private void ApplyToSource()
    {
        CopyMaterial(_workingMaterial, _sourceMaterial);
        _applied = true;
        _context.NotifySceneChanged();
        _context.SetStatus($"貼圖編輯器已套用材質：{_sourceMaterial.Name}");
    }

    private void MapEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_accepted) { _assetStore.Commit(); return; }
        _assetStore.Rollback();
        if (_applied)
        {
            CopyMaterial(_originalMaterial, _sourceMaterial);
            _context.NotifySceneChanged();
            _context.SetStatus("已取消貼圖編輯並還原材質");
        }
    }

    private void MapEditorForm_FormClosed(object? sender, FormClosedEventArgs e)
    {
        _previewRenderer.RendererError -= PreviewRenderer_RendererError;
        _previewRenderer.Dispose();
    }

    private void PreviewRenderer_RendererError(object? sender, string message) =>
        BeginInvoke(() => targetLabel.Text = message);

    private void Change(Action action)
    {
        _undo.Push(_workingMaterial.Clone());
        _redo.Clear();
        action();
        _workingMaterial.Validate();
        _lastSnapshot = _workingMaterial.Clone();
        RefreshPreview();
        UpdateCommands();
    }

    private void ReplaceWorkingMaterial(PbrMaterial material)
    {
        _workingMaterial = material.Clone();
        _previewProject.Models[0].Materials[0] = _workingMaterial;
        _lastSnapshot = _workingMaterial.Clone();
        RebuildLayerList();
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        _previewProject.Models[0].Materials[0] = _workingMaterial;
        _previewRenderer.SetProject(_previewProject);
        _previewRenderer.SetPreviewOverlaysEnabled(false);
        layerPropertyGrid.Refresh();
        UpdateCommands();
    }

    private void RebuildLayerList(Guid? selectId = null)
    {
        _rebuilding = true;
        var stack = CurrentStack;
        var previousId = selectId ?? CurrentLayer?.Id;
        stackEnabledCheckBox.Checked = stack.Enabled;
        layerListBox.BeginUpdate();
        layerListBox.Items.Clear();
        foreach (var layer in stack.Layers) layerListBox.Items.Add(new LayerListItem(layer));
        layerListBox.EndUpdate();
        var selected = layerListBox.Items.Cast<LayerListItem>().FirstOrDefault(item => item.Layer.Id == previousId);
        layerListBox.SelectedItem = selected ?? layerListBox.Items.Cast<object>().LastOrDefault();
        layerPropertyGrid.SelectedObject = CurrentLayer;
        _rebuilding = false;
        UpdateCommands();
    }

    private void UpdateCommands()
    {
        var hasLayer = CurrentLayer is not null;
        duplicateLayerButton.Enabled = hasLayer && CurrentStack.Layers.Count < 8;
        removeLayerButton.Enabled = hasLayer;
        moveLayerUpButton.Enabled = hasLayer && layerListBox.SelectedIndex > 0;
        moveLayerDownButton.Enabled = hasLayer && layerListBox.SelectedIndex >= 0 && layerListBox.SelectedIndex < layerListBox.Items.Count - 1;
        chooseImageButton.Enabled = hasLayer;
        chooseColorButton.Enabled = hasLayer;
        chooseMaskButton.Enabled = hasLayer;
        clearMaskButton.Enabled = hasLayer && CurrentLayer!.Mask.Enabled;
        bakeButton.Enabled = CurrentStack.Layers.Count > 0;
        undoButton.Enabled = _undo.Count > 0;
        redoButton.Enabled = _redo.Count > 0;
        unifiedMenuStrip.SetEditAvailability(undoButton.Enabled, redoButton.Enabled);
        unifiedMenuStrip.RefreshCommandStates();
    }

    private bool TryStoreAsset(string path, out string storedPath)
    {
        try
        {
            storedPath = _assetStore.Import(path);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            MessageBox.Show(this, exception.Message, "貼圖編輯器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            storedPath = string.Empty;
            return false;
        }
    }

    private string ResolveAssetPath(string path) => _assetStore.Resolve(path);
    private static int ToByte(float value) => Math.Clamp((int)MathF.Round(value * 255f), 0, 255);

    private static void CopyMaterial(PbrMaterial source, PbrMaterial target)
    {
        var copy = source.Clone(source.Name);
        target.Name = copy.Name; target.BaseColor = copy.BaseColor; target.Metallic = copy.Metallic;
        target.Roughness = copy.Roughness; target.NormalScale = copy.NormalScale;
        target.AmbientOcclusion = copy.AmbientOcclusion; target.Emissive = copy.Emissive;
        target.EmissiveStrength = copy.EmissiveStrength; target.Opacity = copy.Opacity;
        target.DoubleSided = copy.DoubleSided; target.RenderMode = copy.RenderMode;
        target.AlphaCutoff = copy.AlphaCutoff; target.Transmission = copy.Transmission;
        target.IndexOfRefraction = copy.IndexOfRefraction; target.Thickness = copy.Thickness;
        target.RefractionStrength = copy.RefractionStrength; target.AbsorptionColor = copy.AbsorptionColor;
        target.Textures = copy.Textures; target.TextureStacks = copy.TextureStacks;
        target.Validate();
    }

    private sealed class LayerListItem(TextureLayer layer)
    {
        public TextureLayer Layer { get; } = layer;
        public override string ToString() => $"{(Layer.Enabled ? "●" : "○")} {Layer.Name}　[{Layer.BlendMode}] {Layer.Opacity:P0}";
    }
}
