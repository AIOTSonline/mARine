using UnityEngine;

namespace CreateEnv
{
    // The single bridge between the friendly layer (SimpleEnvironmentSettings) and
    // the technical layer (EnvironmentProfile). The editor UI edits profile.simple
    // and calls Apply(); everything downstream — EnvironmentBounds.Clamp,
    // EnvironmentRepository, EnvironmentLoader — is unchanged and still enforces
    // every cap and cross-field invariant on the derived technical values.
    public static class SimpleEnvironmentMapper
    {
        public static void Apply(EnvironmentProfile p)
        {
            if (p == null || p.simple == null) return;
            var s = p.simple;
            s.Clamp();

            ApplySeafloor(p, s);
            ApplyHabitat(p, s);
            ApplyWater(p, s);
            ApplyExploration(p, s);
            ApplySurfaces(p, s);

            EnvironmentBounds.Clamp(p);
        }

        // ── 1. Seafloor ──────────────────────────────────────────────────────
        // Each profile is a hand-tuned recipe over the terrain-shape and biome-style
        // fields; waterLevel comes with it so invariant I-3 (peaks stay submerged)
        // holds for every recipe. Complexity layers fractal detail on top.
        static void ApplySeafloor(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            switch (s.seafloorProfile)
            {
                case 0: // Flat Plain — wide, gentle dunes
                    p.styleIndex = 0;
                    p.noiseScale = 90f; p.meshHeightMultiplier = 6f; p.waterLevel = 8f;
                    break;
                case 2: // Rocky Reef — tighter, bumpier dunes
                    p.styleIndex = 0;
                    p.noiseScale = 30f; p.meshHeightMultiplier = 24f; p.waterLevel = 8f;
                    break;
                case 3: // Coral Plateau — terraced mesas, gentle ridges
                    p.styleIndex = 1;
                    p.noiseScale = 60f; p.meshHeightMultiplier = 28f; p.waterLevel = 10f;
                    p.warpStrength = 5f;   p.warpScale = 2.5f;
                    p.ridgeWeight  = 0.35f; p.ridgeSharpness = 2f;
                    p.terraceSteps = 7;    p.terraceSharpness = 5f; p.terraceStrength = 0.85f;
                    break;
                case 4: // Underwater Canyon — deep ridged walls (mirrors the Canyon built-in)
                    p.styleIndex = 1;
                    p.noiseScale = 60f; p.meshHeightMultiplier = 40f; p.waterLevel = 14f;
                    p.warpStrength = 6f;   p.warpScale = 2.5f;
                    p.ridgeWeight  = 0.7f; p.ridgeSharpness = 2.6f;
                    p.terraceSteps = 4;    p.terraceSharpness = 4f; p.terraceStrength = 0.5f;
                    break;
                default: // 1: Rolling Hills — the classic dunes baseline
                    p.styleIndex = 0;
                    p.noiseScale = 55f; p.meshHeightMultiplier = 16f; p.waterLevel = 8f;
                    break;
            }

            float c = s.terrainComplexity;
            p.octaves     = 3 + Mathf.RoundToInt(c * 4f);        // 3..7
            p.persistance = Mathf.Lerp(0.40f, 0.62f, c);
            p.lacunarity  = Mathf.Lerp(1.8f, 2.4f, c);
        }

        // ── 2. Habitat ───────────────────────────────────────────────────────
        static void ApplyHabitat(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            // Pack indices follow the LifePackLibrary convention the built-ins use:
            // 1 = sandy/sample, 2 = kelp forest, 3 = coral (see BuiltInProfiles).
            switch (s.marineHabitat)
            {
                case 1:  p.lifePackIndex = 2; break; // Kelp Forest
                case 2:  p.lifePackIndex = 1; break; // Sandy Bottom
                default: p.lifePackIndex = 3; break; // Coral Reef
            }

            // Per-species multipliers are a technical detail the simple layer never
            // sets; empty means "1x for every species in the pack" to the loader.
            p.speciesDensity = System.Array.Empty<float>();

            float d = s.habitatDensity;
            p.lifeDensity      = Mathf.Lerp(0.3f, 1.8f, d);
            p.maxPropsPerChunk = Mathf.RoundToInt(Mathf.Lerp(80f, 300f, d));

            // How thickly rock is overgrown tracks the same density the user set for
            // life, and the palette follows the habitat: reef rock is coralline
            // pink/orange, kelp-forest rock is olive/rust, sand-bottom rock stays bare.
            p.encrustAmount = Mathf.Lerp(0.12f, 0.62f, d);
            p.encrustScale  = 1.6f;

            switch (s.marineHabitat)
            {
                case 1: // Kelp Forest
                    p.encrustColorA = new Color(0.36f, 0.42f, 0.22f, 1f);
                    p.encrustColorB = new Color(0.62f, 0.44f, 0.20f, 1f);
                    break;
                case 2: // Sandy Bottom
                    p.encrustAmount *= 0.35f;
                    p.encrustColorA = new Color(0.55f, 0.52f, 0.42f, 1f);
                    p.encrustColorB = new Color(0.68f, 0.62f, 0.48f, 1f);
                    break;
                default: // Coral Reef
                    p.encrustColorA = new Color(0.62f, 0.28f, 0.42f, 1f);
                    p.encrustColorB = new Color(0.82f, 0.48f, 0.26f, 1f);
                    break;
            }
        }

        // ── 3. Water ─────────────────────────────────────────────────────────
        static void ApplyWater(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            // Clarity is how thick the water reads: fog density plus how much light
            // survives. (Visibility, under Exploration, is how FAR you can see.)
            float clarity = s.waterClarity;
            p.fogDensity       = Mathf.Lerp(0.11f, 0.03f, clarity);
            p.sunGlowIntensity = Mathf.Lerp(0.7f, 1.3f, clarity);
            p.ambientIntensity = Mathf.Lerp(0.8f, 1.2f, clarity);

            // Murky water absorbs the channels it is dark in harder, and carries more
            // suspended particulate. Both fall out of the clarity slider the user
            // already sets — no new knob, and every existing palette gets them.
            p.absorbTint  = Mathf.Lerp(2.6f, 1.3f, clarity);
            p.snowCount   = Mathf.RoundToInt(Mathf.Lerp(900f, 400f, clarity));
            p.snowOpacity = Mathf.Lerp(0.58f, 0.34f, clarity);
            p.snowDrift   = 0.05f;
            p.snowSink    = 0.03f;

            p.surgeAmplitude = 0.05f;
            p.surgeSpeed     = 0.55f;
            p.surgeDirX      = 1f;
            p.surgeDirZ      = 0.35f;

            switch (s.waterColour)
            {
                case 1: // Coastal Green (kelp-forest palette)
                    SetColours(p, new Color(0.03f, 0.40f, 0.42f), new Color(0.40f, 0.80f, 0.65f),
                                  new Color(0.01f, 0.22f, 0.20f), new Color(0.70f, 0.95f, 0.80f));
                    break;
                case 2: // Deep Ocean Blue
                    SetColours(p, new Color(0.01f, 0.25f, 0.45f), new Color(0.20f, 0.55f, 0.80f),
                                  new Color(0.002f, 0.10f, 0.22f), new Color(0.60f, 0.80f, 0.95f));
                    break;
                case 3: // Algae Bloom
                    SetColours(p, new Color(0.10f, 0.42f, 0.25f), new Color(0.45f, 0.75f, 0.40f),
                                  new Color(0.03f, 0.18f, 0.10f), new Color(0.80f, 0.90f, 0.60f));
                    break;
                default: // 0: Tropical Blue (the shipped default palette)
                    SetColours(p, new Color(0.015f, 0.46f, 0.595f), new Color(0.33f, 0.78f, 0.85f),
                                  new Color(0.004f, 0.19f, 0.30f), new Color(0.75f, 0.95f, 0.90f));
                    break;
            }
        }

        static void SetColours(EnvironmentProfile p, Color water, Color glow, Color deep, Color sun)
        {
            water.a = glow.a = deep.a = sun.a = 1f;
            p.waterColor = water; p.surfaceGlowColor = glow;
            p.deepColor = deep;   p.sunGlowColor = sun;
        }

        // ── 5. Surface styles ────────────────────────────────────────────────
        // A straight pass-through, unlike every other mapping here. These two are
        // genuinely independent aesthetic choices: they drive no other system, so
        // there is nothing to correlate them with. Both are already validated
        // indices (SimpleEnvironmentSettings.Clamp), and index 0 leaves the
        // scene's own materials untouched.
        static void ApplySurfaces(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            p.terrainTextureStyle = s.seafloorSurface;
            p.waterStyle          = s.waterMovement;
        }

        // ── 4. Exploration ───────────────────────────────────────────────────
        static void ApplyExploration(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            p.viewDistanceIndex = s.explorationArea; // Small/Medium/Large = Near/Medium/Far

            // How far you can see before the fog closes in. EnvironmentBounds.Clamp
            // still trims these against the streaming reach (invariants I-1/I-2).
            p.fadeStart = Mathf.Lerp(8f, 26f, s.visibility);
            p.fadeEnd   = Mathf.Lerp(14f, 44f, s.visibility);
        }
    }
}
