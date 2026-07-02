# Ocean Biomes

The project now ships three distinct ocean environments instead of one. Each biome is a
self-contained environment prefab (drop one into a scene near the AR anchor; nothing else to wire) — the
streaming, LOD and scatter systems underneath are shared.

| Biome | Prefab (Assets/ProceduralTerrain/Prefabs) | Mood |
|---|---|---|
| Coral Shallows | `ProceduralTerrainEnvironment.prefab` | The original bright turquoise reef dunes |
| Kelp Forest | `KelpForestEnvironment.prefab` | Emerald water, sun shafts, tall swaying kelp groves, warm stone |
| Coral Canyons | `CoralCanyonsEnvironment.prefab` | Bright turquoise reef canyons: terraced cliff walls, swim-through arches, grotto caves, coral mounds, purple anemone patches, pink sea fans |

## How the cliff / cave / overhang feel works

The terrain itself is still a 2D heightmap (cheap, streamable, threadable), so it can
never fold over itself. Two tricks layer on top of it:

1. **Terrain styling** (`TerrainShaper.cs`). The `Canyons` style runs domain-warped
   ridged fBm through a terracing function, producing flat mesa tops with near-vertical
   cliff bands between them. Combined with a tall height multiplier, walls read as
   canyon cliffs even though every column is still single-valued. `Classic` style is
   bit-identical to the original noise, so old scenes are unaffected.

2. **Procedural 3D formations** (`ProceduralMeshLibrary.cs` + `ProceduralFeatureScatter.cs`).
   True overhangs come from separate meshes seated on the heightfield:
   - **Arch** — a noise-deformed half-torus you can swim through
   - **Overhang** — mushroom/table rock with a cantilevered cap (shadowed underside baked in)
   - **Grotto** — a hollow double-shelled dome with a mouth opening; the interior bakes
     near-black vertex AO, so it reads as a cave without any lights
   - **Boulder / Spire** — fill shapes for fields and skylines
   - **KelpPlant / GlowAnemone / SeaFan** — flora, softly glowing anemone patches, and
     branching gorgonian fans

   All meshes are generated once at startup from a seed (a handful of variants per rule)
   and shared by every instance, flat-shaded, textureless, vertex-coloured. Scattering is
   deterministic per chunk, so formations rebuild identically when you swim back.

## Rendering

Three new hand-written URP shaders match the existing underwater fog/backdrop system
(driven by `UnderwaterEnvironment.cs` shader globals), so props melt into the same water
as the sand and skybox:

- `Custom/UnderwaterRock` — colour ramp + strata bands + baked vertex AO + caustics on
  up-facing surfaces + rim light. No textures.
- `Custom/UnderwaterKelp` — vertex-shader current sway (roots stay planted), root→tip
  gradient, sun-through-blade translucency, two-sided.
- `Custom/UnderwaterGlow` — pulsing emissive mask in vertex alpha, de-phased per instance,
  partially punches through fog so distant glows read in the dark.

Everything is tuned for mobile: no shadows on props, no textures, small shared meshes,
SRP-batcher-compatible shaders, and the same chunk spawn/despawn hysteresis as the
prefab scatter.

## Making another ocean

1. Duplicate one of the biome scenes (or environment prefabs in the Marine app).
2. Retint the palette on `UnderwaterEnvironment` (fog = horizon; keep `fadeEnd` below the
   terrain view distance).
3. Pick a terrain style on `MapGenerator` → Terrain Style (`Classic` dunes or `Canyons`).
4. Swap materials (sand/water tints) and edit the `ProceduralFeatureScatter` rules —
   densities, sizes, masks — plus the prefab rules on `TerrainDetailScatter`.
