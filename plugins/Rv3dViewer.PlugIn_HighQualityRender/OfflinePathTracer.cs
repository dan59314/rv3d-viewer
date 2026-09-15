using System.Drawing.Imaging;
using System.Numerics;
using Rv3dViewer.Core;
using Rv3dViewer.Rendering.OpenGL;

namespace Rv3dViewer.HighQualityRenderPlugin;

internal static class OfflinePathTracer
{
    private const float Pi = MathF.PI;

    public static Task<bool> RenderToPngAsync(
        RenderSceneSnapshot scene,
        RenderOptions options,
        IProgress<RenderProgress>? progress,
        CancellationToken cancellationToken) => Task.Run(() =>
    {
        if (scene.Triangles.Length == 0) throw new InvalidOperationException("場景中沒有可 Render 的三角形。");
        if (cancellationToken.IsCancellationRequested) return false;
        using var textures = TextureLibrary.Load(scene, options.TextureQuality, options.UseHdriImportanceSampling);
        if (cancellationToken.IsCancellationRequested) return false;
        var accelerator = new Bvh(scene.Triangles);
        if (cancellationToken.IsCancellationRequested) return false;
        var pixels = Render(scene, accelerator, textures, options, progress, cancellationToken);
        if (pixels is null || cancellationToken.IsCancellationRequested) return false;
        SavePng(pixels, options.Width, options.Height, options.OutputPath);
        return true;
    });

    private static byte[]? Render(
        RenderSceneSnapshot scene,
        Bvh accelerator,
        TextureLibrary textures,
        RenderOptions options,
        IProgress<RenderProgress>? progress,
        CancellationToken cancellationToken)
    {
        var pixels = new byte[checked(options.Width * options.Height * 4)];
        var forward = RenderSceneSnapshot.SafeNormalize(scene.Camera.To - scene.Camera.From, -Vector3.UnitZ);
        var right = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(forward, scene.Camera.Up), Vector3.UnitX);
        var up = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(right, forward), Vector3.UnitY);
        var aspect = options.Width / (float)options.Height;
        var halfHeight = MathF.Tan(scene.Camera.FieldOfViewDegrees * Pi / 360F);
        var completedRows = 0;
        var renderWorkerCount = GetRenderWorkerCount(Environment.ProcessorCount);
        var parallelOptions = new ParallelOptions
        {
            // Reserve roughly one quarter of the logical processors so the
            // modeless Render window and MainForm remain responsive.
            MaxDegreeOfParallelism = renderWorkerCount
        };

        Parallel.For(0, options.Height, parallelOptions, (y, loopState) =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                loopState.Stop();
                return;
            }

            for (var x = 0; x < options.Width; x++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    loopState.Stop();
                    return;
                }
                var seed = unchecked((uint)(x * 1973 + y * 9277 + options.Width * 26699) | 1U);
                var random = new FastRandom(seed);
                var accumulated = Vector3.Zero;
                var alpha = 0F;
                var samplesTaken = 0;
                var luminanceSum = 0F;
                var luminanceSquaredSum = 0F;
                for (var sample = 0; sample < options.SamplesPerPixel; sample++)
                {
                    if ((sample & 15) == 0 && cancellationToken.IsCancellationRequested)
                    {
                        loopState.Stop();
                        return;
                    }
                    var sx = (x + random.NextFloat()) / options.Width;
                    var sy = (y + random.NextFloat()) / options.Height;
                    var px = (2F * sx - 1F) * aspect * halfHeight;
                    var py = (1F - 2F * sy) * halfHeight;
                    var ray = new Ray(scene.Camera.From, Vector3.Normalize(forward + right * px + up * py));
                    var sampleColor = Trace(ray, scene, accelerator, textures, options, ref random, out var primaryHit);
                    sampleColor = ClampLuminance(sampleColor, options.FireflyClamp);
                    accumulated += sampleColor;
                    alpha += options.TransparentBackground && !primaryHit ? 0F : 1F;
                    samplesTaken++;
                    var luminance = Luminance(sampleColor);
                    luminanceSum += luminance;
                    luminanceSquaredSum += luminance * luminance;
                    if (options.AdaptiveSampling && samplesTaken >= 32 && (samplesTaken & 7) == 0)
                    {
                        var mean = luminanceSum / samplesTaken;
                        var variance = MathF.Max(0F, luminanceSquaredSum / samplesTaken - mean * mean);
                        var standardError = MathF.Sqrt(variance / samplesTaken);
                        var convergenceLimit = MathF.Max(0.003F, 0.015F * MathF.Max(mean, 0.1F));
                        if (standardError <= convergenceLimit) break;
                    }
                }

                var color = accumulated / Math.Max(1, samplesTaken);
                color = RenderColorPipeline.EncodeDisplay(color);
                var offset = (y * options.Width + x) * 4;
                pixels[offset] = ToByte(color.Z);
                pixels[offset + 1] = ToByte(color.Y);
                pixels[offset + 2] = ToByte(color.X);
                pixels[offset + 3] = ToByte(alpha / Math.Max(1, samplesTaken));
            }

            var done = Interlocked.Increment(ref completedRows);
            if (done == options.Height || done % Math.Max(1, options.Height / 100) == 0)
                progress?.Report(new RenderProgress(done, options.Height));
        });
        return cancellationToken.IsCancellationRequested ? null : pixels;
    }

    private static Vector3 Trace(
        Ray initialRay,
        RenderSceneSnapshot scene,
        Bvh accelerator,
        TextureLibrary textures,
        RenderOptions options,
        ref FastRandom random,
        out bool primaryHit)
    {
        var radiance = Vector3.Zero;
        var throughput = Vector3.One;
        var ray = initialRay;
        primaryHit = false;
        var previousBsdfPdf = 0F;
        var previousWasDelta = true;
        Span<RenderMedium> mediumStorage = stackalloc RenderMedium[8];
        var media = new RenderMediumStack(mediumStorage);

        for (var bounce = 0; bounce < options.MaximumBounces; bounce++)
        {
            if (!accelerator.TryIntersect(ray, float.PositiveInfinity, out var hit))
            {
                var environment = SampleEnvironment(ray.Direction, scene, textures, options.UseEnvironment, bounce == 0);
                var misWeight = bounce > 0 && !previousWasDelta
                    ? PowerHeuristic(previousBsdfPdf, textures.EnvironmentPdf(ray.Direction, scene.Environment.RotationRadians))
                    : 1F;
                radiance += throughput * environment * misWeight;
                break;
            }

            if (bounce == 0) primaryHit = true;
            var triangle = scene.Triangles[hit.TriangleIndex];
            var rayEpsilon = RenderGeometryPrecision.RayOffset(hit.Position, triangle);
            var material = scene.Materials[triangle.MaterialIndex];
            var surface = EvaluateSurface(triangle, material, hit, textures);
            throughput *= media.SegmentTransmittance(hit.Distance);
            if (Math.Max(throughput.X, Math.Max(throughput.Y, throughput.Z)) < 0.000001F) break;

            if (material.RenderMode == MaterialRenderMode.Cutout && surface.Opacity < material.AlphaCutoff)
            {
                ray = new Ray(hit.Position + ray.Direction * rayEpsilon, ray.Direction);
                bounce--;
                continue;
            }
            if (material.RenderMode is MaterialRenderMode.Auto or MaterialRenderMode.Transparent && surface.Opacity < 0.999F && random.NextFloat() > surface.Opacity)
            {
                ray = new Ray(hit.Position + ray.Direction * rayEpsilon, ray.Direction);
                bounce--;
                continue;
            }

            var frontFace = Vector3.Dot(ray.Direction, hit.GeometricNormal) < 0F;
            var normal = Vector3.Dot(surface.Normal, hit.GeometricNormal) < 0F ? -surface.Normal : surface.Normal;
            if (!frontFace) normal = -normal;
            var view = -ray.Direction;
            radiance += throughput * surface.Emissive;

            if (material.RenderMode == MaterialRenderMode.Glass)
            {
                if (options.GlassQuality == RenderGlassQuality.Fast)
                {
                    throughput *= RenderOptics.SurfaceTransmission(surface.BaseColor, material.Transmission);
                    ray = new Ray(hit.Position + ray.Direction * rayEpsilon, ray.Direction);
                    bounce--;
                    continue;
                }
                // Glass has no diffuse lobe. Only its dielectric specular lobe may
                // reflect direct lights; transmitted radiance arrives through the
                // refracted continuation ray below.
                var incidentIor = media.IncidentIndexOfRefraction(
                    triangle.MaterialIndex, frontFace, material.IndexOfRefraction);
                var transmittedIor = media.TransmittedIndexOfRefraction(
                    triangle.MaterialIndex, frontFace, material.IndexOfRefraction);
                var boundaryF0 = RenderOptics.DielectricFresnel(1F, incidentIor, transmittedIor);
                var deltaGlass = RenderOptics.IsDeltaGlass(surface.Roughness);
                if (!deltaGlass)
                    radiance += throughput * EvaluateDirectLighting(
                        hit.Position, normal, view, surface, scene, accelerator, textures, ref random,
                        options.UseEnvironment, rayEpsilon, options.GlassQuality, dielectricF0: boundaryF0);
                var microfacetNormal = deltaGlass
                    ? normal
                    : SampleGgxHalfVector(normal, surface.Roughness, ref random);
                if (Vector3.Dot(view, microfacetNormal) < 0F) microfacetNormal = -microfacetNormal;
                var eta = RenderOptics.RelativeIndexOfRefraction(incidentIor, transmittedIor);
                var cosTheta = Math.Clamp(Vector3.Dot(view, microfacetNormal), 0F, 1F);
                var reflectProbability = RenderOptics.DielectricFresnel(cosTheta, incidentIor, transmittedIor);
                var cannotRefract = eta * MathF.Sqrt(MathF.Max(0F, 1F - cosTheta * cosTheta)) > 1F;
                var reflected = cannotRefract || random.NextFloat() < reflectProbability;
                var direction = reflected
                    ? Vector3.Reflect(ray.Direction, microfacetNormal)
                    : Refract(ray.Direction, microfacetNormal, eta);
                direction = RenderSceneSnapshot.SafeNormalize(direction, Vector3.Reflect(ray.Direction, normal));
                if (!reflected)
                {
                    throughput *= RenderOptics.SurfaceTransmission(surface.BaseColor, material.Transmission);
                    media.CrossBoundary(triangle.MaterialIndex, frontFace, material);
                }
                ray = new Ray(hit.Position + direction * rayEpsilon, Vector3.Normalize(direction));
                previousWasDelta = true;
                previousBsdfPdf = 0F;
            }
            else
            {
                radiance += throughput * EvaluateDirectLighting(
                    hit.Position, normal, view, surface, scene, accelerator, textures, ref random,
                    options.UseEnvironment, rayEpsilon, options.GlassQuality, dielectricF0: null);
                var metallic = surface.Metallic;
                var specularProbability = 0.15F + metallic * 0.75F;
                var diffuseProbability = Math.Max(1F - specularProbability, 0.05F);
                var chooseSpecular = random.NextFloat() < specularProbability;
                Vector3 direction;
                if (chooseSpecular)
                {
                    var halfVector = SampleGgxHalfVector(normal, surface.Roughness, ref random);
                    direction = Vector3.Reflect(ray.Direction, halfVector);
                    var ndv = MathF.Max(Vector3.Dot(normal, view), 0.0001F);
                    var ndl = MathF.Max(Vector3.Dot(normal, direction), 0F);
                    var ndh = MathF.Max(Vector3.Dot(normal, halfVector), 0.0001F);
                    var vdh = MathF.Max(Vector3.Dot(view, halfVector), 0.0001F);
                    if (ndl <= 0F)
                    {
                        direction = Vector3.Reflect(ray.Direction, normal);
                        halfVector = normal;
                        ndl = MathF.Max(Vector3.Dot(normal, direction), 0.0001F);
                        ndh = 1F;
                        vdh = ndv;
                    }
                    var f0 = Vector3.Lerp(new Vector3(0.04F), surface.BaseColor, metallic);
                    var fresnel = FresnelSchlick(vdh, f0);
                    var geometry = GeometrySmith(normal, view, direction, surface.Roughness);
                    throughput *= fresnel * (geometry * vdh / Math.Max(ndv * ndh * specularProbability, 0.0001F));
                }
                else
                {
                    direction = CosineHemisphere(normal, ref random);
                    var halfVector = RenderSceneSnapshot.SafeNormalize(view + direction, normal);
                    var f0 = Vector3.Lerp(new Vector3(0.04F), surface.BaseColor, metallic);
                    var fresnel = FresnelSchlick(MathF.Max(Vector3.Dot(view, halfVector), 0F), f0);
                    throughput *= (Vector3.One - fresnel) * surface.BaseColor * (1F - metallic) *
                        surface.AmbientOcclusion / diffuseProbability;
                }
                var sampledHalfVector = RenderSceneSnapshot.SafeNormalize(view + direction, normal);
                var sampledNdh = MathF.Max(Vector3.Dot(normal, sampledHalfVector), 0F);
                var sampledVdh = MathF.Max(Vector3.Dot(view, sampledHalfVector), 0.0001F);
                var specularPdf = specularProbability * DistributionGgx(normal, sampledHalfVector, surface.Roughness) *
                    sampledNdh / Math.Max(4F * sampledVdh, 0.0001F);
                var diffusePdf = diffuseProbability * MathF.Max(Vector3.Dot(normal, direction), 0F) / Pi;
                previousBsdfPdf = specularPdf + diffusePdf;
                previousWasDelta = false;
                ray = new Ray(hit.Position + normal * rayEpsilon, direction);
            }

            if (bounce >= 2)
            {
                var survival = Math.Clamp(Math.Max(throughput.X, Math.Max(throughput.Y, throughput.Z)), 0.08F, 0.95F);
                if (random.NextFloat() > survival) break;
                throughput /= survival;
            }
        }
        return radiance;
    }

    private static Vector3 ClampLuminance(Vector3 color, float maximumLuminance)
    {
        if (maximumLuminance <= 0F) return color;
        var luminance = Luminance(color);
        return luminance > maximumLuminance
            ? color * (maximumLuminance / MathF.Max(luminance, 0.000001F))
            : color;
    }

    private static float Luminance(Vector3 color) =>
        color.X * 0.2126F + color.Y * 0.7152F + color.Z * 0.0722F;

    private static Surface EvaluateSurface(RenderTriangle triangle, RenderMaterial material, Hit hit, TextureLibrary textures)
    {
        var w = 1F - hit.U - hit.V;
        var uv = triangle.Uv0 * w + triangle.Uv1 * hit.U + triangle.Uv2 * hit.V;
        var normal = RenderSceneSnapshot.SafeNormalize(triangle.N0 * w + triangle.N1 * hit.U + triangle.N2 * hit.V, hit.GeometricNormal);
        var interpolatedTangent = RenderSceneSnapshot.SafeNormalize(
            triangle.T0 * w + triangle.T1 * hit.U + triangle.T2 * hit.V,
            RenderSceneSnapshot.OrthonormalTangent(normal));
        var baseSample = textures.Sample(triangle.MaterialIndex, TextureSemantic.BaseColor, uv, Vector4.One, srgb: true);
        var baseColor = Vector3.Clamp(new Vector3(material.BaseColor.X, material.BaseColor.Y, material.BaseColor.Z) * new Vector3(baseSample.X, baseSample.Y, baseSample.Z), Vector3.Zero, new Vector3(16F));
        var metallic = Math.Clamp(material.Metallic * textures.SampleScalar(triangle.MaterialIndex, TextureSemantic.Metallic, uv), 0F, 1F);
        var roughness = Math.Clamp(material.Roughness * textures.SampleScalar(triangle.MaterialIndex, TextureSemantic.Roughness, uv), 0.001F, 1F);
        var ao = Math.Clamp(material.AmbientOcclusion * textures.SampleScalar(triangle.MaterialIndex, TextureSemantic.AmbientOcclusion, uv), 0F, 1F);
        var opacity = Math.Clamp(material.Opacity * material.BaseColor.W * baseSample.W * textures.SampleScalar(triangle.MaterialIndex, TextureSemantic.Opacity, uv), 0F, 1F);
        var emissiveSample = textures.Sample(triangle.MaterialIndex, TextureSemantic.Emissive, uv, Vector4.One, srgb: true);
        var emissive = material.Emissive * new Vector3(emissiveSample.X, emissiveSample.Y, emissiveSample.Z);

        if (textures.Has(triangle.MaterialIndex, TextureSemantic.Normal))
        {
            var sample = textures.Sample(triangle.MaterialIndex, TextureSemantic.Normal, uv, new Vector4(0.5F, 0.5F, 1F, 1F), srgb: false);
            var mapped = RenderSceneSnapshot.SafeNormalize(new Vector3((sample.X * 2F - 1F) * material.NormalScale, (sample.Y * 2F - 1F) * material.NormalScale, sample.Z * 2F - 1F), Vector3.UnitZ);
            var tangent = RenderSceneSnapshot.SafeNormalize(interpolatedTangent - normal * Vector3.Dot(interpolatedTangent, normal), RenderSceneSnapshot.OrthonormalTangent(normal));
            var bitangent = Vector3.Normalize(Vector3.Cross(normal, tangent));
            normal = Vector3.Normalize(tangent * mapped.X + bitangent * mapped.Y + normal * mapped.Z);
        }
        if (textures.Has(triangle.MaterialIndex, TextureSemantic.Bump))
        {
            const float delta = 1F / 1024F;
            var height = textures.SampleScalar(triangle.MaterialIndex, TextureSemantic.Bump, uv);
            var dx = textures.SampleScalar(triangle.MaterialIndex, TextureSemantic.Bump, uv + new Vector2(delta, 0F)) - height;
            var dy = textures.SampleScalar(triangle.MaterialIndex, TextureSemantic.Bump, uv + new Vector2(0F, delta)) - height;
            var tangent = RenderSceneSnapshot.SafeNormalize(interpolatedTangent - normal * Vector3.Dot(interpolatedTangent, normal), RenderSceneSnapshot.OrthonormalTangent(normal));
            var bitangent = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(normal, tangent), Vector3.UnitY);
            normal = RenderSceneSnapshot.SafeNormalize(normal - tangent * dx * material.BumpScale - bitangent * dy * material.BumpScale, normal);
        }
        return new Surface(normal, baseColor, metallic, roughness, ao, emissive, opacity);
    }

    private static Vector3 EvaluateDirectLighting(
        Vector3 position,
        Vector3 normal,
        Vector3 view,
        Surface surface,
        RenderSceneSnapshot scene,
        Bvh accelerator,
        TextureLibrary textures,
        ref FastRandom random,
        bool useEnvironment,
        float rayEpsilon,
        RenderGlassQuality glassQuality,
        float? dielectricF0)
    {
        var dielectricOnly = dielectricF0.HasValue;
        var result = Vector3.Zero;
        foreach (var light in scene.Lights)
        {
            Vector3 direction;
            float distance;
            float attenuation;
            if (light.Type == SceneLightType.Directional)
            {
                direction = -light.Direction;
                distance = float.PositiveInfinity;
                attenuation = 1F;
            }
            else
            {
                var offset = light.Position - position;
                distance = offset.Length();
                if (distance <= rayEpsilon || distance >= light.Range) continue;
                direction = offset / distance;
                var normalizedDistance = distance / light.Range;
                attenuation = (1F - SmoothStep(0.85F, 1F, normalizedDistance)) / (1F + 4F * normalizedDistance * normalizedDistance);
                if (light.Type == SceneLightType.Spot)
                {
                    var coneCos = Vector3.Dot(-direction, light.Direction);
                    attenuation *= SmoothStep(light.OuterCos, light.InnerCos, coneCos);
                }
            }
            var ndl = MathF.Max(Vector3.Dot(normal, direction), 0F);
            if (ndl <= 0F || attenuation <= 0F) continue;
            var transmittance = TraceShadowTransmittance(
                new Ray(position + normal * rayEpsilon, direction),
                distance - rayEpsilon,
                scene,
                accelerator,
                textures, glassQuality);
            if (transmittance.LengthSquared() <= 0.000001F) continue;

            var halfVector = Vector3.Normalize(view + direction);
            var f0 = dielectricOnly
                ? new Vector3(dielectricF0!.Value)
                : Vector3.Lerp(new Vector3(0.04F), surface.BaseColor, surface.Metallic);
            var fresnel = FresnelSchlick(MathF.Max(Vector3.Dot(halfVector, view), 0F), f0);
            var distribution = DistributionGgx(normal, halfVector, surface.Roughness);
            var geometry = GeometrySmith(normal, view, direction, surface.Roughness);
            var denominator = Math.Max(4F * MathF.Max(Vector3.Dot(normal, view), 0F) * ndl, 0.001F);
            var specular = fresnel * (distribution * geometry / denominator);
            var diffuse = dielectricOnly
                ? Vector3.Zero
                : (Vector3.One - fresnel) * (1F - surface.Metallic) * surface.BaseColor / Pi;
            result += (diffuse + specular) * light.Color * transmittance * (light.Intensity * attenuation * ndl);
        }
        if (useEnvironment && scene.Environment.Enabled && textures.TrySampleEnvironmentImportance(
                random.NextFloat(), random.NextFloat(), random.NextFloat(), scene.Environment.RotationRadians, out var environmentSample))
        {
            var direction = environmentSample.Direction;
            var ndl = MathF.Max(Vector3.Dot(normal, direction), 0F);
            if (ndl > 0F && environmentSample.Pdf > 0F)
            {
                var transmittance = TraceShadowTransmittance(
                    new Ray(position + normal * rayEpsilon, direction), float.PositiveInfinity,
                    scene, accelerator, textures, glassQuality);
                if (transmittance.LengthSquared() > 0.000001F)
                {
                    var halfVector = RenderSceneSnapshot.SafeNormalize(view + direction, normal);
                    var f0 = dielectricOnly ? new Vector3(dielectricF0!.Value) :
                        Vector3.Lerp(new Vector3(0.04F), surface.BaseColor, surface.Metallic);
                    var fresnel = FresnelSchlick(MathF.Max(Vector3.Dot(halfVector, view), 0F), f0);
                    var distribution = DistributionGgx(normal, halfVector, surface.Roughness);
                    var geometry = GeometrySmith(normal, view, direction, surface.Roughness);
                    var specular = fresnel * (distribution * geometry /
                        Math.Max(4F * MathF.Max(Vector3.Dot(normal, view), 0F) * ndl, 0.001F));
                    var diffuse = dielectricOnly ? Vector3.Zero :
                        (Vector3.One - fresnel) * (1F - surface.Metallic) * surface.BaseColor / Pi;
                    var specularProbability = dielectricOnly ? 1F : 0.15F + surface.Metallic * 0.75F;
                    var bsdfPdf = specularProbability * distribution * MathF.Max(Vector3.Dot(normal, halfVector), 0F) /
                        Math.Max(4F * MathF.Max(Vector3.Dot(view, halfVector), 0.0001F), 0.0001F) +
                        (dielectricOnly ? 0F : 1F - specularProbability) * ndl / Pi;
                    var weight = PowerHeuristic(environmentSample.Pdf, bsdfPdf);
                    var environment = SampleEnvironment(direction, scene, textures, true, primary: false);
                    result += (diffuse + specular) * environment * transmittance *
                        (surface.AmbientOcclusion * ndl * weight / environmentSample.Pdf);
                }
            }
        }
        return result;
    }

    private static Vector3 TraceShadowTransmittance(
        Ray initialRay,
        float maximumDistance,
        RenderSceneSnapshot scene,
        Bvh accelerator,
        TextureLibrary textures,
        RenderGlassQuality glassQuality)
    {
        var ray = initialRay;
        var remaining = maximumDistance;
        var transmittance = Vector3.One;
        Span<RenderMedium> mediumStorage = stackalloc RenderMedium[8];
        var media = new RenderMediumStack(mediumStorage);
        for (var layer = 0; layer < 24; layer++)
        {
            if (!accelerator.TryIntersect(ray, remaining, out var hit)) return transmittance;
            var triangle = scene.Triangles[hit.TriangleIndex];
            var rayEpsilon = RenderGeometryPrecision.RayOffset(hit.Position, triangle);
            var material = scene.Materials[triangle.MaterialIndex];
            var surface = EvaluateSurface(triangle, material, hit, textures);
            transmittance *= media.SegmentTransmittance(hit.Distance);
            if (material.RenderMode == MaterialRenderMode.Glass)
            {
                var entering = Vector3.Dot(ray.Direction, hit.GeometricNormal) < 0F;
                transmittance *= RenderOptics.SurfaceTransmission(surface.BaseColor, material.Transmission);
                if (glassQuality == RenderGlassQuality.Physical)
                    media.CrossBoundary(triangle.MaterialIndex, entering, material);
            }
            else if (material.RenderMode is MaterialRenderMode.Transparent or MaterialRenderMode.Auto && surface.Opacity < 0.999F)
            {
                transmittance *= new Vector3(1F - surface.Opacity);
            }
            else if (material.RenderMode == MaterialRenderMode.Cutout && surface.Opacity < material.AlphaCutoff)
            {
                // Cutout hole does not attenuate the shadow ray.
            }
            else
            {
                return Vector3.Zero;
            }

            if (Math.Max(transmittance.X, Math.Max(transmittance.Y, transmittance.Z)) < 0.005F)
                return Vector3.Zero;
            var advance = hit.Distance + rayEpsilon;
            if (!float.IsPositiveInfinity(remaining))
            {
                remaining -= advance;
                if (remaining <= rayEpsilon) return transmittance;
            }
            ray = new Ray(hit.Position + ray.Direction * rayEpsilon, ray.Direction);
        }
        return transmittance;
    }

    private static Vector3 SampleEnvironment(Vector3 direction, RenderSceneSnapshot scene, TextureLibrary textures, bool useEnvironment, bool primary)
    {
        if (useEnvironment && scene.Environment.Enabled)
        {
            var environment = textures.SampleEnvironment(direction, scene.Environment.RotationRadians,
                primary ? scene.Environment.BackgroundBlur * textures.EnvironmentMaximumLod : 0F);
            if (environment.HasValue && (!primary || scene.Environment.ShowBackground))
                return environment.Value * scene.Environment.Intensity;
            if (!primary) return new Vector3(0.05F);
        }
        return primary
            ? new Vector3(scene.BackgroundColor.X, scene.BackgroundColor.Y, scene.BackgroundColor.Z)
            : new Vector3(0.05F);
    }

    private static float PowerHeuristic(float firstPdf, float secondPdf)
    {
        var firstSquared = firstPdf * firstPdf;
        var secondSquared = secondPdf * secondPdf;
        return firstSquared / Math.Max(firstSquared + secondSquared, 0.000001F);
    }

    private static float DistributionGgx(Vector3 n, Vector3 h, float roughness)
    {
        var a = roughness * roughness;
        var a2 = a * a;
        var ndh = MathF.Max(Vector3.Dot(n, h), 0F);
        var denominator = ndh * ndh * (a2 - 1F) + 1F;
        return a2 / Math.Max(Pi * denominator * denominator, 0.000001F);
    }

    private static float GeometrySmith(Vector3 n, Vector3 v, Vector3 l, float roughness)
    {
        var r = roughness + 1F;
        var k = r * r / 8F;
        static float Part(float nd, float kValue) => nd / Math.Max(nd * (1F - kValue) + kValue, 0.000001F);
        return Part(MathF.Max(Vector3.Dot(n, v), 0F), k) * Part(MathF.Max(Vector3.Dot(n, l), 0F), k);
    }

    private static Vector3 FresnelSchlick(float cosine, Vector3 f0) => f0 + (Vector3.One - f0) * MathF.Pow(Math.Clamp(1F - cosine, 0F, 1F), 5F);

    private static Vector3 CosineHemisphere(Vector3 normal, ref FastRandom random)
    {
        var r1 = 2F * Pi * random.NextFloat();
        var r2 = random.NextFloat();
        var r2s = MathF.Sqrt(r2);
        var tangent = RenderSceneSnapshot.OrthonormalTangent(normal);
        var bitangent = Vector3.Cross(normal, tangent);
        return Vector3.Normalize(tangent * (MathF.Cos(r1) * r2s) + bitangent * (MathF.Sin(r1) * r2s) + normal * MathF.Sqrt(1F - r2));
    }

    private static Vector3 SampleGgxHalfVector(Vector3 normal, float roughness, ref FastRandom random)
    {
        var alpha = Math.Max(roughness * roughness, 0.000001F);
        var phi = 2F * Pi * random.NextFloat();
        var xi = Math.Clamp(random.NextFloat(), 0F, 0.999999F);
        var cosTheta = MathF.Sqrt((1F - xi) / (1F + (alpha * alpha - 1F) * xi));
        var sinTheta = MathF.Sqrt(MathF.Max(0F, 1F - cosTheta * cosTheta));
        var tangent = RenderSceneSnapshot.OrthonormalTangent(normal);
        var bitangent = Vector3.Cross(normal, tangent);
        return Vector3.Normalize(
            tangent * (MathF.Cos(phi) * sinTheta) +
            bitangent * (MathF.Sin(phi) * sinTheta) +
            normal * cosTheta);
    }

    private static Vector3 Refract(Vector3 incident, Vector3 normal, float eta)
    {
        var cosTheta = MathF.Min(Vector3.Dot(-incident, normal), 1F);
        var perpendicular = eta * (incident + cosTheta * normal);
        var parallel = -MathF.Sqrt(MathF.Abs(1F - perpendicular.LengthSquared())) * normal;
        return perpendicular + parallel;
    }

    private static byte ToByte(float value) => (byte)Math.Clamp((int)MathF.Round(value * 255F), 0, 255);
    private static float SmoothStep(float edge0, float edge1, float value) { var t = Math.Clamp((value - edge0) / Math.Max(edge1 - edge0, 0.000001F), 0F, 1F); return t * t * (3F - 2F * t); }

    private static void SavePng(byte[] pixels, int width, int height, string outputPath)
    {
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            if (data.Stride == width * 4)
                System.Runtime.InteropServices.Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            else
                for (var y = 0; y < height; y++)
                    System.Runtime.InteropServices.Marshal.Copy(pixels, y * width * 4, data.Scan0 + y * data.Stride, width * 4);
        }
        finally { bitmap.UnlockBits(data); }
        bitmap.Save(outputPath, ImageFormat.Png);
    }

    internal static Vector3 ToneMapForTest(Vector3 value) => RenderColorPipeline.AcesToneMap(value);

    internal static bool IntersectForTest(RenderTriangle[] triangles, Vector3 origin, Vector3 direction,
        float maximumDistance, out float distance)
    {
        var found = new Bvh(triangles).TryIntersect(new Ray(origin, direction), maximumDistance, out var hit);
        distance = hit.Distance;
        return found;
    }
    internal static int GetRenderWorkerCount(int processorCount) =>
        Math.Max(1, (int)MathF.Floor(Math.Max(1, processorCount) * 0.75F));

    internal static void SaveLinearGpuPixels(float[] linearRgba, int width, int height, string outputPath)
    {
        var pixels = new byte[checked(width * height * 4)];
        for (var index = 0; index < width * height; index++)
        {
            var source = index * 4;
            var color = new Vector3(linearRgba[source], linearRgba[source + 1], linearRgba[source + 2]);
            color = RenderColorPipeline.EncodeDisplay(color);
            pixels[source] = ToByte(color.Z);
            pixels[source + 1] = ToByte(color.Y);
            pixels[source + 2] = ToByte(color.X);
            pixels[source + 3] = ToByte(linearRgba[source + 3]);
        }
        SavePng(pixels, width, height, outputPath);
    }

    private readonly record struct Surface(Vector3 Normal, Vector3 BaseColor, float Metallic, float Roughness, float AmbientOcclusion, Vector3 Emissive, float Opacity);
    private readonly record struct Ray(Vector3 Origin, Vector3 Direction);
    private readonly record struct Hit(int TriangleIndex, float Distance, float U, float V, Vector3 Position, Vector3 GeometricNormal);

    private struct FastRandom(uint state)
    {
        private uint _state = state == 0 ? 1U : state;
        public float NextFloat() { _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5; return (_state & 0x00FFFFFF) / 16777216F; }
    }

    private sealed class Bvh
    {
        private readonly RenderTriangle[] _triangles;
        private readonly int[] _indices;
        private readonly List<Node> _nodes = [];

        public Bvh(RenderTriangle[] triangles)
        {
            _triangles = triangles;
            _indices = Enumerable.Range(0, triangles.Length).ToArray();
            Build(0, _indices.Length);
        }

        public bool TryIntersect(Ray ray, float maximumDistance, out Hit hit)
        {
            hit = default;
            if (_nodes.Count == 0) return false;
            var closest = maximumDistance;
            var found = false;
            Span<int> stack = stackalloc int[128];
            var stackCount = 1;
            stack[0] = 0;
            while (stackCount > 0)
            {
                var nodeIndex = stack[--stackCount];
                var node = _nodes[nodeIndex];
                if (!node.Bounds.Intersects(ray, closest)) continue;
                if (node.Count > 0)
                {
                    for (var i = node.Start; i < node.Start + node.Count; i++)
                    {
                        var triangleIndex = _indices[i];
                        if (!IntersectTriangle(ray, _triangles[triangleIndex], closest, out var distance, out var u, out var v)) continue;
                        closest = distance;
                        var triangle = _triangles[triangleIndex];
                        var geometricNormal = RenderSceneSnapshot.SafeNormalize(Vector3.Cross(triangle.P1 - triangle.P0, triangle.P2 - triangle.P0), Vector3.UnitY);
                        hit = new Hit(triangleIndex, distance, u, v, ray.Origin + ray.Direction * distance, geometricNormal);
                        found = true;
                    }
                }
                else if (stackCount + 2 <= stack.Length)
                {
                    stack[stackCount++] = node.Left;
                    stack[stackCount++] = node.Right;
                }
            }
            return found;
        }

        private int Build(int start, int count)
        {
            var bounds = Bounds.Empty;
            var centroidBounds = Bounds.Empty;
            for (var i = start; i < start + count; i++)
            {
                var triangle = _triangles[_indices[i]];
                bounds = bounds.Include(triangle.P0).Include(triangle.P1).Include(triangle.P2);
                centroidBounds = centroidBounds.Include((triangle.P0 + triangle.P1 + triangle.P2) / 3F);
            }
            var nodeIndex = _nodes.Count;
            _nodes.Add(default);
            if (count <= 6)
            {
                _nodes[nodeIndex] = new Node(bounds, start, count, -1, -1);
                return nodeIndex;
            }
            var extent = centroidBounds.Maximum - centroidBounds.Minimum;
            var axis = extent.X >= extent.Y && extent.X >= extent.Z ? 0 : extent.Y >= extent.Z ? 1 : 2;
            Array.Sort(_indices, start, count, Comparer<int>.Create((a, b) => Axis(Centroid(_triangles[a]), axis).CompareTo(Axis(Centroid(_triangles[b]), axis))));
            var leftCount = count / 2;
            var left = Build(start, leftCount);
            var right = Build(start + leftCount, count - leftCount);
            _nodes[nodeIndex] = new Node(bounds, 0, 0, left, right);
            return nodeIndex;
        }

        private static Vector3 Centroid(RenderTriangle t) => (t.P0 + t.P1 + t.P2) / 3F;
        private static float Axis(Vector3 value, int axis) => axis == 0 ? value.X : axis == 1 ? value.Y : value.Z;

        private static bool IntersectTriangle(Ray ray, RenderTriangle triangle, float maximumDistance, out float distance, out float u, out float v)
        {
            var edge1 = triangle.P1 - triangle.P0;
            var edge2 = triangle.P2 - triangle.P0;
            var p = Vector3.Cross(ray.Direction, edge2);
            var determinant = Vector3.Dot(edge1, p);
            var areaScale = Vector3.Cross(edge1, edge2).Length();
            if (!float.IsFinite(determinant) || areaScale <= 0F || MathF.Abs(determinant) <= areaScale * 1e-7F)
            { distance = u = v = 0F; return false; }
            var inverse = 1F / determinant;
            var t = ray.Origin - triangle.P0;
            u = Vector3.Dot(t, p) * inverse;
            if (u < 0F || u > 1F) { distance = v = 0F; return false; }
            var q = Vector3.Cross(t, edge1);
            v = Vector3.Dot(ray.Direction, q) * inverse;
            if (v < 0F || u + v > 1F) { distance = 0F; return false; }
            distance = Vector3.Dot(edge2, q) * inverse;
            return float.IsFinite(distance) && distance > 0F && distance < maximumDistance;
        }

        private readonly record struct Node(Bounds Bounds, int Start, int Count, int Left, int Right);

        private readonly record struct Bounds(Vector3 Minimum, Vector3 Maximum)
        {
            public static Bounds Empty => new(new Vector3(float.PositiveInfinity), new Vector3(float.NegativeInfinity));
            public Bounds Include(Vector3 point) => new(Vector3.Min(Minimum, point), Vector3.Max(Maximum, point));
            public bool Intersects(Ray ray, float maximumDistance)
            {
                var minimum = 0F;
                var maximum = maximumDistance;
                for (var axis = 0; axis < 3; axis++)
                {
                    var origin = Axis(ray.Origin, axis);
                    var direction = Axis(ray.Direction, axis);
                    if (direction == 0F)
                    {
                        if (origin < Axis(Minimum, axis) || origin > Axis(Maximum, axis)) return false;
                        continue;
                    }
                    var inverse = 1F / direction;
                    var near = (Axis(Minimum, axis) - origin) * inverse;
                    var far = (Axis(Maximum, axis) - origin) * inverse;
                    if (near > far) (near, far) = (far, near);
                    minimum = MathF.Max(minimum, near);
                    maximum = MathF.Min(maximum, far);
                    if (maximum < minimum) return false;
                }
                return true;
            }
        }
    }

    private sealed class TextureLibrary : IDisposable
    {
        private readonly Dictionary<(int Material, TextureSemantic Semantic), TextureImage> _textures = [];
        private TextureImage? _environment;
        private EnvironmentImportanceSampler? _environmentImportance;

        public static TextureLibrary Load(RenderSceneSnapshot scene,
            RenderTextureQuality textureQuality = RenderTextureQuality.Original,
            bool useEnvironmentImportanceSampling = true)
        {
            var library = new TextureLibrary();
            var maximumDimension = textureQuality switch
            {
                RenderTextureQuality.Low => 512,
                RenderTextureQuality.Medium => 1024,
                _ => int.MaxValue
            };
            for (var materialIndex = 0; materialIndex < scene.Materials.Length; materialIndex++)
                foreach (var (semantic, path) in scene.Materials[materialIndex].TexturePaths)
                    if (TextureImage.TryLoad(path, maximumDimension, out var image))
                        library._textures[(materialIndex, semantic)] = image;
            if (scene.Environment.Path is not null &&
                TextureImage.TryLoad(scene.Environment.Path, maximumDimension, out var environment))
                library._environment = environment;
            if (useEnvironmentImportanceSampling)
                library._environmentImportance = EnvironmentImportanceSampler.TryLoad(scene.Environment.Path);
            return library;
        }

        public bool Has(int material, TextureSemantic semantic) => _textures.ContainsKey((material, semantic));
        public float EnvironmentMaximumLod => _environment?.MaximumLod ?? 0F;
        public float SampleScalar(int material, TextureSemantic semantic, Vector2 uv) => Sample(material, semantic, uv, Vector4.One, false).X;
        public Vector4 Sample(int material, TextureSemantic semantic, Vector2 uv, Vector4 fallback, bool srgb)
        {
            if (!_textures.TryGetValue((material, semantic), out var image)) return fallback;
            var sample = image.Sample(uv.X, uv.Y);
            if (!srgb) return sample;
            return new Vector4(SrgbToLinear(sample.X), SrgbToLinear(sample.Y), SrgbToLinear(sample.Z), sample.W);
        }
        public Vector3? SampleEnvironment(Vector3 direction, float rotation, float lod = 0F)
        {
            if (_environment is null) return null;
            direction = EnvironmentDirectionTransform.WorldToMap(direction, rotation);
            var u = MathF.Atan2(direction.Z, direction.X) / (2F * Pi) + 0.5F;
            var v = 0.5F - MathF.Asin(Math.Clamp(direction.Y, -1F, 1F)) / Pi;
            var sample = _environment.Sample(u, v, lod);
            return _environment.IsLinear
                ? new Vector3(sample.X, sample.Y, sample.Z)
                : new Vector3(SrgbToLinear(sample.X), SrgbToLinear(sample.Y), SrgbToLinear(sample.Z));
        }
        public bool TrySampleEnvironmentImportance(float selector, float jitterX, float jitterY, float rotation,
            out EnvironmentSample sample)
        {
            if (_environmentImportance is null) { sample = default; return false; }
            sample = _environmentImportance.Sample(selector, jitterX, jitterY, rotation);
            return true;
        }
        public float EnvironmentPdf(Vector3 direction, float rotation) =>
            _environmentImportance?.Pdf(direction, rotation) ?? 0F;
        public void Dispose() { foreach (var texture in _textures.Values) texture.Dispose(); _environment?.Dispose(); }
        private static float SrgbToLinear(float value) => value <= 0.04045F ? value / 12.92F : MathF.Pow((value + 0.055F) / 1.055F, 2.4F);
    }

    private sealed class TextureImage : IDisposable
    {
        private readonly int _width;
        private readonly int _height;
        private readonly byte[]? _pixels;
        private readonly float[]? _linearPixels;
        private readonly List<(int Width, int Height, float[] Pixels)>? _linearMipmaps;

        private TextureImage(int width, int height, byte[] pixels)
        {
            _width = width;
            _height = height;
            _pixels = pixels;
        }

        private TextureImage(HdrImage image)
        {
            _width = image.Width;
            _height = image.Height;
            _linearPixels = image.Pixels;
            _linearMipmaps = BuildLinearMipmaps(image.Width, image.Height, image.Pixels);
        }

        public bool IsLinear => _linearPixels is not null;
        public float MaximumLod => (_linearMipmaps?.Count ?? 1) - 1;

        public static bool TryLoad(string path, int maximumDimension, out TextureImage image)
        {
            try
            {
                if (string.Equals(Path.GetExtension(path), ".hdr", StringComparison.OrdinalIgnoreCase))
                {
                    image = new TextureImage(RadianceHdrLoader.Load(path));
                    return true;
                }
                using var source = Image.FromFile(path);
                var scale = Math.Min(1F, maximumDimension / (float)Math.Max(source.Width, source.Height));
                var width = Math.Max(1, (int)MathF.Round(source.Width * scale));
                var height = Math.Max(1, (int)MathF.Round(source.Height * scale));
                using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(bitmap)) graphics.DrawImage(source, 0, 0, width, height);
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    var pixels = new byte[checked(bitmap.Width * bitmap.Height * 4)];
                    for (var y = 0; y < bitmap.Height; y++)
                        System.Runtime.InteropServices.Marshal.Copy(data.Scan0 + y * data.Stride, pixels, y * bitmap.Width * 4, bitmap.Width * 4);
                    image = new TextureImage(bitmap.Width, bitmap.Height, pixels);
                    return true;
                }
                finally { bitmap.UnlockBits(data); }
            }
            catch { image = null!; return false; }
        }
        public Vector4 Sample(float u, float v, float lod = 0F)
        {
            u -= MathF.Floor(u); v -= MathF.Floor(v);
            var level = _linearMipmaps is null ? 0 : Math.Clamp((int)MathF.Round(lod), 0, _linearMipmaps.Count - 1);
            var width = level == 0 ? _width : _linearMipmaps![level].Width;
            var height = level == 0 ? _height : _linearMipmaps![level].Height;
            var fx = u * (width - 1);
            var fy = (1F - v) * (height - 1);
            var x0 = Math.Clamp((int)MathF.Floor(fx), 0, width - 1);
            var y0 = Math.Clamp((int)MathF.Floor(fy), 0, height - 1);
            var x1 = Math.Min(x0 + 1, width - 1);
            var y1 = Math.Min(y0 + 1, height - 1);
            var tx = fx - x0;
            var ty = fy - y0;
            var top = Vector4.Lerp(ReadPixel(x0, y0, level), ReadPixel(x1, y0, level), tx);
            var bottom = Vector4.Lerp(ReadPixel(x0, y1, level), ReadPixel(x1, y1, level), tx);
            return Vector4.Lerp(top, bottom, ty);
        }

        private Vector4 ReadPixel(int x, int y, int level)
        {
            if (_linearPixels is not null)
            {
                var mip = _linearMipmaps![level];
                var offset = (y * mip.Width + x) * 3;
                return new Vector4(mip.Pixels[offset], mip.Pixels[offset + 1], mip.Pixels[offset + 2], 1F);
            }
            var byteOffset = (y * _width + x) * 4;
            return new Vector4(_pixels![byteOffset + 2], _pixels[byteOffset + 1], _pixels[byteOffset], _pixels[byteOffset + 3]) / 255F;
        }

        private static List<(int Width, int Height, float[] Pixels)> BuildLinearMipmaps(
            int width, int height, float[] source)
        {
            var levels = new List<(int Width, int Height, float[] Pixels)> { (width, height, source) };
            while (width > 1 || height > 1)
            {
                var nextWidth = Math.Max(1, width / 2);
                var nextHeight = Math.Max(1, height / 2);
                var previous = levels[^1].Pixels;
                var next = new float[nextWidth * nextHeight * 3];
                for (var y = 0; y < nextHeight; y++)
                for (var x = 0; x < nextWidth; x++)
                for (var channel = 0; channel < 3; channel++)
                {
                    var sum = 0F;
                    var count = 0;
                    for (var oy = 0; oy < 2; oy++)
                    for (var ox = 0; ox < 2; ox++)
                    {
                        var px = Math.Min(x * 2 + ox, width - 1);
                        var py = Math.Min(y * 2 + oy, height - 1);
                        sum += previous[(py * width + px) * 3 + channel];
                        count++;
                    }
                    next[(y * nextWidth + x) * 3 + channel] = sum / count;
                }
                width = nextWidth;
                height = nextHeight;
                levels.Add((width, height, next));
            }
            return levels;
        }
        public void Dispose() { }
    }
}
