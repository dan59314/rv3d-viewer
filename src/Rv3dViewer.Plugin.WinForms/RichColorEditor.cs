using System.ComponentModel;
using System.Drawing.Design;

namespace Rv3dViewer.Plugin.WinForms;

public sealed class RichColorEditor : UITypeEditor
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered) return;
        TypeDescriptor.AddAttributes(typeof(Color),
            new EditorAttribute(typeof(RichColorEditor), typeof(UITypeEditor)),
            new TypeConverterAttribute(typeof(RichColorConverter)));
        TypeDescriptor.Refresh(typeof(Color));
        _registered = true;
    }

    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) =>
        UITypeEditorEditStyle.Modal;

    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        var color = value is Color selected ? selected : Color.White;
        using var dialog = new RichColorPickerForm(color);
        var committedColor = color;
        void Preview(Color previewColor)
        {
            if (context?.Instance is null || context.PropertyDescriptor is null) return;
            context.PropertyDescriptor.SetValue(context.Instance, previewColor);
            context.OnComponentChanged();
        }
        dialog.PreviewColorChanged += (_, _) => Preview(dialog.SelectedColor);
        dialog.ApplyRequested += (_, _) =>
        {
            Preview(dialog.SelectedColor);
            committedColor = dialog.SelectedColor;
        };
        if (dialog.ShowDialog(Form.ActiveForm) == DialogResult.OK)
            return dialog.SelectedColor;
        Preview(committedColor);
        return committedColor;
    }
}

/// <summary>
/// Preserves Color text conversion while suppressing PropertyGrid's built-in
/// Known Colors list, leaving RichColorEditor as the only color selection UI.
/// </summary>
public sealed class RichColorConverter : ColorConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => false;

    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;
}
