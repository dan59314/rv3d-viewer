using System.Numerics;
using Rv3dViewer.CameraAnimationPlugin;
using Rv3dViewer.Core;
using Xunit;

namespace Rv3dViewer.Tests;

public sealed class CameraAnimationTests
{
    [Fact]
    public void Evaluate_ReturnsExactEndpointPositions()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Linear);

        var first = CameraAnimationEvaluator.Evaluate(document, 0d);
        var last = CameraAnimationEvaluator.Evaluate(document, 2d);

        Assert.Equal(new Vector3(0f, 0f, 10f), first.From);
        Assert.Equal(new Vector3(10f, 5f, 0f), last.From);
        Assert.Equal(40f, last.FieldOfViewDegrees);
    }

    [Fact]
    public void Evaluate_InterpolatesPositionFovAndValidOrientation()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Linear);

        var camera = CameraAnimationEvaluator.Evaluate(document, 1d);

        Assert.True(Vector3.Distance(camera.From, new Vector3(5f, 2.5f, 5f)) < 0.0001f);
        Assert.InRange(camera.FieldOfViewDegrees, 49.999f, 50.001f);
        Assert.True(float.IsFinite(camera.To.X) && float.IsFinite(camera.Up.Y));
        Assert.InRange(camera.Up.Length(), 0.999f, 1.001f);
        Assert.True(Math.Abs(Vector3.Dot(Vector3.Normalize(camera.To - camera.From), camera.Up)) < 0.0001f);
    }

    [Fact]
    public void Evaluate_HoldKeepsPreviousCameraUntilNextKeyframe()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Hold);

        var camera = CameraAnimationEvaluator.Evaluate(document, 1.999d);

        Assert.Equal(document.Keyframes[0].From, camera.From);
        Assert.Equal(document.Keyframes[0].To, camera.To);
    }

    [Fact]
    public void Evaluate_InterpolatesRollAcrossShortestAngle()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Linear);
        document.Keyframes[0].RollDegrees = 170f;
        document.Keyframes[1].RollDegrees = -170f;

        var camera = CameraAnimationEvaluator.Evaluate(document, 1d);

        Assert.Equal(-180f, camera.RollDegrees);
    }

    [Fact]
    public void EvaluateLights_InterpolatesContinuousLightParameters()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Linear);
        var lightId = Guid.NewGuid();
        document.Keyframes[0].Lights = [new AnimationLightState
        {
            Id = lightId, Name = "Key", Position = Vector3.Zero, Color = Vector3.One,
            Intensity = 2f, Range = 10f, Direction = -Vector3.UnitY
        }];
        document.Keyframes[1].Lights = [new AnimationLightState
        {
            Id = lightId, Name = "Key", Position = new Vector3(10f, 4f, 2f),
            Color = new Vector3(1f, 0f, 0f), Intensity = 6f, Range = 30f,
            Direction = Vector3.UnitX
        }];

        var light = Assert.Single(CameraAnimationEvaluator.EvaluateLights(document, 1d)!);

        Assert.Equal(new Vector3(5f, 2f, 1f), light.Position);
        Assert.Equal(4f, light.Intensity);
        Assert.Equal(20f, light.Range);
        Assert.Equal(new Vector3(1f, 0.5f, 0.5f), light.Color);
        Assert.True(Vector3.Distance(light.Direction, Vector3.Normalize(new Vector3(1f, -1f, 0f))) < 0.0001f);
    }

    [Fact]
    public void Validate_RepairsDirectionalLightTrackIdsAcrossKeyframes()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Linear);
        document.Keyframes[0].Lights = [new AnimationLightState
        {
            Id = Guid.NewGuid(), Name = "Directional", Type = SceneLightType.Directional,
            Direction = -Vector3.UnitY
        }];
        document.Keyframes[1].Lights = [new AnimationLightState
        {
            Id = Guid.NewGuid(), Name = "Directional", Type = SceneLightType.Directional,
            Direction = Vector3.UnitX
        }];

        document.Validate();

        Assert.Equal(document.Keyframes[0].Lights![0].Id, document.Keyframes[1].Lights![0].Id);
        Assert.Equal(Vector3.UnitX, document.Keyframes[1].Lights![0].Direction);
    }

    [Fact]
    public async Task Serializer_RoundTripsDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), $"camera-animation-{Guid.NewGuid():N}.json");
        try
        {
            var source = CreateTwoFrameDocument(CameraEasing.EaseInOut);
            source.Name = "測試動畫";
            source.Keyframes[0].Lights = [new AnimationLightState
            {
                Name = "Key", Type = SceneLightType.Spot, Position = new Vector3(1f, 2f, 3f),
                Direction = -Vector3.UnitY, Intensity = 8f
            }];

            await CameraAnimationSerializer.SaveAsync(source, path);
            var loaded = await CameraAnimationSerializer.LoadAsync(path);

            Assert.Equal(source.Name, loaded.Name);
            Assert.Equal(2, loaded.Keyframes.Count);
            Assert.Equal(source.Keyframes[1].From, loaded.Keyframes[1].From);
            Assert.Equal(source.Keyframes[1].RollDegrees, loaded.Keyframes[1].RollDegrees);
            Assert.Equal(CameraEasing.EaseInOut, loaded.Keyframes[1].Easing);
            Assert.Equal(8f, Assert.Single(loaded.Keyframes[0].Lights!).Intensity);
            Assert.Equal(-Vector3.UnitY, loaded.Keyframes[0].Lights![0].Direction);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Validate_RejectsUnknownDocumentVersion()
    {
        var document = new CameraAnimationDocument { Version = 999 };
        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Fact]
    public void Validate_MigratesVersionOneOutgoingEasingToIncomingEasing()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Linear);
        document.Version = 1;
        document.Keyframes[0].Easing = CameraEasing.Hold;
        document.Keyframes[1].Easing = CameraEasing.EaseOut;

        document.Validate();

        Assert.Equal(CameraAnimationDocument.CurrentVersion, document.Version);
        Assert.Equal(CameraEasing.Hold, document.Keyframes[1].Easing);
    }

    [Fact]
    public void Validate_RejectsNonFiniteKeyframeValues()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Linear);
        document.Keyframes[0].From = new Vector3(float.NaN, 0f, 1f);

        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Fact]
    public void FrameSequencePlan_UsesExactFrameTimesAndIncludesEndpoint()
    {
        var plan = CameraFrameSequencePlan.Create(2d, 30);

        Assert.Equal(61, plan.FrameCount);
        Assert.Equal(0d, plan.GetTimeSeconds(0));
        Assert.Equal(1d, plan.GetTimeSeconds(30));
        Assert.Equal(2d, plan.GetTimeSeconds(60));
    }

    [Fact]
    public void FrameSequencePlan_IncludesNonFrameAlignedDurationAsLastFrame()
    {
        var plan = CameraFrameSequencePlan.Create(1.05d, 30);

        Assert.Equal(33, plan.FrameCount);
        Assert.Equal(1.05d, plan.GetTimeSeconds(32));
    }

    [Fact]
    public void FrameSequencePlan_UsesStableZeroPaddedPngNames()
    {
        var plan = CameraFrameSequencePlan.Create(2d, 30);

        Assert.Equal("frame_000000.png", plan.GetFileName(0));
        Assert.Equal("frame_000060.png", plan.GetFileName(60));
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.GetFileName(61));
    }

    [Fact]
    public void KeyframeInsertionPlan_UsesPreviousAndSelectedMidpoint()
    {
        var document = CreateTwoFrameDocument(CameraEasing.EaseOut);

        var plan = CameraKeyframeInsertionPlan.Create(document, document.Keyframes[1]);

        Assert.Equal(1d, plan.TimeSeconds);
        Assert.Equal(CameraEasing.EaseOut, plan.Easing);
    }

    [Fact]
    public void KeyframeInsertionPlan_RejectsFirstKeyframe()
    {
        var document = CreateTwoFrameDocument(CameraEasing.Linear);

        Assert.Throws<InvalidOperationException>(() =>
            CameraKeyframeInsertionPlan.Create(document, document.Keyframes[0]));
    }

    private static CameraAnimationDocument CreateTwoFrameDocument(CameraEasing easing) => new()
    {
        DurationSeconds = 2d,
        Keyframes =
        [
            new CameraKeyframe
            {
                Name = "Start",
                TimeSeconds = 0d,
                From = new Vector3(0f, 0f, 10f),
                To = Vector3.Zero,
                Up = Vector3.UnitY,
                FieldOfViewDegrees = 60f,
                Easing = CameraEasing.EaseInOut
            },
            new CameraKeyframe
            {
                Name = "End",
                TimeSeconds = 2d,
                From = new Vector3(10f, 5f, 0f),
                To = Vector3.Zero,
                Up = Vector3.UnitY,
                FieldOfViewDegrees = 40f,
                Easing = easing
            }
        ]
    };
}
