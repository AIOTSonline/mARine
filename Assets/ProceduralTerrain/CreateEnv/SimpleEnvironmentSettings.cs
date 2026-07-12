using System;
using UnityEngine;

namespace CreateEnv
{
    // The user-facing half of an environment: a few friendly choices (dropdowns +
    // 0..1 sliders), no technical knobs. Only knobs that genuinely drive a system
    // in the technical profile exist here — nothing decorative. The editor UI edits
    // this; SimpleEnvironmentMapper translates it into the full technical
    // EnvironmentProfile. Stored inside the profile's .json so editing an
    // environment re-opens with the same choices.
    [Serializable]
    public class SimpleEnvironmentSettings
    {
        // Dropdown option labels. The stored value is the index into these arrays.
        public static readonly string[] SeafloorProfiles =
            { "Flat Plain", "Rolling Hills", "Rocky Reef", "Coral Plateau", "Underwater Canyon" };
        public static readonly string[] MarineHabitats =
            { "Coral Reef", "Kelp Forest", "Sandy Bottom" };
        public static readonly string[] WaterColours =
            { "Tropical Blue", "Coastal Green", "Deep Ocean Blue", "Algae Bloom" };
        public static readonly string[] ExplorationAreas =
            { "Small", "Medium", "Large" };

        // ── 1. Seafloor (terrain shape + biome style) ────────────────────────
        public int   seafloorProfile = 1;      // Rolling Hills
        public float terrainComplexity = 0.5f; // Simple <-> Complex

        // ── 2. Habitat (life pack + scatter density) ─────────────────────────
        public int   marineHabitat = 0;        // Coral Reef
        public float habitatDensity = 0.5f;    // Sparse <-> Dense

        // ── 3. Water (fog thickness/light + colour palette) ──────────────────
        public float waterClarity = 0.6f;      // Murky <-> Crystal Clear
        public int   waterColour = 0;          // Tropical Blue

        // ── 4. Exploration (streaming reach + fog fade distance) ─────────────
        public int   explorationArea = 1;      // Medium
        public float visibility = 0.6f;        // Low <-> High

        public void Clamp()
        {
            seafloorProfile = Mathf.Clamp(seafloorProfile, 0, SeafloorProfiles.Length - 1);
            marineHabitat   = Mathf.Clamp(marineHabitat, 0, MarineHabitats.Length - 1);
            waterColour     = Mathf.Clamp(waterColour, 0, WaterColours.Length - 1);
            explorationArea = Mathf.Clamp(explorationArea, 0, ExplorationAreas.Length - 1);

            terrainComplexity = Mathf.Clamp01(terrainComplexity);
            habitatDensity    = Mathf.Clamp01(habitatDensity);
            waterClarity      = Mathf.Clamp01(waterClarity);
            visibility        = Mathf.Clamp01(visibility);
        }

        public SimpleEnvironmentSettings Clone() => (SimpleEnvironmentSettings)MemberwiseClone();
    }
}
