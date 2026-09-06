using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatic cinematic pass for the Gate Zero hub.
/// It runs only in GZ_Hub_01 and deliberately avoids changing source Synty assets.
/// </summary>
public static class GateZeroCinematicBootstrap
{
    private const string TargetScene = "GZ_Hub_01";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyOnStartup()
    {
        Apply(SceneManager.GetActiveScene());
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply(scene);
    }

    private static void Apply(Scene scene)
    {
        if (!scene.IsValid() || scene.name != TargetScene)
            return;

        ConfigureCamera();
        ConfigureAtmosphere();
        ConfigurePostProcessing();
        ConfigureRain();
        ConfigureWetSurfaces();
        EnsureReflectionProbe();
        ConfigureQuality();
    }

    private static void ConfigureCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.allowHDR = true;

        if (cam.TryGetComponent(out UniversalAdditionalCameraData cameraData))
        {
            cameraData.renderPostProcessing = true;
            cameraData.stopNaN = true;
            cameraData.dithering = true;
        }
    }

    private static void ConfigureAtmosphere()
    {
        // Dark blue atmospheric perspective: enough to soften the skyline without hiding gameplay.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.025f, 0.045f, 0.080f, 1f);
        RenderSettings.fogDensity = 0.0065f;

        // Keep the environment dark enough that neon and wet reflections become the light sources.
        RenderSettings.ambientIntensity = Mathf.Min(RenderSettings.ambientIntensity, 0.62f);
        RenderSettings.reflectionIntensity = Mathf.Max(RenderSettings.reflectionIntensity, 1.05f);
    }

    private static void ConfigurePostProcessing()
    {
        Volume target = null;
        Volume[] volumes = Object.FindObjectsOfType<Volume>(true);

        foreach (Volume volume in volumes)
        {
            if (volume == null || volume.profile == null)
                continue;

            if (target == null)
                target = volume;

            if (volume.isGlobal)
            {
                target = volume;
                break;
            }
        }

        if (target == null)
        {
            GameObject go = new GameObject("GZ_Cinematic_GlobalVolume");
            target = go.AddComponent<Volume>();
            target.isGlobal = true;
            target.priority = 100f;
            target.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        VolumeProfile profile = target.profile;

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.90f);
        bloom.intensity.Override(1.15f);
        bloom.scatter.Override(0.68f);

        Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        color.postExposure.Override(-0.22f);
        color.contrast.Override(14f);
        color.saturation.Override(-6f);
        color.colorFilter.Override(new Color(0.93f, 0.97f, 1.00f, 1f));

        WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(profile);
        whiteBalance.active = true;
        whiteBalance.temperature.Override(-7f);
        whiteBalance.tint.Override(2f);

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.active = true;
        vignette.intensity.Override(0.14f);
        vignette.smoothness.Override(0.42f);

        FilmGrain grain = GetOrAdd<FilmGrain>(profile);
        grain.active = true;
        grain.intensity.Override(0.08f);
        grain.response.Override(0.82f);

        ChromaticAberration chromatic = GetOrAdd<ChromaticAberration>(profile);
        chromatic.active = true;
        chromatic.intensity.Override(0.015f);
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component))
            return component;

        return profile.Add<T>(true);
    }

    private static void ConfigureRain()
    {
        ParticleSystem[] systems = Object.FindObjectsOfType<ParticleSystem>(true);

        foreach (ParticleSystem ps in systems)
        {
            if (ps == null)
                continue;

            string lower = ps.name.ToLowerInvariant();

            if (lower.Contains("rainsplash") || lower.Contains("rain_splash") || lower.Contains("splash"))
                ConfigureSplash(ps);
            else if (lower.Contains("rain"))
                ConfigureRainEmitter(ps);
        }
    }

    private static void ConfigureRainEmitter(ParticleSystem ps)
    {
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.simulationSpeed = 1f;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.86f, 0.93f, 1f, 0.62f));

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(1.35f, 2.05f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.18f, 0.50f);

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.1f;
            renderer.velocityScale = 0.12f;
            renderer.cameraVelocityScale = 0f;
        }
    }

    private static void ConfigureSplash(ParticleSystem ps)
    {
        var main = ps.main;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.simulationSpeed = 1f;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
        main.gravityModifier = new ParticleSystem.MinMaxCurve(0.40f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.90f, 0.96f, 1f, 0.68f));

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = false;

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }

    private static void ConfigureWetSurfaces()
    {
        Renderer[] renderers = Object.FindObjectsOfType<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            bool objectLooksWet = renderer.name.ToLowerInvariant().Contains("wetroad") ||
                                  renderer.name.ToLowerInvariant().Contains("wet_road");

            Material[] materials = renderer.materials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                    continue;

                string materialName = material.name.ToLowerInvariant();
                bool materialLooksWet = materialName.Contains("wetroad") ||
                                        materialName.Contains("wet_road") ||
                                        materialName.Contains("wetoverlay");

                if (!objectLooksWet && !materialLooksWet)
                    continue;

                if (material.HasProperty("_Smoothness"))
                    material.SetFloat("_Smoothness", 0.93f);

                if (material.HasProperty("_Metallic"))
                    material.SetFloat("_Metallic", 0.02f);

                if (material.HasProperty("_BaseColor"))
                {
                    Color c = material.GetColor("_BaseColor");
                    c.r *= 0.72f;
                    c.g *= 0.76f;
                    c.b *= 0.82f;
                    c.a = Mathf.Min(c.a, 0.20f);
                    material.SetColor("_BaseColor", c);
                }

                changed = true;
            }

            if (changed)
                renderer.materials = materials;
        }
    }

    private static void EnsureReflectionProbe()
    {
        ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>(true);
        if (probes.Length > 0)
            return;

        GameObject player = GameObject.Find("PlayerArmature");
        Vector3 position = player != null ? player.transform.position : Vector3.zero;

        GameObject go = new GameObject("GZ_ReflectionProbe_Main");
        go.transform.position = position + new Vector3(0f, 3.5f, 0f);

        ReflectionProbe probe = go.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.resolution = 128;
        probe.intensity = 1.10f;
        probe.boxProjection = true;
        probe.size = new Vector3(32f, 12f, 44f);
        probe.nearClipPlane = 0.3f;
        probe.farClipPlane = 70f;
    }

    private static void ConfigureQuality()
    {
        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 70f);
        QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 1.5f);
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
    }
}
