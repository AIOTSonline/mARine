using UnityEngine;

namespace CreateEnv
{
    // Lives on one GameObject in the Explore scene. On Start it takes the selected
    // profile, clamps it once more (defence in depth), and pushes every value into
    // the five terrain systems in the correct order — MapGenerator first, streaming
    // LAST — so chunks are never generated from stale/default parameters.
    //
    // This is the ONLY place configuration is applied, and it runs once. There is no
    // per-frame cost: after Start, the terrain behaves exactly like a hand-authored scene.
    public class EnvironmentLoader : MonoBehaviour
    {
        [Header("Optional explicit refs (auto-found if left empty)")]
        public MapGenerator          mapGenerator;
        public UnderwaterEnvironment underwater;
        public WaterSurface          water;
        public TerrainDetailScatter  scatter;
        public EndlessTerrain        endlessTerrain;
        public MarineSnow            snow;

        [Tooltip("Used only when Explore is entered directly (no StartScreen). " +
                 "0 = Sample, 1 = Canyon, 2 = Kelp.")]
        public int fallbackBuiltInIndex = 0;

        void Start()
        {
            var profile = ResolveProfile();
            EnvironmentBounds.Clamp(profile); // never trust the incoming object
            Apply(profile);
        }

        EnvironmentProfile ResolveProfile()
        {
            if (EnvironmentSession.Selected != null)
                return EnvironmentSession.Selected;

            // Entered Explore directly (e.g. pressing Play in the Editor): fall back
            // to a built-in so the scene still works for development.
            var builtins = BuiltInProfiles.All();
            int i = Mathf.Clamp(fallbackBuiltInIndex, 0, builtins.Length - 1);
            Debug.Log($"[EnvironmentLoader] No selected profile; using built-in '{builtins[i].displayName}'.");
            return builtins[i];
        }

        void Apply(EnvironmentProfile p)
        {
            AutoWire();

            // 1) Terrain shape — must be set before any chunk generates.
            if (mapGenerator != null) mapGenerator.ApplyProfile(p);
            else Debug.LogWarning("[EnvironmentLoader] No MapGenerator found — terrain won't match the profile.");

            // 2) Water & atmosphere (single waterLevel source drives both, invariant I-4).
            if (underwater != null)
            {
                underwater.waterColor       = p.waterColor;
                underwater.surfaceGlowColor = p.surfaceGlowColor;
                underwater.deepColor        = p.deepColor;
                underwater.sunGlowColor     = p.sunGlowColor;
                underwater.sunGlowIntensity = p.sunGlowIntensity;
                underwater.ambientIntensity = p.ambientIntensity;
                underwater.fogDensity       = p.fogDensity;
                underwater.fadeStart        = p.fadeStart;
                underwater.fadeEnd          = p.fadeEnd;
                underwater.cameraFarMargin  = p.cameraFarMargin;
                underwater.waterLevel       = p.waterLevel; // I-5 far plane derived inside Apply()

                underwater.absorbTint     = p.absorbTint;
                underwater.surgeAmplitude = p.surgeAmplitude;
                underwater.surgeSpeed     = p.surgeSpeed;
                underwater.surgeDirection = new Vector2(p.surgeDirX, p.surgeDirZ);
                underwater.encrustAmount  = p.encrustAmount;
                underwater.encrustScale   = p.encrustScale;
                underwater.encrustColorA  = p.encrustColorA;
                underwater.encrustColorB  = p.encrustColorB;
            }

            // 2b) Suspended particulate. The wrap box is tied to fadeEnd so snow never
            // pops in beyond the distance the fog has already gone opaque (invariant I-2).
            if (snow != null)
            {
                float snowFar = Mathf.Clamp(p.fadeEnd * 0.35f, 3f, 9f);
                snow.nearFade = 0.5f;
                snow.farFade  = snowFar;
                snow.ApplySettings(p.snowCount, snowFar * 1.7f,
                                   Color.Lerp(p.surfaceGlowColor, Color.white, 0.55f),
                                   p.snowOpacity, p.snowSizeMin, p.snowSizeMax,
                                   p.snowDrift, p.snowSink);
            }

            // 2c) Surface styles — must precede water.Rebuild() (which assigns the
            // water material) and EndlessTerrain.Initialize() (chunks read
            // MapGenerator.terrainMaterial as they are created).
            ApplySurfaceStyles(p);

            // 3) Water plane.
            if (water != null)
            {
                water.waterLevel = p.waterLevel;
                water.resolution = Mathf.Clamp(p.waterResolution, 8, 160);
                water.Rebuild();
            }

            // 4) Life / scatter (per-species density, capped by maxPropsPerChunk = I-6).
            if (scatter != null)
            {
                var lib  = LifePackLibrary.Load();
                var pack = lib != null ? lib.GetPack(p.lifePackIndex) : null;
                if (lib == null && p.lifePackIndex > 0)
                    Debug.LogWarning("[EnvironmentLoader] No LifePackLibrary in Resources — keeping the scene's existing scatter rules; only global density is applied.");

                if (lib != null)
                    scatter.ApplyLifePack(pack, p.lifeDensity, p.speciesDensity,
                                          p.maxPropsPerChunk, p.castShadows, p.waterTint, p.scatterSeed);
                else
                {
                    // Fallback: scale whatever rules the scene already has.
                    scatter.maxPropsPerChunk = p.maxPropsPerChunk;
                    scatter.castShadows      = p.castShadows;
                    scatter.waterTint        = p.waterTint;
                }
            }

            // 5) Streaming — LAST, after MapGenerator is configured.
            if (endlessTerrain != null)
            {
                endlessTerrain.autoStart    = false;
                endlessTerrain.detailLevels = EndlessTerrain.BuildViewDistance(p.viewDistanceIndex);
                endlessTerrain.Initialize();
            }
            else Debug.LogWarning("[EnvironmentLoader] No EndlessTerrain found — nothing will stream.");
        }

        // Runtime material clones created for the chosen surface styles. Kept so they
        // can be destroyed with this object — a Material created with `new` is not
        // collected automatically and would leak once per environment load.
        Material _styledTerrainMaterial;
        Material _styledWaterMaterial;

        // Applies the sea-floor texture style and the water style.
        //
        // Both are applied to CLONES. TerrainTextureStyles/WaterStyles write directly
        // into the material they are handed, and the materials here are shared project
        // assets: mutating them in the Editor persists the change into the asset file,
        // so every environment loaded afterwards would inherit the last one's look.
        //
        // Style index 0 is a strict no-op and is deliberately handled by skipping the
        // clone entirely rather than by cloning and applying nothing — an untouched
        // scene material is the one case that must be bit-for-bit unchanged, since
        // every profile saved before these fields existed deserializes to 0.
        void ApplySurfaceStyles(EnvironmentProfile p)
        {
            if (p.terrainTextureStyle > 0 && mapGenerator != null && mapGenerator.terrainMaterial != null)
            {
                _styledTerrainMaterial = new Material(mapGenerator.terrainMaterial)
                {
                    name = mapGenerator.terrainMaterial.name + " (styled)"
                };
                TerrainTextureStyles.Apply(_styledTerrainMaterial, p.terrainTextureStyle);
                // Caustic character is seeded from the environment's own seed and
                // palette, so two environments on the same sea floor still differ.
                TerrainTextureStyles.ApplyCaustics(_styledTerrainMaterial, p.terrainTextureStyle,
                                                   p.seed, p.surfaceGlowColor);
                mapGenerator.terrainMaterial = _styledTerrainMaterial;
            }

            if (p.waterStyle > 0 && water != null && water.waterMaterial != null)
            {
                _styledWaterMaterial = new Material(water.waterMaterial)
                {
                    name = water.waterMaterial.name + " (styled)"
                };
                WaterStyles.Apply(_styledWaterMaterial, p.waterStyle, p.waterColor, p.seed);
                water.waterMaterial = _styledWaterMaterial;
            }
        }

        void OnDestroy()
        {
            if (_styledTerrainMaterial != null) Destroy(_styledTerrainMaterial);
            if (_styledWaterMaterial != null)   Destroy(_styledWaterMaterial);
        }

        void AutoWire()
        {
            if (mapGenerator   == null) mapGenerator   = FindFirstObjectByType<MapGenerator>();
            if (underwater     == null) underwater     = FindFirstObjectByType<UnderwaterEnvironment>();
            if (water          == null) water          = FindFirstObjectByType<WaterSurface>();
            if (scatter        == null) scatter        = FindFirstObjectByType<TerrainDetailScatter>();
            if (endlessTerrain == null) endlessTerrain = FindFirstObjectByType<EndlessTerrain>();
            if (snow           == null) snow           = FindFirstObjectByType<MarineSnow>();
        }
    }
}
