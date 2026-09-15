using System.Numerics;
using System.Text;
using Rv3dViewer.Core;

namespace Rv3dViewer.App;

public static class StlExporter
{
    public static Task ExportAsync(ViewerProject project, string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return Task.Run(() => Write(project, filePath, cancellationToken), cancellationToken);
    }

    private static void Write(ViewerProject project, string filePath, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(filePath);
        var outputDirectory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(outputDirectory);
        var temporaryPath = Path.Combine(outputDirectory, $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            uint triangleCount = 0;
            using (var stream = File.Create(temporaryPath))
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false))
            {
                var header = new byte[80];
                Encoding.ASCII.GetBytes("Binary STL exported by Rv3d Viewer").CopyTo(header, 0);
                writer.Write(header);
                writer.Write(0u);

                foreach (var model in project.Models.Where(model => model.IsVisible))
                foreach (var instance in SceneTraversal.GetMeshInstances(model))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (model.HiddenMeshIndices.Contains(instance.MeshIndex)) continue;
                    var mesh = model.Meshes[instance.MeshIndex];
                    var world = SceneTraversal.GetMeshWorldTransform(model, instance);
                    var reverseWinding = world.GetDeterminant() < 0f;
                    for (var index = 0; index + 2 < mesh.Indices.Length; index += 3)
                    {
                        var ia = mesh.Indices[index];
                        var ib = mesh.Indices[index + (reverseWinding ? 2 : 1)];
                        var ic = mesh.Indices[index + (reverseWinding ? 1 : 2)];
                        if (ia >= mesh.Positions.Length || ib >= mesh.Positions.Length || ic >= mesh.Positions.Length) continue;
                        var a = Vector3.Transform(mesh.Positions[ia], world);
                        var b = Vector3.Transform(mesh.Positions[ib], world);
                        var c = Vector3.Transform(mesh.Positions[ic], world);
                        var cross = Vector3.Cross(b - a, c - a);
                        var normal = cross.LengthSquared() > 0.0000001f ? Vector3.Normalize(cross) : Vector3.Zero;
                        WriteVector(writer, normal);
                        WriteVector(writer, a);
                        WriteVector(writer, b);
                        WriteVector(writer, c);
                        writer.Write((ushort)0);
                        triangleCount = checked(triangleCount + 1);
                    }
                }

                if (triangleCount == 0)
                    throw new InvalidOperationException("場景中沒有可輸出的可見 Mesh。");
                stream.Position = 80;
                writer.Write(triangleCount);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static void WriteVector(BinaryWriter writer, Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }
}
