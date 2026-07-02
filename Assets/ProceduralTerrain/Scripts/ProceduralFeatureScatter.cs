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

        [Header("Minimap")]
        public bool showOnMinimap = false;
        public Color minimapColor = new Color(0.7f, 0.75f, 0.8f, 1f);
    }

    [Header("Seed")]
    public int seed = 7331;

    [Header("Rendering")]
    [Tooltip("Decorative props rarely need shadows; off is much cheaper on mobile.")]
    public bool castShadows = false;
    [Tooltip("Safety cap on instances per chunk (across all rules).")]
    public int maxPropsPerChunk = 80;

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

        for (int r = 0; r < rules.Length && spawned < maxPropsPerChunk; r++)
        {
            FeatureRule rule = rules[r];
            if (rule == null || !rule.enabled || rule.densityPerChunk <= 0f) continue;
            if (rule.material == null) continue;

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

                if (root == null)
                {
                    root = new GameObject($"Features ({chunkX},{chunkZ})");
                    root.transform.SetParent(parent, false);
                }

                SpawnFeature(rule, r, rng, p, nrm, root.transform);
                spawned++;
            }
        }

        return root;
    }

    void SpawnFeature(FeatureRule rule, int ruleIndex, System.Random rng,
                      Vector3 groundPoint, Vector3 groundNormal, Transform root)
    {
        Mesh mesh = GetMesh(ruleIndex, rng.Next(1 << 20));

        var go = new GameObject(rule.name);
        go.transform.SetParent(root, false);

        var filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = rule.material;
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
