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
            }

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

        void AutoWire()
        {
            if (mapGenerator   == null) mapGenerator   = FindFirstObjectByType<MapGenerator>();
            if (underwater     == null) underwater     = FindFirstObjectByType<UnderwaterEnvironment>();
            if (water          == null) water          = FindFirstObjectByType<WaterSurface>();
            if (scatter        == null) scatter        = FindFirstObjectByType<TerrainDetailScatter>();
            if (endlessTerrain == null) endlessTerrain = FindFirstObjectByType<EndlessTerrain>();
        }
    }
}
