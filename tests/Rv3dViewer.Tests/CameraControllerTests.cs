using System.Numerics;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class CameraControllerTests
{
    [Fact]
    public void Orbit_PreservesDistanceAndTarget()
    {
        var camera = new CameraState { From = new Vector3(0, 0, 10), To = Vector3.Zero };
        CameraController.Orbit(camera, 0.5f, 0.25f);
        Assert.Equal(Vector3.Zero, camera.To);
        Assert.InRange(Vector3.Distance(camera.From, camera.To), 9.999f, 10.001f);
    }

    [Fact]
    public void AdjustRoll_PreservesFromAndToAndUpdatesUp()
    {
        var camera = new CameraState { From = new Vector3(0, 0, 10), To = Vector3.Zero };
        var originalFrom = camera.From;
        var originalTo = camera.To;

        CameraController.AdjustRoll(camera, 45f);

        Assert.Equal(originalFrom, camera.From);
        Assert.Equal(originalTo, camera.To);
        Assert.Equal(45f, camera.RollDegrees);
        Assert.True(Vector3.Distance(camera.Up, Vector3.UnitY) > 0.1f);
        Assert.True(Math.Abs(Vector3.Dot(Vector3.Normalize(camera.To - camera.From), camera.Up)) < 0.0001f);
    }

    [Fact]
    public void UpdateUpFromRoll_RotatesUpAroundViewingAxis()
    {
        var camera = new CameraState
        {
            From = new Vector3(0, 0, 10),
            To = Vector3.Zero,
            RollDegrees = 90f
        };

        CameraController.UpdateUpFromRoll(camera);

        Assert.True(Vector3.Dot(camera.Up, Vector3.UnitX) > 0.999f);
        Assert.True(Math.Abs(Vector3.Dot(Vector3.Normalize(camera.To - camera.From), camera.Up)) < 0.0001f);
    }

    [Theory]
    [InlineData(540f, -180f)]
    [InlineData(-540f, -180f)]
    [InlineData(370f, 10f)]
    public void RollDegrees_NormalizesToSignedRange(float value, float expected)
    {
        var camera = new CameraState { RollDegrees = value };

        Assert.Equal(expected, camera.RollDegrees);
    }

    [Fact]
    public void Pan_MovesFromAndToBySameDelta()
    {
        var camera = CameraState.Default;
        var before = camera.From - camera.To;
        CameraController.Pan(camera, 2f, -1f);
        Assert.True(Vector3.Distance(before, camera.From - camera.To) < 0.0001f);
    }

    [Fact]
    public void PanTarget_MovesOnlyTarget()
    {
        var camera = CameraState.Default;
        var originalFrom = camera.From;
        var originalTarget = camera.To;

        CameraController.PanTarget(camera, 2f, -1f);

        Assert.Equal(originalFrom, camera.From);
        Assert.NotEqual(originalTarget, camera.To);
    }

    [Theory]
    [InlineData(60f, -2f, 58f)]
    [InlineData(6f, -10f, 5f)]
    [InlineData(118f, 10f, 120f)]
    public void AdjustFieldOfView_ChangesAndClampsFov(float initial, float delta, float expected)
    {
        var camera = CameraState.Default;
        camera.FieldOfViewDegrees = initial;

        CameraController.AdjustFieldOfView(camera, delta);

        Assert.Equal(expected, camera.FieldOfViewDegrees);
    }

    [Fact]
    public void Dolly_NeverCrossesTarget()
    {
        var camera = new CameraState { From = new Vector3(0, 0, 1), To = Vector3.Zero };
        CameraController.Dolly(camera, -100f);
        Assert.True(Vector3.Distance(camera.From, camera.To) >= 0.0009f);
    }

    [Theory]
    [InlineData(CameraView.Front, 0f, 0f, 10f)]
    [InlineData(CameraView.Back, 0f, 0f, -10f)]
    [InlineData(CameraView.Left, -10f, 0f, 0f)]
    [InlineData(CameraView.Right, 10f, 0f, 0f)]
    [InlineData(CameraView.Top, 0f, 10f, 0f)]
    [InlineData(CameraView.Bottom, 0f, -10f, 0f)]
    public void SetView_UsesCanonicalDirectionAndPreservesDistance(CameraView view, float x, float y, float z)
    {
        var camera = new CameraState { From = new Vector3(0, 0, 10), To = Vector3.Zero };

        CameraController.SetView(camera, view);

        Assert.True(Vector3.Distance(camera.From, new Vector3(x, y, z)) < 0.0001f);
        Assert.InRange(Vector3.Distance(camera.From, camera.To), 9.999f, 10.001f);
        Assert.True(Math.Abs(Vector3.Dot(Vector3.Normalize(camera.To - camera.From), camera.Up)) < 0.0001f);
    }
}
