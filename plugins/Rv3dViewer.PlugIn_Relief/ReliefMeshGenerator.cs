using System.Numerics;
using Rv3dViewer.Core;
using SkiaSharp;

namespace Rv3dViewer.ReliefPlugin;

internal enum ReliefModelPlane { XY, YZ, XZ }
internal enum ReliefModelAlignment { Center, LeftBottom }

internal readonly record struct ReliefMeshBuildSettings(
    float TotalWidth,
    float TotalThickness,
    float BorderWidth,
    float ReliefHeight,
    int LongestSidePoints,
    string TexturePath,
    bool SmoothingEnabled,
    float SmoothingThreshold,
    float SmoothingStrength,
    int SmoothingIterations,
    bool SimplificationEnabled,
    float SimplificationTargetRatio,
    float SimplificationNormalAngle,
    ReliefModelPlane ModelPlane,
    float ModelRotationAngle,
    ReliefModelAlignment ModelAlignment);

internal sealed record ReliefMeshBuildResult(
    SceneModel Model,
    float FinishedWidth,
    float FinishedHeight,
    float MinimumTopHeight,
    float MaximumTopHeight,
    int Columns,
    int Rows,
    int VertexCount,
    int TriangleCount,
    int OpenGeometricEdgeCount,
    float[] TopHeights,
    bool SmoothingApplied,
    bool SimplificationApplied,
    int OriginalTriangleCount);

internal static class ReliefMeshGenerator
{
    public static ReliefMeshBuildResult Generate(
        SKBitmap depthBitmap,
        ReliefMeshBuildSettings settings,
        CancellationToken cancellationToken)
    {
        return Generate(ReliefDepthMap.FromBitmap(depthBitmap, cancellationToken), settings, cancellationToken);
    }

    public static ReliefMeshBuildResult Generate(
        ReliefDepthMap depthMap,
        ReliefMeshBuildSettings settings,
        CancellationToken cancellationToken)
    {
        Validate(depthMap.Width, depthMap.Height, settings);
        var finishedHeight = settings.TotalWidth * depthMap.Height / depthMap.Width;
        if (settings.BorderWidth * 2f >= Math.Min(settings.TotalWidth, finishedHeight))
            throw new ArgumentOutOfRangeException(nameof(settings.BorderWidth), "邊框寬度必須小於成品寬、高較短邊的一半。");

        var (columns, rows) = CalculateGridSize(
            settings.TotalWidth,
            finishedHeight,
            settings.LongestSidePoints);
        var xDistances = CreateAxisDistances(settings.TotalWidth, columns, settings.BorderWidth);
        var zDistances = CreateAxisDistances(finishedHeight, rows, settings.BorderWidth);
        var baseThickness = settings.TotalThickness - settings.ReliefHeight;
        var heights = CreateHeightField(
            depthMap,
            settings.TotalWidth,
            finishedHeight,
            settings.BorderWidth,
            baseThickness,
            settings.ReliefHeight,
            xDistances,
            zDistances,
            cancellationToken);
        if (settings.SmoothingEnabled)
        {
            heights = SmoothHeightField(
                heights,
                settings.TotalWidth,
                finishedHeight,
                settings.BorderWidth,
                baseThickness,
                settings.TotalThickness,
                xDistances,
                zDistances,
                settings.SmoothingThreshold,
                settings.SmoothingStrength,
                settings.SmoothingIterations,
                cancellationToken);
        }

        var front = CreateFrontMesh(
            heights,
            settings.TotalWidth,
            finishedHeight,
            xDistances,
            zDistances,
            cancellationToken);
        var shell = CreateShellMesh(
            heights,
            settings.TotalWidth,
            finishedHeight,
            xDistances,
            zDistances,
            cancellationToken);
        var originalTriangleCount = front.TriangleCount + shell.TriangleCount;
        var simplificationApplied = false;
        if (settings.SimplificationEnabled)
        {
            var simplifiedFront = ReliefMeshSimplifier.Simplify(
                front, settings.SimplificationTargetRatio, settings.SimplificationNormalAngle, cancellationToken);
            if (simplifiedFront.TriangleCount < front.TriangleCount)
            {
                var candidate = CreateModel(simplifiedFront, shell, settings.TexturePath);
                if (CountOpenGeometricEdges(candidate.Meshes, cancellationToken) == 0)
                {
                    front = simplifiedFront;
                    simplificationApplied = true;
                }
            }
        }
        TransformMeshForPlane(front, settings.ModelPlane);
        TransformMeshForPlane(shell, settings.ModelPlane);
        RotateMeshesAroundPlaneNormal(front, shell, settings.ModelPlane, settings.ModelRotationAngle);
        AlignMeshes(front, shell, settings.ModelAlignment);
        var model = CreateModel(front, shell, settings.TexturePath);
        var openEdges = CountOpenGeometricEdges(model.Meshes, cancellationToken);
        if (openEdges != 0)
            throw new InvalidDataException($"浮雕網格未完整封閉，偵測到 {openEdges} 條開放邊。");

        return new ReliefMeshBuildResult(
            model,
            settings.TotalWidth,
            finishedHeight,
            heights.Min(),
            heights.Max(),
            columns,
            rows,
            front.Positions.Length + shell.Positions.Length,
            front.TriangleCount + shell.TriangleCount,
            openEdges,
            heights,
            settings.SmoothingEnabled,
            simplificationApplied,
            originalTriangleCount);
    }

    private static void AlignMeshes(MeshData front, MeshData shell, ReliefModelAlignment alignment)
    {
        if (alignment == ReliefModelAlignment.Center) return;

        var minimum = new Vector3(float.PositiveInfinity);
        foreach (var position in front.Positions.Concat(shell.Positions))
            minimum = Vector3.Min(minimum, position);
        Translate(front, -minimum);
        Translate(shell, -minimum);
        return;

        static void Translate(MeshData mesh, Vector3 offset)
        {
            for (var index = 0; index < mesh.Positions.Length; index++)
                mesh.Positions[index] += offset;
        }
    }

    private static void RotateMeshesAroundPlaneNormal(
        MeshData front,
        MeshData shell,
        ReliefModelPlane plane,
        float angleDegrees)
    {
        if (MathF.Abs(angleDegrees) < 0.0001f) return;

        var minimum = new Vector3(float.PositiveInfinity);
        var maximum = new Vector3(float.NegativeInfinity);
        foreach (var position in front.Positions.Concat(shell.Positions))
        {
            minimum = Vector3.Min(minimum, position);
            maximum = Vector3.Max(maximum, position);
        }

        var center = (minimum + maximum) * 0.5f;
        var axis = plane switch
        {
            ReliefModelPlane.XY => Vector3.UnitZ,
            ReliefModelPlane.YZ => Vector3.UnitX,
            _ => Vector3.UnitY
        };
        var rotation = Matrix4x4.CreateFromAxisAngle(axis, angleDegrees * MathF.PI / 180f);
        Rotate(front);
        Rotate(shell);
        return;

        void Rotate(MeshData mesh)
        {
            for (var index = 0; index < mesh.Positions.Length; index++)
                mesh.Positions[index] = Vector3.Transform(mesh.Positions[index] - center, rotation) + center;
            for (var index = 0; index < mesh.Normals.Length; index++)
                mesh.Normals[index] = Vector3.Normalize(Vector3.TransformNormal(mesh.Normals[index], rotation));
            for (var index = 0; index < mesh.Tangents.Length; index++)
            {
                var tangent = mesh.Tangents[index];
                var direction = Vector3.Normalize(Vector3.TransformNormal(
                    new Vector3(tangent.X, tangent.Y, tangent.Z), rotation));
                mesh.Tangents[index] = new Vector4(direction, tangent.W);
            }
        }
    }

    private static void TransformMeshForPlane(MeshData mesh, ReliefModelPlane plane)
    {
        if (plane == ReliefModelPlane.XZ) return;
        for (var index = 0; index < mesh.Positions.Length; index++)
            mesh.Positions[index] = Transform(mesh.Positions[index]);
        for (var index = 0; index < mesh.Normals.Length; index++)
            mesh.Normals[index] = Vector3.Normalize(Transform(mesh.Normals[index]));
        for (var index = 0; index < mesh.Tangents.Length; index++)
        {
            var tangent = mesh.Tangents[index];
            var transformed = Vector3.Normalize(Transform(new Vector3(tangent.X, tangent.Y, tangent.Z)));
            mesh.Tangents[index] = new Vector4(transformed, tangent.W);
        }
        return;

        Vector3 Transform(Vector3 value) => plane switch
        {
            ReliefModelPlane.XY => new Vector3(-value.X, value.Z, value.Y),
            ReliefModelPlane.YZ => new Vector3(value.Y, -value.X, value.Z),
            _ => value
        };
    }

    private static void Validate(int width, int height, ReliefMeshBuildSettings settings)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("深度影像尺寸無效。", nameof(width));
        if (!float.IsFinite(settings.TotalWidth) || settings.TotalWidth <= 0f)
            throw new ArgumentOutOfRangeException(nameof(settings.TotalWidth), "成品總寬必須大於 0。 ");
        if (!float.IsFinite(settings.TotalThickness) || settings.TotalThickness <= 0f)
            throw new ArgumentOutOfRangeException(nameof(settings.TotalThickness), "成品厚度必須大於 0。");
        if (!float.IsFinite(settings.ReliefHeight) || settings.ReliefHeight <= 0f ||
            settings.ReliefHeight > settings.TotalThickness)
            throw new ArgumentOutOfRangeException(nameof(settings.ReliefHeight), "浮雕高度必須大於 0，且不得超過成品厚度。");
        if (!float.IsFinite(settings.BorderWidth) || settings.BorderWidth < 0f)
            throw new ArgumentOutOfRangeException(nameof(settings.BorderWidth), "邊框寬度不得小於 0。");
        if (!float.IsFinite(settings.ModelRotationAngle) || settings.ModelRotationAngle is < -180f or > 180f)
            throw new ArgumentOutOfRangeException(nameof(settings.ModelRotationAngle), "模型旋轉角度必須介於 -180° 到 180°。");
        if (settings.LongestSidePoints is < 2 or > 512)
            throw new ArgumentOutOfRangeException(nameof(settings.LongestSidePoints), "網格最長邊點數必須介於 2 到 512。");
        if (!float.IsFinite(settings.SmoothingThreshold) || settings.SmoothingThreshold <= 0f)
            throw new ArgumentOutOfRangeException(nameof(settings.SmoothingThreshold), "平滑落差門檻必須大於 0。");
        if (!float.IsFinite(settings.SmoothingStrength) || settings.SmoothingStrength is <= 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(settings.SmoothingStrength), "平滑強度必須大於 0 且不超過 100%。");
        if (settings.SmoothingIterations is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(settings.SmoothingIterations), "平滑疊代次數必須介於 1 到 10。");
        if (string.IsNullOrWhiteSpace(settings.TexturePath) || !File.Exists(settings.TexturePath))
            throw new FileNotFoundException("找不到浮雕正面使用的原始影像工作貼圖。", settings.TexturePath);
    }

    private static (int Columns, int Rows) CalculateGridSize(float width, float height, int longestSidePoints)
    {
        if (width >= height)
            return (longestSidePoints, Math.Max(4, (int)MathF.Round(longestSidePoints * height / width)));
        return (Math.Max(4, (int)MathF.Round(longestSidePoints * width / height)), longestSidePoints);
    }

    private static float[] CreateAxisDistances(float length, int pointCount, float borderWidth)
    {
        var distances = new float[pointCount];
        if (borderWidth <= 0f)
        {
            for (var index = 0; index < pointCount; index++)
                distances[index] = length * index / (pointCount - 1f);
            return distances;
        }

        var maximumBorderIndex = Math.Max(1, (pointCount - 2) / 2);
        var firstInnerIndex = Math.Clamp(
            (int)MathF.Round(borderWidth / length * (pointCount - 1)),
            1,
            maximumBorderIndex);
        var lastInnerIndex = pointCount - 1 - firstInnerIndex;
        for (var index = 0; index <= firstInnerIndex; index++)
            distances[index] = borderWidth * index / firstInnerIndex;
        for (var index = firstInnerIndex + 1; index < lastInnerIndex; index++)
            distances[index] = borderWidth +
                (length - 2f * borderWidth) * (index - firstInnerIndex) / (lastInnerIndex - firstInnerIndex);
        for (var index = lastInnerIndex; index < pointCount; index++)
            distances[index] = length - borderWidth +
                borderWidth * (index - lastInnerIndex) / (pointCount - 1 - lastInnerIndex);
        return distances;
    }

    private static float[] CreateHeightField(
        ReliefDepthMap depthMap,
        float width,
        float height,
        float borderWidth,
        float baseThickness,
        float reliefHeight,
        float[] xDistances,
        float[] zDistances,
        CancellationToken cancellationToken)
    {
        var columns = xDistances.Length;
        var rows = zDistances.Length;
        var output = new float[columns * rows];
        var values = depthMap.Values;
        for (var row = 0; row < rows; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var distanceFromTop = zDistances[row];
            var imageV = distanceFromTop / height;
            var distanceFromBottom = height - distanceFromTop;
            for (var column = 0; column < columns; column++)
            {
                var distanceFromLeft = xDistances[column];
                var imageU = distanceFromLeft / width;
                var distanceFromRight = width - distanceFromLeft;
                var isBorder = borderWidth > 0f &&
                    Math.Min(Math.Min(distanceFromLeft, distanceFromRight), Math.Min(distanceFromTop, distanceFromBottom)) <= borderWidth;
                output[row * columns + column] = isBorder
                    ? baseThickness + reliefHeight
                    : baseThickness + SampleDepth(values, depthMap.Width, depthMap.Height, 1f - imageU, imageV) * reliefHeight;
            }
        }
        return output;
    }

    private static float SampleDepth(float[] values, int width, int height, float u, float v)
    {
        var x = Math.Clamp(u, 0f, 1f) * (width - 1);
        var y = Math.Clamp(v, 0f, 1f) * (height - 1);
        var x0 = (int)MathF.Floor(x);
        var y0 = (int)MathF.Floor(y);
        var x1 = Math.Min(x0 + 1, width - 1);
        var y1 = Math.Min(y0 + 1, height - 1);
        var tx = x - x0;
        var ty = y - y0;
        var top = Lerp(values[y0 * width + x0], values[y0 * width + x1], tx);
        var bottom = Lerp(values[y1 * width + x0], values[y1 * width + x1], tx);
        return Lerp(top, bottom, ty);
    }

    private static float Lerp(float first, float second, float amount) => first + (second - first) * amount;

    private static float[] SmoothHeightField(
        float[] heights,
        float width,
        float height,
        float borderWidth,
        float minimumHeight,
        float maximumHeight,
        float[] xDistances,
        float[] zDistances,
        float threshold,
        float strength,
        int iterations,
        CancellationToken cancellationToken)
    {
        var columns = xDistances.Length;
        var rows = zDistances.Length;
        var current = (float[])heights.Clone();
        var next = new float[current.Length];
        var borderMask = new bool[current.Length];
        for (var row = 0; row < rows; row++)
        {
            var top = zDistances[row];
            var bottom = height - top;
            for (var column = 0; column < columns; column++)
            {
                var left = xDistances[column];
                var right = width - left;
                borderMask[row * columns + column] = borderWidth > 0f &&
                    Math.Min(Math.Min(left, right), Math.Min(top, bottom)) <= borderWidth;
            }
        }

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Array.Copy(current, next, current.Length);
            for (var row = 1; row < rows - 1; row++)
            {
                if ((row & 31) == 0) cancellationToken.ThrowIfCancellationRequested();
                for (var column = 1; column < columns - 1; column++)
                {
                    var index = row * columns + column;
                    if (borderMask[index]) continue;

                    var sum = 0f;
                    var count = 0;
                    var maximumDelta = 0f;

                    Accumulate(index - 1);
                    Accumulate(index + 1);
                    Accumulate(index - columns);
                    Accumulate(index + columns);
                    if (count == 0 || maximumDelta <= threshold) continue;

                    var average = sum / count;
                    next[index] = Math.Clamp(
                        Lerp(current[index], average, strength),
                        minimumHeight,
                        maximumHeight);

                    void Accumulate(int neighborIndex)
                    {
                        if (borderMask[neighborIndex]) return;
                        var neighborHeight = current[neighborIndex];
                        sum += neighborHeight;
                        count++;
                        maximumDelta = Math.Max(maximumDelta, Math.Abs(neighborHeight - current[index]));
                    }
                }
            }
            (current, next) = (next, current);
        }

        return current;
    }

    public static SKBitmap CreateHeightPreview(ReliefMeshBuildResult result)
    {
        var bitmap = new SKBitmap(result.Columns, result.Rows, SKColorType.Bgra8888, SKAlphaType.Premul);
        var pixels = new SKColor[result.TopHeights.Length];
        var range = result.MaximumTopHeight - result.MinimumTopHeight;
        for (var row = 0; row < result.Rows; row++)
        for (var column = 0; column < result.Columns; column++)
        {
            var index = row * result.Columns + column;
            var sourceIndex = row * result.Columns + (result.Columns - 1 - column);
            var normalized = range <= float.Epsilon
                ? 0f
                : Math.Clamp((result.TopHeights[sourceIndex] - result.MinimumTopHeight) / range, 0f, 1f);
            var value = (byte)MathF.Round(normalized * 255f);
            pixels[index] = new SKColor(value, value, value, 255);
        }
        bitmap.Pixels = pixels;
        return bitmap;
    }

    private static MeshData CreateFrontMesh(
        float[] heights,
        float width,
        float height,
        float[] xDistances,
        float[] zDistances,
        CancellationToken cancellationToken)
    {
        var columns = xDistances.Length;
        var rows = zDistances.Length;
        var positions = new Vector3[columns * rows];
        var normals = new Vector3[positions.Length];
        var textureCoordinates = new Vector2[positions.Length];
        var tangents = new Vector4[positions.Length];
        for (var row = 0; row < rows; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var imageV = zDistances[row] / height;
            var z = height * 0.5f - zDistances[row];
            for (var column = 0; column < columns; column++)
            {
                var index = row * columns + column;
                var imageU = xDistances[column] / width;
                var x = -width * 0.5f + xDistances[column];
                positions[index] = new Vector3(x, heights[index], z);
                textureCoordinates[index] = new Vector2(1f - imageU, 1f - imageV);

                var left = heights[row * columns + Math.Max(0, column - 1)];
                var right = heights[row * columns + Math.Min(columns - 1, column + 1)];
                var top = heights[Math.Max(0, row - 1) * columns + column];
                var bottom = heights[Math.Min(rows - 1, row + 1) * columns + column];
                var leftColumn = Math.Max(0, column - 1);
                var rightColumn = Math.Min(columns - 1, column + 1);
                var topRow = Math.Max(0, row - 1);
                var bottomRow = Math.Min(rows - 1, row + 1);
                var dxDivisor = xDistances[rightColumn] - xDistances[leftColumn];
                var dzDivisor = zDistances[bottomRow] - zDistances[topRow];
                var derivativeX = (right - left) / dxDivisor;
                var derivativeZ = (top - bottom) / dzDivisor;
                var normal = Vector3.Normalize(new Vector3(-derivativeX, 1f, -derivativeZ));
                var tangent3 = Vector3.Normalize(new Vector3(1f, derivativeX, 0f));
                normals[index] = normal;
                tangents[index] = new Vector4(tangent3, 1f);
            }
        }

        var indices = new uint[(columns - 1) * (rows - 1) * 6];
        var target = 0;
        for (var row = 0; row < rows - 1; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var column = 0; column < columns - 1; column++)
            {
                var topLeft = (uint)(row * columns + column);
                var topRight = topLeft + 1;
                var bottomLeft = topLeft + (uint)columns;
                var bottomRight = bottomLeft + 1;
                indices[target++] = topLeft;
                indices[target++] = topRight;
                indices[target++] = bottomLeft;
                indices[target++] = topRight;
                indices[target++] = bottomRight;
                indices[target++] = bottomLeft;
            }
        }

        return new MeshData
        {
            Name = "Textured relief face",
            MaterialIndex = 0,
            Positions = positions,
            Normals = normals,
            TextureCoordinates = textureCoordinates,
            Tangents = tangents,
            Indices = indices
        };
    }

    private static MeshData CreateShellMesh(
        float[] heights,
        float width,
        float height,
        float[] xDistances,
        float[] zDistances,
        CancellationToken cancellationToken)
    {
        var columns = xDistances.Length;
        var rows = zDistances.Length;
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var textureCoordinates = new List<Vector2>();
        var tangents = new List<Vector4>();
        var indices = new List<uint>();

        for (var column = 0; column < columns - 1; column++)
            AddSideSegment(0, column, 0, column + 1, Vector3.UnitZ);
        for (var row = 0; row < rows - 1; row++)
            AddSideSegment(row, columns - 1, row + 1, columns - 1, Vector3.UnitX);
        for (var column = columns - 1; column > 0; column--)
            AddSideSegment(rows - 1, column, rows - 1, column - 1, -Vector3.UnitZ);
        for (var row = rows - 1; row > 0; row--)
            AddSideSegment(row, 0, row - 1, 0, -Vector3.UnitX);

        var perimeter = BuildPerimeter(columns, rows);
        var bottomStart = (uint)positions.Count;
        foreach (var (row, column) in perimeter)
            AddVertex(PositionAt(row, column) with { Y = 0f }, -Vector3.UnitY, Vector2.Zero, Vector3.UnitX);
        var centerIndex = (uint)positions.Count;
        AddVertex(new Vector3(0f, 0f, 0f), -Vector3.UnitY, new Vector2(0.5f), Vector3.UnitX);
        for (var index = 0; index < perimeter.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = bottomStart + (uint)index;
            var next = bottomStart + (uint)((index + 1) % perimeter.Count);
            indices.Add(centerIndex);
            indices.Add(next);
            indices.Add(current);
        }

        return new MeshData
        {
            Name = "Relief shell",
            MaterialIndex = 1,
            Positions = [.. positions],
            Normals = [.. normals],
            TextureCoordinates = [.. textureCoordinates],
            Tangents = [.. tangents],
            Indices = [.. indices]
        };

        void AddSideSegment(int firstRow, int firstColumn, int secondRow, int secondColumn, Vector3 normal)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var topFirst = PositionAt(firstRow, firstColumn);
            var topSecond = PositionAt(secondRow, secondColumn);
            var start = (uint)positions.Count;
            AddVertex(topFirst, normal, new Vector2(0f, 1f), Vector3.UnitX);
            AddVertex(topFirst with { Y = 0f }, normal, new Vector2(0f, 0f), Vector3.UnitX);
            AddVertex(topSecond, normal, new Vector2(1f, 1f), Vector3.UnitX);
            AddVertex(topSecond with { Y = 0f }, normal, new Vector2(1f, 0f), Vector3.UnitX);
            indices.AddRange([start, start + 1, start + 2, start + 2, start + 1, start + 3]);
        }

        Vector3 PositionAt(int row, int column)
        {
            var x = -width * 0.5f + xDistances[column];
            var z = height * 0.5f - zDistances[row];
            return new Vector3(x, heights[row * columns + column], z);
        }

        void AddVertex(Vector3 position, Vector3 normal, Vector2 uv, Vector3 tangent)
        {
            positions.Add(position);
            normals.Add(normal);
            textureCoordinates.Add(uv);
            tangents.Add(new Vector4(tangent, 1f));
        }
    }

    private static List<(int Row, int Column)> BuildPerimeter(int columns, int rows)
    {
        var perimeter = new List<(int Row, int Column)>(2 * columns + 2 * rows - 4);
        for (var column = 0; column < columns; column++) perimeter.Add((0, column));
        for (var row = 1; row < rows; row++) perimeter.Add((row, columns - 1));
        for (var column = columns - 2; column >= 0; column--) perimeter.Add((rows - 1, column));
        for (var row = rows - 2; row > 0; row--) perimeter.Add((row, 0));
        return perimeter;
    }

    private static SceneModel CreateModel(MeshData front, MeshData shell, string texturePath)
    {
        var model = new SceneModel
        {
            Name = "2.5D Relief",
            IsProcedural = true,
            SourceFilePath = null,
            Materials =
            [
                new PbrMaterial
                {
                    Name = "Original image",
                    BaseColor = Vector4.One,
                    Metallic = 0f,
                    Roughness = 0.68f,
                    DoubleSided = false,
                    Textures = new Dictionary<TextureSemantic, TextureSlot>
                    {
                        [TextureSemantic.BaseColor] = new()
                        {
                            Path = texturePath,
                            Enabled = true,
                            Wrap = TextureWrap.ClampToEdge,
                            Filter = TextureFilter.LinearMipmapLinear
                        }
                    }
                },
                new PbrMaterial
                {
                    Name = "Relief edge",
                    BaseColor = new Vector4(0.72f, 0.74f, 0.78f, 1f),
                    Metallic = 0.02f,
                    Roughness = 0.72f,
                    DoubleSided = false
                }
            ],
            Meshes = [front, shell],
            Nodes = [new SceneNode { Name = "Relief", MeshIndices = [0, 1] }],
            SourceMeshIndices = [0, 1],
            SourceMeshIndicesCaptured = true
        };
        model.CaptureMeshMaterialIndices();
        model.CaptureProceduralGeometry();
        return model;
    }

    private static int CountOpenGeometricEdges(IEnumerable<MeshData> meshes, CancellationToken cancellationToken)
    {
        var edgeCounts = new Dictionary<(PositionKey First, PositionKey Second), int>();
        foreach (var mesh in meshes)
        {
            for (var index = 0; index < mesh.Indices.Length; index += 3)
            {
                if ((index & 0x3FFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
                AddEdge(mesh.Positions[mesh.Indices[index]], mesh.Positions[mesh.Indices[index + 1]]);
                AddEdge(mesh.Positions[mesh.Indices[index + 1]], mesh.Positions[mesh.Indices[index + 2]]);
                AddEdge(mesh.Positions[mesh.Indices[index + 2]], mesh.Positions[mesh.Indices[index]]);
            }
        }
        return edgeCounts.Values.Count(count => count != 2);

        void AddEdge(Vector3 firstPosition, Vector3 secondPosition)
        {
            var first = PositionKey.From(firstPosition);
            var second = PositionKey.From(secondPosition);
            var edge = first.CompareTo(second) <= 0 ? (first, second) : (second, first);
            edgeCounts[edge] = edgeCounts.GetValueOrDefault(edge) + 1;
        }
    }

    private readonly record struct PositionKey(long X, long Y, long Z) : IComparable<PositionKey>
    {
        private const double Scale = 1_000_000d;

        public static PositionKey From(Vector3 position) => new(
            (long)Math.Round(position.X * Scale),
            (long)Math.Round(position.Y * Scale),
            (long)Math.Round(position.Z * Scale));

        public int CompareTo(PositionKey other)
        {
            var x = X.CompareTo(other.X);
            if (x != 0) return x;
            var y = Y.CompareTo(other.Y);
            return y != 0 ? y : Z.CompareTo(other.Z);
        }
    }
}
