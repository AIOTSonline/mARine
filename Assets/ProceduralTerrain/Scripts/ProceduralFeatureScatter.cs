using UnityEngine;
using System.Collections.Generic;

// Scatters procedurally-generated feature meshes (rock formations, kelp plants,
// glow anemones — see ProceduralMeshLibrary) onto streamed terrain chunks.
// Sister of TerrainDetailScatter, but instead of prefabs it shares a small
// library of runtime-built meshes: every arch in the world is one of a few
// mesh variants, so memory stays flat no matter how far you swim.
// Deterministic per chunk, so formations rebuild identically when you return.
public class ProceduralFeatureScatter : ChunkDecorator
{
    [System.Serializable]
    public class FeatureRule
    {
        public string name = "Feature";
        public bool enabled = true;
        public ProceduralMeshLibrary.FeatureKind kind = ProceduralMeshLibrary.FeatureKind.Boulder;

        [Tooltip("Material for every instance of this rule (use the Underwater* prop shaders).")]
        public Material material;
        [Tooltip("How many mesh variations to build for this rule. More = less repetition.")]
        [Range(1, 8)] public int variants = 4;

        [Header("Density")]
        [Tooltip("Average instances per 8 m chunk.")]
        [Range(0f, 30f)] public float densityPerChunk = 1.5f;
        [Tooltip("Cluster into fields using low-frequency world noise instead of spreading evenly.")]
        public bool useMask = false;
        [Tooltip("Smaller = larger fields.")]
        public float maskScale = 0.045f;
        [Range(0f, 1f)] public float maskThreshold = 0.55f;

        [Header("Where it may appear")]
        [Range(0f, 90f)] public float minSlope = 0f;
        [Range(0f, 90f)] public float maxSlope = 45f;
        public Vector2 heightRange = new Vector2(-1000f, 1000f);

        [Header("Per-instance")]
        [Tooltip("Real-world height in metres (min..max).")]
        public Vector2 sizeMeters = new Vector2(0.8f, 2f);
        [Tooltip("0 = stand straight up, 1 = fully follow the ground slope.")]
        [Range(0f, 1f)] public float alignToNormal = 0.25f;
        [Tooltip("Random lean, degrees.")]
        public float randomTilt = 6f;
        [Tooltip("Fraction of height sunk into the surface so nothing floats.")]
        [Range(0f, 0.5f)] public float embed = 0.08f;
        [Tooltip("Add a MeshCollider (only worth it for big swim-through formations).")]
        public bool addCollider = false;

        [Header("Talus (broken rubble around the base)")]
        [Tooltip("Rubble pieces shed around each formation. Real boulders sit in a skirt of " +
                 "their own broken-off fragments; without it the rock meets sand at a hard " +
                 "line, which is the 'pasted on' read. 0 disables it.")]
        public Vector2Int talusCount = Vector2Int.zero;
        [Tooltip("Outer edge of the rubble skirt, as a multiple of the formation's footprint.")]
        public float talusSpread = 1.7f;
        [Tooltip("Largest fragment as a fraction of the formation's height.")]
        [Range(0.01f, 0.5f)] public float talusSizeFraction = 0.14f;
        [Tooltip("Higher = more small chips and fewer big slabs.")]
        [Range(1f, 6f)] public float talusSizeExponent = 2.5f;

        [Header("Minimap")]
        public bool showOnMinimap = false;
        public Color minimapColor = new Color(0.7f, 0.75f, 0.8f, 1f);
    }

    public static bool IsRockKind(ProceduralMeshLibrary.FeatureKind kind)
    {
        return kind == ProceduralMeshLibrary.FeatureKind.Boulder
            || kind == ProceduralMeshLibrary.FeatureKind.Spire
            || kind == ProceduralMeshLibrary.FeatureKind.Arch
            || kind == ProceduralMeshLibrary.FeatureKind.Overhang
            || kind == ProceduralMeshLibrary.FeatureKind.Grotto;
    }

    [Header("Seed")]
    public int seed = 7331;

    [Header("Rendering")]
    [Tooltip("Decorative props rarely need shadows; off is much cheaper on mobile.")]
    public bool castShadows = false;
    [Tooltip("Safety cap on instances per chunk (across all rules).")]
    public int maxPropsPerChunk = 80;
    [Tooltip("When set, every rock-kind rule (boulder/spire/arch/overhang/grotto) is forced " +
             "onto this material, so an arch cannot end up a different colour from the " +
             "boulders around it. Leave empty to use each rule's own material.")]
    public Material sharedRockMaterial;

    [Header("Feature rules")]
    public FeatureRule[] rules;

    // Mesh library: built once, shared by every instance. Keyed per rule index
    // so two rules with the same kind still get distinct variant sets.
    Mesh[][] _variantCache;

    void OnValidate()
    {
        if (rules == null) return;
        foreach (var r in rules)
            if (r != null && r.maxSlope < r.minSlope) r.maxSlope = r.minSlope;
        _variantCache = null; // rebuild with new settings next time
    }

    Mesh GetMesh(int ruleIndex, int variant)
    {
        if (_variantCache == null || _variantCache.Length != rules.Length)
            _variantCache = new Mesh[rules.Length][];

        FeatureRule rule = rules[ruleIndex];
        var set = _variantCache[ruleIndex];
        if (set == null || set.Length != rule.variants)
            _variantCache[ruleIndex] = set = new Mesh[rule.variants];

        int v = Mathf.Abs(variant) % rule.variants;
        if (set[v] == null)
            set[v] = ProceduralMeshLibrary.Build(rule.kind,
                seed * 31 + ruleIndex * 977 + v * 7919 + (int)rule.kind * 53);
        return set[v];
    }

    public override GameObject PopulateChunk(int chunkX, int chunkZ, Vector3 worldCentre,
                                             float worldHalfSize, Collider surface, Transform parent)
    {
        if (rules == null || rules.Length == 0 || surface == null) return null;

        var rng = new System.Random(HashChunk(chunkX, chunkZ, seed));
        GameObject root = null;
        int spawned = 0;
        float rayTop = worldCentre.y + 50f;

        List<CombineInstance>[] talus = null;

        for (int r = 0; r < rules.Length && spawned < maxPropsPerChunk; r++)
        {
            FeatureRule rule = rules[r];
            if (rule == null || !rule.enabled || rule.densityPerChunk <= 0f) continue;
            if (MaterialFor(rule) == null) continue;

            int n = PoissonCount(rule.densityPerChunk, rng);
            for (int k = 0; k < n && spawned < maxPropsPerChunk; k++)
            {
                float x = worldCentre.x + Rand11(rng) * worldHalfSize;
                float z = worldCentre.z + Rand11(rng) * worldHalfSize;

                if (rule.useMask && (float)rng.NextDouble() > MaskFactor(x, z, rule)) continue;
                if (!SampleGround(surface, x, z, rayTop, out Vector3 p, out Vector3 nrm)) continue;

                float slope = Vector3.Angle(nrm, Vector3.up);
                if (slope < rule.minSlope || slope > rule.maxSlope) continue;
                if (p.y < rule.heightRange.x || p.y > rule.heightRange.y) continue;

                EnsureRoot(ref root, parent, chunkX, chunkZ);

                float footprint = SpawnFeature(rule, r, rng, p, nrm, root.transform);
                spawned++;

                if (rule.talusCount.y > 0 && footprint > 0.001f)
                {
                    talus ??= new List<CombineInstance>[rules.Length];
                    talus[r] ??= new List<CombineInstance>();
                    BuildTalus(rule, r, rng, p, footprint, surface, rayTop, talus[r]);
                }
            }
        }

        if (talus != null)
        {
            for (int r = 0; r < talus.Length; r++)
            {
                if (talus[r] == null || talus[r].Count == 0) continue;
                EnsureRoot(ref root, parent, chunkX, chunkZ);
                CommitTalus(rules[r], talus[r], root.transform);
            }
        }

        return root;
    }

    // Rubble is merged into one mesh per rule per chunk: a skirt of fragments costs
    // one extra draw call, not one per pebble.
    void BuildTalus(FeatureRule rule, int ruleIndex, System.Random rng, Vector3 basePoint,
                    float footprint, Collider surface, float rayTop, List<CombineInstance> sink)
    {
        int n = RandRange(rng, rule.talusCount.x, rule.talusCount.y);
        float maxSize = Mathf.Max(0.01f, rule.sizeMeters.y * rule.talusSizeFraction);
        float inner = footprint * 0.65f;
        float outer = Mathf.Max(inner + 0.05f, footprint * rule.talusSpread);

        for (int i = 0; i < n; i++)
        {
            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float rad = Mathf.Lerp(inner, outer, Mathf.Sqrt((float)rng.NextDouble()));
            float x = basePoint.x + Mathf.Cos(ang) * rad;
            float z = basePoint.z + Mathf.Sin(ang) * rad;

            if (!SampleGround(surface, x, z, rayTop, out Vector3 hp, out Vector3 hn)) continue;

            Mesh piece = GetMesh(ruleIndex, rng.Next(1 << 20));
            Bounds b = piece.bounds;
            if (b.size.y <= 0.0001f) continue;

            float u = Mathf.Pow((float)rng.NextDouble(), rule.talusSizeExponent);
            float target = Mathf.Lerp(maxSize * 0.18f, maxSize, u);
            float scale = target / b.size.y;

            Quaternion rot = Quaternion.Slerp(Quaternion.identity,
                                 Quaternion.FromToRotation(Vector3.up, hn), 0.6f)
                           * Quaternion.Euler((float)rng.NextDouble() * 360f,
                                              (float)rng.NextDouble() * 360f,
                                              (float)rng.NextDouble() * 360f);

            Vector3 pos = hp + Vector3.up * (-b.min.y * scale - target * 0.35f);

            sink.Add(new CombineInstance
            {
                mesh = piece,
                transform = Matrix4x4.TRS(pos, rot, Vector3.one * scale),
            });
        }
    }

    void CommitTalus(FeatureRule rule, List<CombineInstance> pieces, Transform root)
    {
        var combined = new Mesh { name = "Talus" };
        combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combined.CombineMeshes(pieces.ToArray(), true, true);
        combined.RecalculateBounds();

        var go = new GameObject("Talus");
        go.transform.SetParent(root, false);
        go.AddComponent<MeshFilter>().sharedMesh = combined;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = MaterialFor(rule);
        if (!castShadows)
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    Material MaterialFor(FeatureRule rule)
    {
        if (sharedRockMaterial != null && IsRockKind(rule.kind)) return sharedRockMaterial;
        return rule.material;
    }

    static int RandRange(System.Random rng, int lo, int hi)
    {
        if (hi < lo) hi = lo;
        return rng.Next(lo, hi + 1);
    }

    static void EnsureRoot(ref GameObject root, Transform parent, int chunkX, int chunkZ)
    {
        if (root != null) return;
        root = new GameObject($"Features ({chunkX},{chunkZ})");
        root.transform.SetParent(parent, false);
    }

    // Returns the placed formation's ground footprint radius, so talus can be
    // shed in a skirt that actually matches how big the rock turned out.
    float SpawnFeature(FeatureRule rule, int ruleIndex, System.Random rng,
                       Vector3 groundPoint, Vector3 groundNormal, Transform root)
    {
        Mesh mesh = GetMesh(ruleIndex, rng.Next(1 << 20));

        var go = new GameObject(rule.name);
        go.transform.SetParent(root, false);

        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialFor(rule);
        if (!castShadows)
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (rule.showOnMinimap)
            go.AddComponent<MinimapMarker>().color = rule.minimapColor;

        // Orientation: partial slope alignment + random yaw + a small lean.
        Quaternion align = Quaternion.Slerp(Quaternion.identity,
            Quaternion.FromToRotation(Vector3.up, groundNormal), rule.alignToNormal);
        Quaternion yaw = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        Quaternion tilt = Quaternion.Euler(Rand11(rng) * rule.randomTilt, 0f, Rand11(rng) * rule.randomTilt);
        go.transform.rotation = align * yaw * tilt;

        // Scale so the mesh's native height hits the target metres, then seat
        // it so its (scaled) base sits at the ground minus the embed fraction.
        float target = Mathf.Lerp(rule.sizeMeters.x, rule.sizeMeters.y, (float)rng.NextDouble());
        Bounds b = mesh.bounds;
        float scale = (b.size.y > 0.0001f) ? target / b.size.y : 1f;
        go.transform.localScale = Vector3.one * scale;
        go.transform.position = groundPoint + Vector3.up * (-b.min.y * scale - rule.embed * target);

        if (rule.addCollider)
        {
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh; // non-convex: you can swim through arches/grottos
        }

        return Mathf.Max(b.size.x, b.size.z) * scale * 0.5f;
    }

    // Low-frequency world noise: 0 = skip here, 1 = field of this feature.
    float MaskFactor(float worldX, float worldZ, FeatureRule rule)
    {
        float nz = Mathf.PerlinNoise(worldX * rule.maskScale + 917f, worldZ * rule.maskScale + 917f);
        float t = Mathf.Clamp01((nz - rule.maskThreshold) / 0.25f);
        return t * t * (3f - 2f * t);
    }

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

    static int PoissonCount(float mean, System.Random rng)
    {
        int n = Mathf.FloorToInt(mean);
        if ((float)rng.NextDouble() < (mean - n)) n++;
        return n;
    }

    static float Rand11(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

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
