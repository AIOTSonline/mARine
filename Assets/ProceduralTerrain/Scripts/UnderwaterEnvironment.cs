using UnityEngine;
using UnityEngine.Rendering;

// Drives fog, ambient, camera and the underwater shader globals from one palette.
// Keep fadeEnd below the terrain view distance (last LOD threshold * 0.25).
[ExecuteAlways]
public class UnderwaterEnvironment : MonoBehaviour
{
    [Header("Water Palette (horizon = fog colour; all fades stay seamless)")]
    public Color waterColor = new Color(0.015f, 0.46f, 0.595f, 1f);
    [Tooltip("Bright glow when looking up toward the surface.")]
    public Color surfaceGlowColor = new Color(0.33f, 0.78f, 0.85f, 1f);
    [Tooltip("Deep navy when looking down past the sand.")]
    public Color deepColor = new Color(0.004f, 0.19f, 0.30f, 1f);
    [Tooltip("In-scattered glow around the sun (skybox, fog and god rays).")]
    public Color sunGlowColor = new Color(0.75f, 0.95f, 0.90f, 1f);
    [Range(0f, 2f)] public float sunGlowIntensity = 1f;

    [Header("Backdrop")]
    [Tooltip("Material using Custom/UnderwaterSkybox. If set, the camera clears with the gradient skybox instead of a flat colour.")]
    public Material skyboxMaterial;

    [Header("Visibility (world metres)")]
    [Tooltip("Exp2 fog density. 0.055 keeps the foreground crisp; the hard fade still guarantees 100% by fadeEnd.")]
    public float fogDensity = 0.055f;
    [Tooltip("Distance where the hard fade to water colour begins.")]
    public float fadeStart = 14f;
    [Tooltip("Distance where everything is 100% water colour. Keep below the terrain view distance!")]
    public float fadeEnd = 22f;
    [Tooltip("Camera far plane = fadeEnd + this margin.")]
    public float cameraFarMargin = 3f;

    [Header("Scene References")]
    public Camera targetCamera;
    [Tooltip("World height of the water surface (match the WaterSurface object).")]
    public float waterLevel = 8f;

    [Header("Ambient Light")]
    [Range(0f, 2f)] public float ambientIntensity = 1f;

    [Header("Medium (per-channel absorption)")]
    [Tooltip("How much harder the channels the water is dark in are absorbed. 0 reproduces " +
             "the original single-channel fog exactly; ~2 gives red dying about 3x faster " +
             "than blue, which is what makes distance read as water rather than haze.")]
    [Range(0f, 4f)] public float absorbTint = 2f;

    [Header("Surge (shared swell for soft life)")]
    [Range(0f, 1f)] public float surgeAmplitude = 0.05f;
    public float surgeSpeed = 0.55f;
    public Vector2 surgeDirection = new Vector2(1f, 0.35f);

    [Header("Encrusting Life (rock surfaces)")]
    [Range(0f, 1f)] public float encrustAmount = 0.35f;
    public float encrustScale = 1.6f;
    public Color encrustColorA = new Color(0.62f, 0.28f, 0.42f, 1f);
    public Color encrustColorB = new Color(0.78f, 0.46f, 0.26f, 1f);

    static readonly int FogColorId     = Shader.PropertyToID("_UnderwaterFogColor");
    static readonly int SurfaceColorId = Shader.PropertyToID("_UnderwaterColorSurface");
    static readonly int DeepColorId    = Shader.PropertyToID("_UnderwaterColorDeep");
    static readonly int SunGlowId      = Shader.PropertyToID("_UnderwaterSunGlow");
    static readonly int FogDensityId   = Shader.PropertyToID("_UnderwaterFogDensity");
    static readonly int FadeStartId    = Shader.PropertyToID("_UnderwaterFadeStart");
    static readonly int FadeEndId      = Shader.PropertyToID("_UnderwaterFadeEnd");
    static readonly int WaterLevelId   = Shader.PropertyToID("_UnderwaterLevel");
    static readonly int AbsorbTintId   = Shader.PropertyToID("_UnderwaterAbsorbTint");
    static readonly int SurgeAmpId     = Shader.PropertyToID("_UnderwaterSurgeAmp");
    static readonly int SurgeSpeedId   = Shader.PropertyToID("_UnderwaterSurgeSpeed");
    static readonly int SurgeDirId     = Shader.PropertyToID("_UnderwaterSurgeDir");
    static readonly int EncrustAId     = Shader.PropertyToID("_UnderwaterEncrustA");
    static readonly int EncrustBId     = Shader.PropertyToID("_UnderwaterEncrustB");
    static readonly int EncrustAmtId   = Shader.PropertyToID("_UnderwaterEncrustAmount");
    static readonly int EncrustScaleId = Shader.PropertyToID("_UnderwaterEncrustScale");

    void OnEnable()  => Apply();
    void OnValidate() => Apply();

    void Update() => Apply(); // keep live, and re-assert settings each frame

    void Apply()
    {
        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogColor   = waterColor;
        RenderSettings.fogDensity = fogDensity;

        // Ambient trilight from the palette.
        RenderSettings.ambientMode         = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor     = surfaceGlowColor * ambientIntensity;
        RenderSettings.ambientEquatorColor = waterColor * ambientIntensity;
        RenderSettings.ambientGroundColor  = deepColor * ambientIntensity;

        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        // AR fallback: drive the active camera when none is wired (runtime only, so we
        // don't hijack the Scene-view / editor camera while [ExecuteAlways] runs).
        if (targetCamera == null && Application.isPlaying)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            targetCamera.clearFlags = skyboxMaterial != null
                ? CameraClearFlags.Skybox
                : CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = waterColor;
            targetCamera.farClipPlane    = fadeEnd + cameraFarMargin;
        }

        // Shader globals skip Unity's auto sRGB→linear, so convert here.
        bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        Color fogCol     = linear ? waterColor.linear       : waterColor;
        Color surfaceCol = linear ? surfaceGlowColor.linear : surfaceGlowColor;
        Color deepCol    = linear ? deepColor.linear        : deepColor;
        Color glowCol    = (linear ? sunGlowColor.linear    : sunGlowColor) * sunGlowIntensity;

        Shader.SetGlobalColor(FogColorId,     fogCol);
        Shader.SetGlobalColor(SurfaceColorId, surfaceCol);
        Shader.SetGlobalColor(DeepColorId,    deepCol);
        Shader.SetGlobalColor(SunGlowId,      glowCol);
        Shader.SetGlobalFloat(FogDensityId,   fogDensity);
        Shader.SetGlobalFloat(FadeStartId,    fadeStart);
        Shader.SetGlobalFloat(FadeEndId,      fadeEnd);
        Shader.SetGlobalFloat(WaterLevelId,   waterLevel);

        Shader.SetGlobalFloat(AbsorbTintId, Mathf.Max(0f, absorbTint));

        Vector2 dir = surgeDirection.sqrMagnitude > 1e-5f
            ? surgeDirection.normalized
            : new Vector2(1f, 0f);
        Shader.SetGlobalFloat(SurgeAmpId,   Mathf.Max(0f, surgeAmplitude));
        Shader.SetGlobalFloat(SurgeSpeedId, surgeSpeed);
        Shader.SetGlobalVector(SurgeDirId,  new Vector4(dir.x, dir.y, 0f, 0f));

        Color crustA = linear ? encrustColorA.linear : encrustColorA;
        Color crustB = linear ? encrustColorB.linear : encrustColorB;
        Shader.SetGlobalColor(EncrustAId,     crustA);
        Shader.SetGlobalColor(EncrustBId,     crustB);
        Shader.SetGlobalFloat(EncrustAmtId,   Mathf.Clamp01(encrustAmount));
        Shader.SetGlobalFloat(EncrustScaleId, Mathf.Max(0.01f, encrustScale));
    }
}
