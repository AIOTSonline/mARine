using UnityEngine;

namespace CreateEnv
{
    // The three shipped biomes, defined as data (not scenes). These appear as the
    // first tiles on the start screen. Values mirror the original SampleScene /
    // CoralCanyons / KelpForest scenes; tweak here to re-tune a built-in.
    public static class BuiltInProfiles
    {
        public static EnvironmentProfile[] All()
        {
            return new[] { Sample(), Canyon(), Kelp() };
        }

        static EnvironmentProfile Sample()
        {
            var p = new EnvironmentProfile
            {
                id = "builtin-sample", displayName = "Sample", isBuiltIn = true,
                styleIndex = 0,
                noiseScale = 45f, octaves = 5, persistance = 0.5f, lacunarity = 2f,
                meshHeightMultiplier = 18f, seed = 0,
                waterLevel = 8f, fogDensity = 0.055f, fadeStart = 14f, fadeEnd = 22f,
                lifePackIndex = 1, lifeDensity = 1f, viewDistanceIndex = 1,
            };
            EnvironmentBounds.Clamp(p);
            return p;
        }

        static EnvironmentProfile Canyon()
        {
            var p = new EnvironmentProfile
            {
                id = "builtin-canyon", displayName = "Canyon", isBuiltIn = true,
                styleIndex = 1,
                noiseScale = 60f, octaves = 6, persistance = 0.5f, lacunarity = 2.1f,
                meshHeightMultiplier = 40f, seed = 7,
                warpStrength = 6f, warpScale = 2.5f, ridgeWeight = 0.6f, ridgeSharpness = 2.4f,
                terraceSteps = 5, terraceSharpness = 4f, terraceStrength = 0.65f,
                waterLevel = 14f, fogDensity = 0.05f, fadeStart = 18f, fadeEnd = 30f,
                lifePackIndex = 3, lifeDensity = 0.8f, viewDistanceIndex = 2,
                waterColor = new Color(0.02f, 0.35f, 0.5f, 1f),
            };
            EnvironmentBounds.Clamp(p);
            return p;
        }

        static EnvironmentProfile Kelp()
        {
            var p = new EnvironmentProfile
            {
                id = "builtin-kelp", displayName = "Kelp Forest", isBuiltIn = true,
                styleIndex = 0,
                noiseScale = 50f, octaves = 5, persistance = 0.48f, lacunarity = 2f,
                meshHeightMultiplier = 16f, seed = 3,
                waterLevel = 10f, fogDensity = 0.06f, fadeStart = 12f, fadeEnd = 22f,
                lifePackIndex = 2, lifeDensity = 1.3f, viewDistanceIndex = 1,
                waterColor = new Color(0.03f, 0.4f, 0.42f, 1f),
                surfaceGlowColor = new Color(0.4f, 0.8f, 0.65f, 1f),
            };
            EnvironmentBounds.Clamp(p);
            return p;
        }
    }
}
