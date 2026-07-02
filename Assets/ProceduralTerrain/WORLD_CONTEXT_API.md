# World API

A tiny, **read-only** helper so the AI/assistant can ask *"what's around the player right now?"*.

- **One static class:** `World` — no namespace, no setup, no component to add.
- **One file:** `Assets/Scripts/WorldContext/World.cs`
- Call it on demand (when the user asks the assistant), not every frame. It never changes the world.

It reads what's already in the scene — `Camera.main`, the seabed (a downward ray),
`UnderwaterEnvironment` / `WaterSurface`, and objects tagged `Actor` (creatures) / `Obstacle`
(props). If something isn't there, that value falls back to a sensible estimate.

---

## Use it

```csharp
World.GetCreatures(10);   // List<CreatureInfo> within 10 m, nearest first
World.GetEnvironment();   // depth, biome, temperature, visibility, light...
World.GetPlayer();        // position, depth, heading
World.Describe();         // one short sentence for the LLM (see below)
World.ToJson();           // full snapshot as JSON (logging / future tool-calling)
```

`radius` is in metres and optional (defaults to `World.DefaultRadius` = 15).

---

## What comes back

```jsonc
World.GetPlayer()       -> { depthMeters, headingDegrees, headingCompass, x, y, z }
World.GetEnvironment()  -> { biome, depthMeters, seafloorDistanceMeters,
                             waterTemperatureC, visibilityMeters, lightLevel, waterColorHint }
World.GetCreatures(r)   -> [ { name, distanceMeters, direction, verticalPosition } ]
World.GetProps(r)       -> [ { name, category, distanceMeters, direction } ]
```

`direction` is relative to where the player looks: `ahead`, `ahead-left`, `left`,
`behind-left`, `behind`, `behind-right`, `right`, `ahead-right`.
`verticalPosition` is `above you` / `below you` / `same depth`.

`World.Describe()` example:
> "You are about 3.2m deep in the reef zone, 1.1m above the seabed. Water is ~24.7°C, bright,
> visibility ~22m. 2 creature(s) nearby: a Clownfish ahead (1.8m, same depth), a Seahorse
> ahead-left (4.2m, below you)."

---

## Feeding it to the Gemini assistant

`APIManager` sends a plain string to a Google Apps Script relay, so just prepend the description.
One line in `APIManager.SendDataToGas()`, right after `userPrompt` is resolved:

```csharp
userPrompt = World.Describe() + "\n\n" + userPrompt;
```

**Per-scene by design:** `World.Describe()` returns an empty string in scenes that aren't an
ocean/terrain scene (no water system and nothing around), so that line is automatically a no-op
in a menu or any other mode — no flags, no components, no per-scene setup. In a free-explore
scene it returns the summary. That's the whole story.

---

## Tweaks (optional)

`World` has a few static defaults you can set once (e.g. in a bootstrap) if needed:
`CreatureTag` (`"Actor"`), `PropTag` (`"Obstacle"`), `DefaultRadius` (15),
`MaxCreatures` (12), `MaxProps` (10), `DefaultWaterLevel` (8).
