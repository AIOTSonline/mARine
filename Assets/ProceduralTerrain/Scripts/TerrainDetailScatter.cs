using UnityEngine;

// Scatters props onto streamed terrain chunks. Each rule places its prefabs in one of two ways:
//   Cluster   – packs members into a domed mound (reefs, dense seaweed patches)
//   Scattered – drops members individually across the chunk (lone corals, single plants)
// Prefab-only; seeded per chunk so reefs rebuild identically when you return.
public class TerrainDetailScatter : ChunkDecorator
{
    public enum ScatterMode { Cluster, Scattered }

    [System.Serializable]
    public class ScatterRule
    {
        public string name = "Detail";
        public bool enabled = true;
        [Tooltip("Cluster = packed mound. Scattered = individual, independent props.")]
        public ScatterMode mode = ScatterMode.Cluster;

        [Header("Prefabs")]
        [Tooltip("Assign one or more prefabs/models — the rule picks among them.")]
        public GameObject[] members;
        [Tooltip("Cluster only: how many DIFFERENT members dominate one mound (1 = uniform).")]
        public Vector2Int paletteSize = new Vector2Int(2, 3);

        [Header("Density")]
        [Tooltip("Cluster: average mounds per 8 m chunk. Scattered: average individual props per chunk.")]
        [Range(0f, 40f)] public float densityPerChunk = 1.5f;
        [Tooltip("Only place where the lush-zone mask allows. Off = spread evenly everywhere.")]
        public bool respectGardenMask = true;

        [Header("Where it may grow")]
        [Range(0f, 90f)] public float minSlope = 0f;
        [Range(0f, 90f)] public float maxSlope = 40f;
        public Vector2 heightRange = new Vector2(-1000f, 1000f);

        [Header("Per-item")]
        [Tooltip("Real-world height in metres (min..max). Each prop is scaled to fit this, " +
                 "regardless of its model's native import size — keeps everything realistic.")]
        public Vector2 sizeMeters = new Vector2(0.3f, 0.8f);
        [Tooltip("0 = stand straight up, 1 = fully follow the ground slope.")]
        [Range(0f, 1f)] public float alignToNormal = 0.2f;
        [Tooltip("Random lean, degrees.")]
        public float randomTilt = 8f;
        [Tooltip("Fraction of height sunk into the surface so nothing floats.")]
        [Range(0f, 0.5f)] public float embed = 0.06f;

        [Header("Minimap")]
        [Tooltip("Register spawned props of this rule on the minimap.")]
        public bool showOnMinimap = true;
        public Color minimapColor = new Color(0.5f, 1f, 0.65f, 1f);

        [Header("Cluster shape (Cluster mode only)")]
        [Tooltip("Items per mound.")]
        public Vector2Int countRange = new Vector2Int(10, 18);
        [Tooltip("Mound footprint radius, metres.")]
        public float radius = 1.6f;
        [Range(0f, 0.6f)] public float radiusJitter = 0.3f;
        [Tooltip("Centre lift, metres — domes the mound. 0 = flat patch.")]
        public float domeHeight = 0.5f;
        [Range(0f, 0.6f)] public float domeJitter = 0.3f;
        [Tooltip("Extra scale on the centre item (1 = none). Builds a size hierarchy.")]
        public float centerScaleBoost = 1.4f;
    }

    [Header("Seed / streaming")]
    public int seed = 1337;
    [Tooltip("Decorative props rarely need shadows; off is much cheaper on mobile.")]
    public bool castShadows = false;
    [Tooltip("Safety cap on props instantiated per chunk (across all rules).")]
    public int maxPropsPerChunk = 240;

    [Header("Lush-zone mask (open sand between gardens)")]
    public bool useGardenMask = true;
    [Tooltip("Smaller = larger lush zones.")]
    public float gardenScale = 0.05f;
    [Range(0f, 1f)] public float gardenThreshold = 0.4f;
    [Range(0.02f, 1f)] public float gardenSharpness = 0.25f;

    [Header("Blend with environment (fix the 'pasted' look)")]
    [Tooltip("Optional: force ALL props onto this material so they share the scene fog/caustics.")]
    public Material unifyMaterial;
    [Tooltip("Cools props toward the water colour so they sit in the murk. 0 = untouched.")]
    [Range(0f, 1f)] public float waterTint = 0.18f;
    public Color waterTintColor = new Color(0.45f, 0.72f, 0.78f, 1f);

    [Header("Scatter rules")]
    public ScatterRule[] rules;

    const float GoldenAngle = 2.39996323f;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    MaterialPropertyBlock _mpb;
    int[] _paletteBuf;

    void OnValidate()
    {
        if (rules == null) return;
        foreach (var r in rules)
        {
            if (r.maxSlope < r.minSlope) r.maxSlope = r.minSlope;
            if (r.radius < 0.1f) r.radius = 0.1f;
        }
    }

    // Builds one chunk's props by running every rule. Returns a root GameObject (or null if bare).
    public override GameObject PopulateChunk(int chunkX, int chunkZ, Vector3 worldCentre,
                                             float worldHalfSize, Collider surface, Transform parent)
    {
        if (rules == null || rules.Length == 0 || surface == null) return null;

        var rng = new System.Random(HashChunk(chunkX, chunkZ, seed)); // deterministic per chunk
        GameObject root = null;
        int spawned = 0;
        float rayTop = worldCentre.y + 50f;

        for (int r = 0; r < rules.Length && spawned < maxPropsPerChunk; r++)
        {
            ScatterRule rule = rules[r];
            if (rule == null || !rule.enabled || rule.densityPerChunk <= 0f) continue;
            if (rule.members == null || rule.members.Length == 0) continue;

            int n = PoissonCount(rule.densityPerChunk, rng);
            for (int k = 0; k < n && spawned < maxPropsPerChunk; k++)
            {
                if (rule.mode == ScatterMode.Scattered)
                    spawned += TryPlaceSingle(rule, worldCentre, worldHalfSize, surface, rayTop,
                                              rng, parent, ref root, chunkX, chunkZ);
                else
                    spawned += TryBuildMound(rule, worldCentre, worldHalfSize, surface, rayTop,
                                             rng, parent, ref root, chunkX, chunkZ,
                                             maxPropsPerChunk - spawned);
            }
        }

        return root;
    }

    // One independent prop dropped at a random valid point in the chunk.
    int TryPlaceSingle(ScatterRule rule, Vector3 worldCentre, float worldHalfSize,
                       Collider surface, float rayTop, System.Random rng, Transform parent,
                       ref GameObject root, int chunkX, int chunkZ)
    {
        float x = worldCentre.x + Rand11(rng) * worldHalfSize;
        float z = worldCentre.z + Rand11(rng) * worldHalfSize;

        if (rule.respectGardenMask && (float)rng.NextDouble() > GardenFactor(x, z)) return 0;
        if (!SampleGround(surface, x, z, rayTop, out Vector3 p, out Vector3 nrm)) return 0;

        float slope = Vector3.Angle(nrm, Vector3.up);
        if (slope < rule.minSlope || slope > rule.maxSlope) return 0;
        if (p.y < rule.heightRange.x || p.y > rule.heightRange.y) return 0;

        GameObject prefab = rule.members[rng.Next(rule.members.Length)];
        if (prefab == null) return 0;

        EnsureRoot(ref root, parent, chunkX, chunkZ);
        SpawnMember(prefab, rule, rng, p, nrm, 1f, root.transform);
        return 1;
    }

    // One mound: inset centre to stay on the collider, then sunflower-pack a domed patch.
    int TryBuildMound(ScatterRule rule, Vector3 worldCentre, float worldHalfSize,
                      Collider surface, float rayTop, System.Random rng, Transform parent,
                      ref GameObject root, int chunkX, int chunkZ, int budget)
    {
        if (budget <= 0) return 0;

        float moundRadius = rule.radius * (1f + Rand11(rng) * rule.radiusJitter);
        moundRadius = Mathf.Max(0.3f, moundRadius);

        float inset = Mathf.Max(0f, worldHalfSize - moundRadius);
        float cx = worldCentre.x + Rand11(rng) * inset;
        float cz = worldCentre.z + Rand11(rng) * inset;

        if (rule.respectGardenMask && (float)rng.NextDouble() > GardenFactor(cx, cz)) return 0;
        if (!SampleGround(surface, cx, cz, rayTop, out Vector3 cp, out Vector3 cn)) return 0;

        float cs = Vector3.Angle(cn, Vector3.up);
        if (cs < rule.minSlope || cs > rule.maxSlope) return 0;
        if (cp.y < rule.heightRange.x || cp.y > rule.heightRange.y) return 0;

        int count = Mathf.Min(budget, Mathf.Max(1, RandRange(rng, rule.countRange.x, rule.countRange.y)));
        float moundDome = rule.domeHeight * (1f + Rand11(rng) * rule.domeJitter);
        float angleJitter = (float)rng.NextDouble() * Mathf.PI * 2f;
        int palCount = BuildPalette(rule, rng);

        EnsureRoot(ref root, parent, chunkX, chunkZ);
        int placed = 0;

        for (int i = 0; i < count; i++)
        {
            float tNorm = (i + 0.5f) / count;                       // 0 centre … 1 edge
            float rr = moundRadius * Mathf.Sqrt(tNorm) * Mathf.Lerp(0.82f, 1f, (float)rng.NextDouble());
            float ang = i * GoldenAngle + angleJitter;
            float ox = cx + Mathf.Cos(ang) * rr;
            float oz = cz + Mathf.Sin(ang) * rr;

            if (!SampleGround(surface, ox, oz, rayTop, out Vector3 hp, out Vector3 hn)) continue;
            float slope = Vector3.Angle(hn, Vector3.up);
            if (slope < rule.minSlope || slope > rule.maxSlope) continue;
            if (hp.y < rule.heightRange.x || hp.y > rule.heightRange.y) continue;

            GameObject prefab = rule.members[_paletteBuf[rng.Next(palCount)]];
            if (prefab == null) continue;

            float edge = (moundRadius > 0.001f) ? rr / moundRadius : 0f; // 0 centre … 1 edge
            float domeLift = moundDome * (1f - edge * edge);
            float sizeBoost = Mathf.Lerp(rule.centerScaleBoost, 1f, edge); // centre items a touch bigger

            SpawnMember(prefab, rule, rng, new Vector3(ox, hp.y + domeLift, oz), hn, sizeBoost, root.transform);
            placed++;
        }
        return placed;
    }

    // Instantiates one prop, orients it, scales it to a realistic real-world height
    // (independent of the model's native import scale), and seats it on the surface.
    void SpawnMember(GameObject prefab, ScatterRule rule, System.Random rng,
                     Vector3 restPoint, Vector3 groundNormal, float sizeBoost, Transform root)
    {
        GameObject go = Instantiate(prefab);
        Transform tr = go.transform;
        tr.SetParent(root, true);

        if (rule.showOnMinimap)
            go.AddComponent<MinimapMarker>().color = rule.minimapColor;

        Quaternion align = Quaternion.Slerp(Quaternion.identity,
            Quaternion.FromToRotation(Vector3.up, groundNormal), rule.alignToNormal);
        Quaternion yaw = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        Quaternion tilt = Quaternion.Euler(
            Rand11(rng) * rule.randomTilt, 0f, Rand11(rng) * rule.randomTilt);

        tr.rotation = align * yaw * tilt;
        tr.localScale = Vector3.one;
        tr.position = restPoint;

        var renderers = go.GetComponentsInChildren<Renderer>(false);
        ApplyLook(renderers);

        float targetHeight = Mathf.Lerp(rule.sizeMeters.x, rule.sizeMeters.y,
                                        (float)rng.NextDouble()) * sizeBoost;

        if (renderers.Length > 0)
        {
            // Measure native (scale-1) height, then scale so the prop equals targetHeight.
            Bounds nb = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) nb.Encapsulate(renderers[i].bounds);
            if (nb.size.y > 0.0001f && targetHeight > 0f)
                tr.localScale = Vector3.one * (targetHeight / nb.size.y);

            // Re-seat using the scaled bounds so nothing floats or sinks.
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            float lift = restPoint.y - b.min.y - rule.embed * b.size.y;
            tr.position += Vector3.up * lift;
        }
    }

    // Per-instance shadows, optional material override, and water-colour tint.
    void ApplyLook(Renderer[] renderers)
    {
        if (renderers.Length == 0) return;
        bool tint = waterTint > 0.001f;
        bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
        Color cool = Color.Lerp(Color.white, waterTintColor, waterTint);
        if (tint && _mpb == null) _mpb = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            Color baseCol = Color.white;
            Material src = r.sharedMaterial;
            if (src != null && src.HasProperty(BaseColorId))
                baseCol = src.GetColor(BaseColorId);

            if (!castShadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (unifyMaterial != null)
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++) mats[m] = unifyMaterial;
                r.sharedMaterials = mats;
            }

            if (tint)
            {
                Color final = baseCol * cool;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, linear ? final.linear : final);
                r.SetPropertyBlock(_mpb);
            }
        }
    }

    // Fills _paletteBuf[0..return) with distinct member indices (partial Fisher–Yates).
    int BuildPalette(ScatterRule rule, System.Random rng)
    {
        int n = rule.members.Length;
        if (_paletteBuf == null || _paletteBuf.Length < n) _paletteBuf = new int[n];
        for (int i = 0; i < n; i++) _paletteBuf[i] = i;

        int want = Mathf.Clamp(RandRange(rng, rule.paletteSize.x, rule.paletteSize.y), 1, n);
        for (int i = 0; i < want; i++)
        {
            int j = i + rng.Next(n - i);
            (_paletteBuf[i], _paletteBuf[j]) = (_paletteBuf[j], _paletteBuf[i]);
        }
        return want;
    }

    // Low-frequency world noise: 0 = bare sand, 1 = lush zone.
    float GardenFactor(float worldX, float worldZ)
    {
        if (!useGardenMask) return 1f;
        float nz = Mathf.PerlinNoise(worldX * gardenScale + 500f, worldZ * gardenScale + 500f);
        float t = Mathf.Clamp01((nz - gardenThreshold) / gardenSharpness);
        return t * t * (3f - 2f * t); // smoothstep
    }

    static void EnsureRoot(ref GameObject root, Transform parent, int chunkX, int chunkZ)
    {
        if (root != null) return;
        root = new GameObject($"Details ({chunkX},{chunkZ})");
        root.transform.SetParent(parent, false);
    }

    // Raycasts straight down onto the chunk collider for a seabed point + normal.
    static bool SampleGround(Collider surface, float x, float z, float rayTop,
                             out Vector3 point, out Vector3 normal)
    {
        var ray = new Ray(new Vector3(x, rayTop, z), Vector3.down);
        if (surface.Raycast(ray, out RaycastHit hit, rayTop * 2f + 100f))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }
        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    // Expected-value count: floor + a fractional chance for the remainder.
    static int PoissonCount(float mean, System.Random rng)
    {
        int n = Mathf.FloorToInt(mean);
        if ((float)rng.NextDouble() < (mean - n)) n++;
        return n;
    }

    static float Rand11(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

    static int RandRange(System.Random rng, int min, int maxInclusive)
        => (maxInclusive <= min) ? min : rng.Next(min, maxInclusive + 1);

    // Stable hash of a chunk coordinate → per-chunk random seed.
    static int HashChunk(int x, int z, int seed)
    {
        unchecked
        {
            int h = seed;
            h = h * 73856093 ^ x * 19349663 ^ z * 83492791;
            return h;
        }
    }
}
