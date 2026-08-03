using UnityEngine;
using System.Collections.Generic;

// Builds the low-poly prop meshes the biomes scatter around: rocks, spires,
// swim-through arches, mushroom overhangs, hollow grotto caves, kelp plants and
// glow anemones. This is the workaround for a 2D heightmap having no overhangs:
// the terrain stays a cheap heightfield, and true 3D features are separate
// meshes seated on top of it.
//
// Everything is deterministic per seed, generated once at startup and shared by
// every instance (no per-instance mesh memory). Meshes are flat-shaded triangle
// soup with baked vertex colour: rgb = subtle tint variation, a = ambient
// occlusion (grotto interiors bake dark = instant cave mood without lights).
// No textures anywhere — the rock/kelp shaders shade with colour ramps, so the
// whole feature set costs a few small meshes and zero texture memory.
public static class ProceduralMeshLibrary
{
    public enum FeatureKind
    {
        Boulder,     // classic rounded rock, flattened base
        Spire,       // tall tapering pinnacle
        Arch,        // half-torus you can swim through
        Overhang,    // mushroom/table rock: wide cap on a narrow stem
        Grotto,      // hollow dome with a mouth — a fake cave
        KelpPlant,   // fan of tall ribbons (sways in the vertex shader)
        GlowAnemone, // squat dome with emissive tentacle tips
        SeaFan       // branching gorgonian fan (the classic reef silhouette)
    }

    // Nominal size ~1 m tall; the scatter rescales via renderer bounds.
    public static Mesh Build(FeatureKind kind, int seed)
    {
        var rng = new System.Random(seed);
        switch (kind)
        {
            case FeatureKind.Spire:       return BuildSpire(rng, seed);
            case FeatureKind.Arch:        return BuildArch(rng, seed);
            case FeatureKind.Overhang:    return BuildOverhang(rng, seed);
            case FeatureKind.Grotto:      return BuildGrotto(rng, seed);
            case FeatureKind.KelpPlant:   return BuildKelpPlant(rng, seed);
            case FeatureKind.GlowAnemone: return BuildGlowAnemone(rng, seed);
            case FeatureKind.SeaFan:      return BuildSeaFan(rng, seed);
            default:                      return BuildBoulder(rng, seed);
        }
    }

    // ── Rocks ────────────────────────────────────────────────────────────────

    static Mesh BuildBoulder(System.Random rng, int seed)
    {
        List<Vector3> verts; List<int> tris;
        Icosphere(2, out verts, out tris);

        var style = RockStyle.Random(rng, seed, 0.2f);

        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 v = style.ShapeFromDirection(verts[i].normalized) * 0.5f;
            verts[i] = RockStyle.SeatBase(v, -0.18f, 0.14f);
        }

        return FinishFlat(verts, tris, seed,
            ao: p => Mathf.Clamp01(0.6f + p.y * 0.9f));
    }

    static Mesh BuildSpire(System.Random rng, int seed)
    {
        var d = new Draft();
        int rings = 9, segs = 8;
        float height = 1f;
        float baseR = Lerp(rng, 0.16f, 0.24f);
        float lean = Lerp(rng, 0f, 0.12f);
        float leanAng = Lerp(rng, 0f, Mathf.PI * 2f);

        var ringVerts = new Vector3[rings + 1, segs];
        for (int r = 0; r <= rings; r++)
        {
            float t = r / (float)rings;
            // Tapering column with a slight bulge low down and wobbling centre.
            float radius = baseR * Mathf.Pow(1f - t, 0.72f) * (1f + 0.25f * Mathf.Sin(t * 9f + seed)) + 0.015f;
            Vector3 centre = new Vector3(
                Mathf.Cos(leanAng) * lean * t * t,
                t * height,
                Mathf.Sin(leanAng) * lean * t * t);

            for (int s = 0; s < segs; s++)
            {
                float a = s / (float)segs * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float n = 1f + 0.35f * Fbm(new Vector3(a * 0.7f, t * 3.1f, 0f), 2, seed);
                ringVerts[r, s] = centre + dir * (radius * n);
            }
        }

        for (int r = 0; r < rings; r++)
            for (int s = 0; s < segs; s++)
            {
                int s1 = (s + 1) % segs;
                d.Quad(ringVerts[r, s], ringVerts[r, s1], ringVerts[r + 1, s1], ringVerts[r + 1, s]);
            }

        // Top cap fan.
        Vector3 tip = new Vector3(Mathf.Cos(leanAng) * lean, height + 0.03f, Mathf.Sin(leanAng) * lean);
        for (int s = 0; s < segs; s++)
            d.Tri(ringVerts[rings, s], ringVerts[rings, (s + 1) % segs], tip);

        return d.ToFlatMesh(seed, ao: p => Mathf.Clamp01(0.55f + p.y * 0.6f));
    }

    static Mesh BuildArch(System.Random rng, int seed)
    {
        var d = new Draft();
        int steps = 12, segs = 7;
        float major = 0.5f;
        float minor = Lerp(rng, 0.10f, 0.15f);
        float widen = Lerp(rng, 1.1f, 1.5f); // feet thicker than the crown

        // Same rock language as the boulders (displacement + bedding), but with the
        // fracture half-spaces cleared: those are sized against a solid ellipsoid and
        // would slice a swept arch in half. Without this the arch reads as a smooth
        // tube next to faceted boulders — a different material entirely.
        var style = RockStyle.Random(rng, seed, 0.55f);
        style.fractures = null;

        var ringCache = new Vector3[steps + 1, segs];
        for (int i = 0; i <= steps; i++)
        {
            // Sweep slightly past the horizontal so the feet embed in the sand.
            float theta = Mathf.Lerp(-0.15f, Mathf.PI + 0.15f, i / (float)steps);
            Vector3 centre = new Vector3(Mathf.Cos(theta) * major, Mathf.Sin(theta) * major, 0f);

            float crown = Mathf.Sin(theta);                       // 1 at apex, 0 at feet
            float thick = minor * Mathf.Lerp(widen, 1f, crown);

            for (int s = 0; s < segs; s++)
            {
                float a = s / (float)segs * Mathf.PI * 2f;
                // Ring frame around the tube: normal (radial) and binormal (Z).
                Vector3 radial = new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0f);
                Vector3 outward = radial * Mathf.Cos(a) + Vector3.forward * Mathf.Sin(a);
                Vector3 p = centre + outward * thick;
                ringCache[i, s] = style.Weather(p, outward, centre, thick * 2.2f);
            }
        }

        for (int i = 0; i < steps; i++)
            for (int s = 0; s < segs; s++)
            {
                int s1 = (s + 1) % segs;
                d.Quad(ringCache[i, s], ringCache[i, s1], ringCache[i + 1, s1], ringCache[i + 1, s]);
            }

        return d.ToFlatMesh(seed,
            ao: p => Mathf.Clamp01(0.6f + p.y * 0.7f));
    }

    static Mesh BuildOverhang(System.Random rng, int seed)
    {
        var d = new Draft();
        int rings = 10, segs = 9;
        float capR = Lerp(rng, 0.42f, 0.55f);
        float stemR = Lerp(rng, 0.13f, 0.2f);

        var ringVerts = new Vector3[rings + 1, segs];
        for (int r = 0; r <= rings; r++)
        {
            float t = r / (float)rings;
            // Profile: narrow stem, sudden wide cap, rounded top = table rock.
            float radius;
            if      (t < 0.55f) radius = Mathf.Lerp(stemR * 1.4f, stemR, t / 0.55f);
            else if (t < 0.75f) radius = Mathf.Lerp(stemR, capR, SmoothStep((t - 0.55f) / 0.2f));
            else                radius = Mathf.Lerp(capR, capR * 0.45f, SmoothStep((t - 0.75f) / 0.25f));

            for (int s = 0; s < segs; s++)
            {
                float a = s / (float)segs * Mathf.PI * 2f;
                float n = 1f + 0.3f * Fbm(new Vector3(Mathf.Cos(a), t * 2.7f, Mathf.Sin(a)) * 1.9f, 2, seed);
                ringVerts[r, s] = new Vector3(Mathf.Cos(a) * radius * n, t, Mathf.Sin(a) * radius * n);
            }
        }

        for (int r = 0; r < rings; r++)
            for (int s = 0; s < segs; s++)
            {
                int s1 = (s + 1) % segs;
                d.Quad(ringVerts[r, s], ringVerts[r, s1], ringVerts[r + 1, s1], ringVerts[r + 1, s]);
            }

        Vector3 top = new Vector3(0f, 1.02f, 0f);
        for (int s = 0; s < segs; s++)
            d.Tri(ringVerts[rings, s], ringVerts[rings, (s + 1) % segs], top);

        // Underside of the cap bakes dark — reads as shadow under the ledge.
        return d.ToFlatMesh(seed,
            ao: p =>
            {
                float underCap = Mathf.Clamp01((p.y - 0.35f) / 0.3f) *
                                 Mathf.Clamp01((0.72f - p.y) / 0.2f);
                return Mathf.Clamp01(0.65f + p.y * 0.45f - underCap * 0.45f);
            });
    }

    // Hollow squashed dome with a mouth opening: outer shell, darker inner
    // shell, and a bridged rim so the wall reads as solid rock.
    static Mesh BuildGrotto(System.Random rng, int seed)
    {
        int lat = 7, lon = 14;
        float outerR = 0.5f, innerR = 0.40f, squash = 0.72f;
        float mouthYaw = Lerp(rng, 0f, Mathf.PI * 2f);
        Vector3 mouthDir = new Vector3(Mathf.Cos(mouthYaw), -0.15f, Mathf.Sin(mouthYaw)).normalized;
        float mouthCos = Mathf.Cos(Lerp(rng, 0.55f, 0.7f)); // ~32–40° opening

        var d = new Draft();

        // Lattice of directions; theta sweeps a bit past the equator to embed.
        var dirs = new Vector3[lat + 1, lon];
        for (int i = 0; i <= lat; i++)
        {
            float theta = i / (float)lat * (Mathf.PI * 0.62f);
            for (int j = 0; j < lon; j++)
            {
                float phi = j / (float)lon * Mathf.PI * 2f;
                dirs[i, j] = new Vector3(
                    Mathf.Sin(theta) * Mathf.Cos(phi),
                    Mathf.Cos(theta),
                    Mathf.Sin(theta) * Mathf.Sin(phi));
            }
        }

        System.Func<Vector3, float, Vector3> shape = (dir, radius) =>
        {
            float n = 1f + 0.22f * Fbm(dir * 2.2f, 3, seed);
            Vector3 v = dir * (radius * n);
            v.y *= squash;
            return v;
        };

        // A cell is "open" (part of the mouth) if its centre direction falls
        // inside the mouth cone. Same test for both shells => matching holes.
        System.Func<int, int, bool> cellOpen = (i, j) =>
        {
            Vector3 c = (dirs[i, j] + dirs[i + 1, j] + dirs[i, (j + 1) % lon] + dirs[i + 1, (j + 1) % lon]).normalized;
            return Vector3.Dot(c, mouthDir) > mouthCos;
        };

        for (int i = 0; i < lat; i++)
        {
            for (int j = 0; j < lon; j++)
            {
                int j1 = (j + 1) % lon;
                Vector3 o00 = shape(dirs[i, j], outerR),  o10 = shape(dirs[i + 1, j], outerR),
                        o01 = shape(dirs[i, j1], outerR), o11 = shape(dirs[i + 1, j1], outerR);
                Vector3 n00 = shape(dirs[i, j], innerR),  n10 = shape(dirs[i + 1, j], innerR),
                        n01 = shape(dirs[i, j1], innerR), n11 = shape(dirs[i + 1, j1], innerR);

                if (cellOpen(i, j))
                {
                    // Mouth cell: bridge outer to inner along the edges that
                    // border solid cells, forming the rocky rim of the opening.
                    if (i > 0 && !cellOpen(i - 1, j)) d.Quad(o00, o01, n01, n00);
                    if (i < lat - 1 && !cellOpen(i + 1, j)) d.Quad(o11, o10, n10, n11);
                    if (!cellOpen(i, (j - 1 + lon) % lon)) d.Quad(o10, o00, n00, n10);
                    if (!cellOpen(i, j1)) d.Quad(o01, o11, n11, n01);
                    continue;
                }

                d.Quad(o00, o01, o11, o10);   // outer, facing out
                d.Quad(n00, n10, n11, n01);   // inner, facing in
            }
        }

        // Interior bakes very dark, brightening toward the mouth — cave light.
        return d.ToFlatMesh(seed,
            ao: p =>
            {
                float r = new Vector3(p.x, p.y / squash, p.z).magnitude;
                float inside = Mathf.Clamp01((outerR * 0.97f - r) / (outerR - innerR));
                float towardMouth = Mathf.Clamp01(Vector3.Dot(p.normalized, mouthDir));
                float interiorAO = Mathf.Lerp(0.14f, 0.5f, towardMouth * towardMouth);
                return Mathf.Lerp(Mathf.Clamp01(0.6f + p.y * 0.8f), interiorAO, inside);
            });
    }

    // ── Life ─────────────────────────────────────────────────────────────────

    static Mesh BuildKelpPlant(System.Random rng, int seed)
    {
        var d = new Draft();
        int blades = 3 + rng.Next(3);
        int segsY = 7;

        for (int b = 0; b < blades; b++)
        {
            float yaw = (b / (float)blades) * Mathf.PI * 2f + Lerp(rng, -0.3f, 0.3f);
            float bladeH = Lerp(rng, 0.75f, 1f);
            float w0 = Lerp(rng, 0.05f, 0.08f);
            float drift = Lerp(rng, 0.06f, 0.16f);
            Vector3 right = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
            Vector3 fwd = new Vector3(-Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
            float hue = Lerp(rng, -0.5f, 0.5f);
            Color tint = new Color(1f + hue * 0.12f, 1f, 1f - hue * 0.1f, 1f);

            var prev = new Vector3[2];
            var prevUV = new Vector2[2];
            for (int i = 0; i <= segsY; i++)
            {
                float t = i / (float)segsY;
                float width = w0 * (1f - t * 0.55f);
                // Gentle S-curve so blades aren't rigid poles even at rest.
                Vector3 centre = fwd * (drift * Mathf.Sin(t * 3.1f + b)) + right * (drift * 0.4f * Mathf.Sin(t * 5.3f + seed))
                               + Vector3.up * (t * bladeH);
                Vector3 a = centre - right * width;
                Vector3 c = centre + right * width;
                var uvA = new Vector2(0f, t);
                var uvC = new Vector2(1f, t);

                if (i > 0)
                {
                    d.QuadUV(prev[0], prev[1], c, a, prevUV[0], prevUV[1], uvC, uvA, tint);
                }
                prev[0] = a; prev[1] = c;
                prevUV[0] = uvA; prevUV[1] = uvC;
            }
        }

        // Smooth normals suit ribbons; uv.y drives sway + colour ramp in shader.
        return d.ToSmoothMesh();
    }

    static Mesh BuildGlowAnemone(System.Random rng, int seed)
    {
        List<Vector3> verts; List<int> tris;
        Icosphere(1, out verts, out tris);

        var d = new Draft();
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = Squash(verts[tris[i]]), b = Squash(verts[tris[i + 1]]), c = Squash(verts[tris[i + 2]]);
            d.Tri(a, b, c);
        }

        // Tentacle spikes on the upper half; their tips carry the glow mask.
        int spikes = 8 + rng.Next(7);
        for (int s = 0; s < spikes; s++)
        {
            float yaw = Lerp(rng, 0f, Mathf.PI * 2f);
            float pitch = Lerp(rng, 0.35f, 1.35f);
            Vector3 dir = new Vector3(
                Mathf.Cos(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Sin(yaw) * Mathf.Cos(pitch)).normalized;
            Vector3 baseP = Squash(dir) * 0.92f;
            Vector3 tip = baseP + dir * Lerp(rng, 0.22f, 0.42f);
            Vector3 side = Vector3.Cross(dir, Vector3.up).normalized * 0.035f;
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right * 0.035f;
            Vector3 side2 = Vector3.Cross(dir, side).normalized * 0.035f;

            // Tiny 3-sided pyramid per tentacle; glow mask 1 at the tip.
            d.TriGlow(baseP + side, baseP - side * 0.5f + side2, tip, 0.1f, 0.1f, 1f);
            d.TriGlow(baseP - side * 0.5f + side2, baseP - side * 0.5f - side2, tip, 0.1f, 0.1f, 1f);
            d.TriGlow(baseP - side * 0.5f - side2, baseP + side, tip, 0.1f, 0.1f, 1f);
        }

        return d.ToFlatMesh(seed, ao: p => 1f, glowIsBaked: true);

        Vector3 Squash(Vector3 v)
        {
            Vector3 r = v.normalized * 0.5f;
            r.y *= 0.55f;
            if (r.y < 0f) r.y *= 0.35f; // sit nearly flat on the ground
            return r;
        }
    }

    // Gorgonian sea fan: a mostly-planar recursive branch structure, like the
    // pink fans silhouetted against the water in every reef shot. Branch quads
    // are emitted double-sided so the fan reads from both directions.
    static Mesh BuildSeaFan(System.Random rng, int seed)
    {
        var d = new Draft();
        int trunks = 3 + rng.Next(3);
        float spread = Lerp(rng, 0.55f, 0.85f);

        void Branch(Vector3 pos, float angle, float len, float width, int depth)
        {
            if (depth == 0 || width < 0.004f) return;

            Vector3 dir = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f);
            Vector3 end = pos + dir * len;
            end.z += Lerp(rng, -0.025f, 0.025f);  // slight out-of-plane wobble

            Vector3 side = new Vector3(-dir.y, dir.x, 0f) * width;
            Vector3 sideEnd = side * 0.6f;        // taper toward the tip
            d.Quad(pos - side, pos + side, end + sideEnd, end - sideEnd);
            d.Quad(end - sideEnd, end + sideEnd, pos + side, pos - side); // back face

            int kids = 2 + (rng.Next(100) < 35 ? 1 : 0);
            for (int k = 0; k < kids; k++)
            {
                float t = kids == 1 ? 0f : (k / (float)(kids - 1)) * 2f - 1f;
                float childAngle = angle + t * spread * Lerp(rng, 0.6f, 1f);
                Branch(end, childAngle, len * Lerp(rng, 0.58f, 0.72f), width * 0.62f, depth - 1);
            }
        }

        for (int b = 0; b < trunks; b++)
        {
            float t = trunks == 1 ? 0f : (b / (float)(trunks - 1)) * 2f - 1f;
            Branch(new Vector3(t * 0.03f, 0f, 0f), t * spread * 0.8f,
                   Lerp(rng, 0.3f, 0.38f), Lerp(rng, 0.02f, 0.03f), 5);
        }

        // Fans brighten toward the tips, like light shining through the mesh.
        return d.ToFlatMesh(seed, ao: p => Mathf.Clamp01(0.55f + p.y * 0.5f));
    }

    // Converts an indexed mesh (e.g. a displaced icosphere) into a flat-shaded
    // Draft and bakes it.
    static Mesh FinishFlat(List<Vector3> verts, List<int> tris, int seed,
                           System.Func<Vector3, float> ao)
    {
        var d = new Draft();
        for (int i = 0; i < tris.Count; i += 3)
            d.Tri(verts[tris[i]], verts[tris[i + 1]], verts[tris[i + 2]]);
        return d.ToFlatMesh(seed, ao);
    }

    // ── Draft: triangle-soup builder ─────────────────────────────────────────

    class Draft
    {
        public readonly List<Vector3> V = new List<Vector3>();
        public readonly List<int> T = new List<int>();
        public readonly List<Vector2> UV = new List<Vector2>();
        public readonly List<Color> C = new List<Color>();

        public void Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c);
            UV.Add(Vector2.zero); UV.Add(Vector2.zero); UV.Add(Vector2.zero);
            C.Add(Color.white); C.Add(Color.white); C.Add(Color.white);
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
        }

        public void TriGlow(Vector3 a, Vector3 b, Vector3 c, float ga, float gb, float gc)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c);
            UV.Add(Vector2.zero); UV.Add(Vector2.zero); UV.Add(Vector2.zero);
            C.Add(new Color(1f, 1f, 1f, ga)); C.Add(new Color(1f, 1f, 1f, gb)); C.Add(new Color(1f, 1f, 1f, gc));
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(a, b, c);
            Tri(a, c, d);
        }

        public void QuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                           Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD, Color col)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c); V.Add(d);
            UV.Add(uvA); UV.Add(uvB); UV.Add(uvC); UV.Add(uvD);
            C.Add(col); C.Add(col); C.Add(col); C.Add(col);
            T.Add(i); T.Add(i + 1); T.Add(i + 2);
            T.Add(i); T.Add(i + 2); T.Add(i + 3);
        }

        // Flat-shaded output: verts already duplicated per triangle, so face
        // normals come free. AO callback bakes into vertex alpha; rgb gets a
        // faint positional tint so big rocks aren't a single flat colour.
        public Mesh ToFlatMesh(int seed, System.Func<Vector3, float> ao, bool glowIsBaked = false)
        {
            var normals = new Vector3[V.Count];
            var colors = new Color[V.Count];

            for (int i = 0; i < T.Count; i += 3)
            {
                Vector3 n = Vector3.Cross(V[T[i + 1]] - V[T[i]], V[T[i + 2]] - V[T[i]]).normalized;
                normals[T[i]] = normals[T[i + 1]] = normals[T[i + 2]] = n;
            }

            for (int i = 0; i < V.Count; i++)
            {
                float tint = 1f + 0.08f * Fbm(V[i] * 3.7f, 2, seed + 91);
                float a = glowIsBaked ? C[i].a : Mathf.Clamp01(ao(V[i]));
                colors[i] = new Color(C[i].r * tint, C[i].g * tint, C[i].b * tint, a);
            }

            return Bake(normals, colors);
        }

        public Mesh ToSmoothMesh()
        {
            var mesh = Bake(null, C.ToArray());
            mesh.RecalculateNormals();
            return mesh;
        }

        Mesh Bake(Vector3[] normals, Color[] colors)
        {
            var mesh = new Mesh();
            mesh.SetVertices(V);
            mesh.SetTriangles(T, 0);
            mesh.SetUVs(0, UV);
            mesh.SetColors(colors);
            if (normals != null) mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // ── Primitives & noise ───────────────────────────────────────────────────

    static void Icosphere(int subdivisions, out List<Vector3> verts, out List<int> tris)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        verts = new List<Vector3>
        {
            new Vector3(-1,  t, 0).normalized, new Vector3( 1,  t, 0).normalized,
            new Vector3(-1, -t, 0).normalized, new Vector3( 1, -t, 0).normalized,
            new Vector3(0, -1,  t).normalized, new Vector3(0,  1,  t).normalized,
            new Vector3(0, -1, -t).normalized, new Vector3(0,  1, -t).normalized,
            new Vector3( t, 0, -1).normalized, new Vector3( t, 0,  1).normalized,
            new Vector3(-t, 0, -1).normalized, new Vector3(-t, 0,  1).normalized,
        };
        tris = new List<int>
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
            1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
            4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1,
        };

        for (int s = 0; s < subdivisions; s++)
        {
            var midCache = new Dictionary<long, int>();
            var newTris = new List<int>(tris.Count * 4);
            for (int i = 0; i < tris.Count; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                int ab = Midpoint(verts, midCache, a, b);
                int bc = Midpoint(verts, midCache, b, c);
                int ca = Midpoint(verts, midCache, c, a);
                newTris.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            tris = newTris;
        }
    }

    static int Midpoint(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
    {
        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        if (cache.TryGetValue(key, out int idx)) return idx;
        verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
        cache[key] = verts.Count - 1;
        return verts.Count - 1;
    }

    // Deterministic lattice value noise (independent of UnityEngine.Random).
    static float Hash(int x, int y, int z, int seed)
    {
        unchecked
        {
            int h = seed;
            h = h * 374761393 + x * 668265263;
            h = h * 374761393 + y * unchecked((int)2246822519);
            h = h * 374761393 + z * unchecked((int)3266489917);
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    static float ValueNoise(Vector3 p, int seed)
    {
        int x0 = Mathf.FloorToInt(p.x), y0 = Mathf.FloorToInt(p.y), z0 = Mathf.FloorToInt(p.z);
        float fx = SmoothStep(p.x - x0), fy = SmoothStep(p.y - y0), fz = SmoothStep(p.z - z0);

        float v000 = Hash(x0, y0, z0, seed),     v100 = Hash(x0 + 1, y0, z0, seed);
        float v010 = Hash(x0, y0 + 1, z0, seed), v110 = Hash(x0 + 1, y0 + 1, z0, seed);
        float v001 = Hash(x0, y0, z0 + 1, seed), v101 = Hash(x0 + 1, y0, z0 + 1, seed);
        float v011 = Hash(x0, y0 + 1, z0 + 1, seed), v111 = Hash(x0 + 1, y0 + 1, z0 + 1, seed);

        float x00 = Mathf.Lerp(v000, v100, fx), x10 = Mathf.Lerp(v010, v110, fx);
        float x01 = Mathf.Lerp(v001, v101, fx), x11 = Mathf.Lerp(v011, v111, fx);
        return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
    }

    // Signed fBm in roughly [-1, 1].
    static float Fbm(Vector3 p, int octaves, int seed)
    {
        float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            sum += (ValueNoise(p * freq, seed + i * 131) * 2f - 1f) * amp;
            norm += amp;
            amp *= 0.5f;
            freq *= 2.1f;
        }
        return sum / norm;
    }

    static float SmoothStep(float t) => t * t * (3f - 2f * t);
    static float Lerp(System.Random rng, float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());

    static float RidgedFbm(Vector3 p, int octaves, int seed)
    {
        float sum = 0f, amp = 1f, freq = 1f, norm = 0f;
        for (int i = 0; i < octaves; i++)
        {
            float n = ValueNoise(p * freq, seed + i * 131) * 2f - 1f;
            sum += (1f - Mathf.Abs(n)) * amp;
            norm += amp;
            amp *= 0.55f;
            freq *= 2.3f;
        }
        return sum / norm;
    }

    // Turns smooth procedural blobs into something that reads as rock.
    //
    // The old prop meshes were bodies of revolution with a little low-frequency
    // noise on top — the eye recognises those instantly as "a sphere someone
    // wobbled", which is exactly the artificial look. Real rock has three things
    // none of that had:
    //
    //   1. Fracture faces  – rock splits along planes, so it is covered in flat
    //                        facets meeting at hard edges, not smooth curvature.
    //   2. Bedding strata  – sedimentary layers of different hardness: resistant
    //                        bands jut out, soft bands erode back, and the underside
    //                        of each hard band overhangs the one below it.
    //   3. Detail at two scales – broad mass shape plus fine chipped surface.
    //
    // RockStyle bundles those parameters, Weather() applies them to any point on any
    // shape (blob, swept cliff, cave shell), and every rock-like feature in
    // ProceduralMeshLibrary goes through it so the whole biome looks like one rock
    // type instead of a bag of unrelated primitives.
    public class RockStyle
    {
        // Ellipsoid the shape is stretched onto. Non-uniform axes alone kill a lot
        // of the "ball" reading before any noise is applied.
        public Vector3 axes = Vector3.one;

        [Tooltip("Broad mass lumps: low frequency, high amplitude.")]
        public float broadFreq = 1.9f;
        public float broadAmp = 0.22f;

        [Tooltip("Chipped surface: high frequency, low amplitude.")]
        public float detailFreq = 7.5f;
        public float detailAmp = 0.05f;

        [Tooltip("Ridged creases that run across the mass like fracture lines.")]
        public float creaseFreq = 3.4f;
        public float creaseAmp = 0.07f;

        [Header("Bedding")]
        [Tooltip("Number of sedimentary layers across one unit of height.")]
        public float strataCount = 6f;
        public float strataAmp = 0.06f;
        [Tooltip("Bedding plane normal — tilted slightly so layers aren't dead level.")]
        public Vector3 beddingUp = Vector3.up;

        [Header("Fracture")]
        [Tooltip("Half-spaces the shape is clipped against; each one becomes a flat face.")]
        public Vector4[] fractures = new Vector4[0]; // xyz = plane normal, w = distance

        public int seed;

        // Builds a coherent rock style. `flatness` biases toward slabby, heavily
        // bedded rock (cliffs) vs chunky blocks (boulders).
        public static RockStyle Random(System.Random rng, int seed, float flatness = 0f)
        {
            var s = new RockStyle { seed = seed };

            float wide = Lerp(rng, 0.85f, 1.35f);
            float tall = Mathf.Lerp(Lerp(rng, 0.7f, 1.15f), Lerp(rng, 0.45f, 0.7f), flatness);
            s.axes = new Vector3(wide, tall, Lerp(rng, 0.8f, 1.3f) * wide);

            // These amplitudes were tuned against offline renders of the generated
            // meshes: anything weaker and the fracture faces and bedding stop reading
            // at all, and the rock falls straight back to looking like a wobbled ball.
            s.broadFreq = Lerp(rng, 1.0f, 1.7f);
            s.broadAmp = Lerp(rng, 0.26f, 0.40f);
            s.detailFreq = Lerp(rng, 5f, 8f);
            s.detailAmp = Lerp(rng, 0.06f, 0.11f);
            s.creaseFreq = Lerp(rng, 2.0f, 3.4f);
            s.creaseAmp = Lerp(rng, 0.10f, 0.17f);

            s.strataCount = Lerp(rng, 5f, 10f) * Mathf.Lerp(1f, 1.6f, flatness);
            s.strataAmp = Lerp(rng, 0.09f, 0.16f) * Mathf.Lerp(1f, 1.7f, flatness);

            // A few degrees of dip: dead-level layers look like a stack of pancakes.
            float dip = Lerp(rng, 0.03f, 0.16f);
            float dipYaw = Lerp(rng, 0f, Mathf.PI * 2f);
            s.beddingUp = new Vector3(Mathf.Cos(dipYaw) * dip, 1f, Mathf.Sin(dipYaw) * dip).normalized;

            s.fractures = BuildFractures(rng, s.axes, 6 + rng.Next(5), 0.52f, 0.86f);
            return s;
        }

        // Random half-spaces sized against the ellipsoid so each one shaves a real
        // slice off rather than missing the shape or cutting it in half.
        public static Vector4[] BuildFractures(System.Random rng, Vector3 axes, int count,
                                               float minKeep, float maxKeep)
        {
            var planes = new Vector4[Mathf.Max(0, count)];
            for (int i = 0; i < planes.Length; i++)
            {
                // Bias slightly away from straight down so bases stay seatable.
                Vector3 n = new Vector3(
                    (float)rng.NextDouble() * 2f - 1f,
                    (float)rng.NextDouble() * 2f - 0.6f,
                    (float)rng.NextDouble() * 2f - 1f);
                if (n.sqrMagnitude < 1e-5f) n = Vector3.right;
                n.Normalize();

                // Extent of the ellipsoid along n.
                float extent = new Vector3(axes.x * n.x, axes.y * n.y, axes.z * n.z).magnitude;
                planes[i] = new Vector4(n.x, n.y, n.z, extent * Lerp(rng, minKeep, maxKeep));
            }
            return planes;
        }

        // Full treatment for a point given as a direction on the unit sphere:
        // stretch onto the ellipsoid, displace, bed, then fracture.
        public Vector3 ShapeFromDirection(Vector3 dir)
        {
            Vector3 p = new Vector3(dir.x * axes.x, dir.y * axes.y, dir.z * axes.z);

            float r = 1f
                    + broadAmp * Fbm(dir * broadFreq, 3, seed)
                    + detailAmp * Fbm(dir * detailFreq, 2, seed + 401)
                    + creaseAmp * (RidgedFbm(dir * creaseFreq, 3, seed + 907) - 0.55f);

            p *= Mathf.Max(0.25f, r);
            p += StrataPush(p, dir);
            return Fracture(p);
        }

        // Treatment for a point on an arbitrary surface (swept cliff faces, cave
        // shells): displace along the outward direction `outward`, bed by world
        // height, then fracture relative to `pivot`.
        public Vector3 Weather(Vector3 p, Vector3 outward, Vector3 pivot, float amount = 1f)
        {
            if (outward.sqrMagnitude < 1e-6f) outward = Vector3.up;
            outward.Normalize();

            float d = broadAmp * Fbm(p * broadFreq, 3, seed)
                    + detailAmp * Fbm(p * detailFreq, 2, seed + 401)
                    + creaseAmp * (RidgedFbm(p * creaseFreq, 3, seed + 907) - 0.55f);

            Vector3 q = p + outward * (d * amount);
            q += StrataPush(q, outward) * amount;

            Vector3 local = q - pivot;
            return pivot + Fracture(local);
        }

        // Alternating hard/soft sedimentary bands. Within a band the profile leans
        // toward the bottom edge, so hard layers overhang the soft ones beneath —
        // the little shadow line that makes stratified rock read at a glance.
        public Vector3 StrataPush(Vector3 p, Vector3 outward)
        {
            if (strataAmp <= 0f || strataCount <= 0f) return Vector3.zero;

            float h = Vector3.Dot(p, beddingUp) * strataCount;
            float band = Mathf.Floor(h);
            float f = h - band;

            float hardness = Hash(Mathf.RoundToInt(band), 17, 3, seed) * 2f - 1f;
            float lip = 0.35f + 0.65f * (1f - f); // juts most at the band's base
            Vector3 lateral = new Vector3(outward.x, 0f, outward.z);
            if (lateral.sqrMagnitude < 1e-6f) lateral = outward;
            return lateral.normalized * (strataAmp * hardness * lip);
        }

        // Clips the point into every fracture half-space. Points outside a plane are
        // projected onto it, so whole patches of the shape collapse into a single
        // flat facet with a hard silhouette edge where the plane crosses the surface.
        public Vector3 Fracture(Vector3 p)
        {
            if (fractures == null) return p;
            for (int i = 0; i < fractures.Length; i++)
            {
                Vector3 n = new Vector3(fractures[i].x, fractures[i].y, fractures[i].z);
                float over = Vector3.Dot(p, n) - fractures[i].w;
                if (over > 0f) p -= n * over;
            }
            return p;
        }

        // Flattens whatever sits below `y` so a rock seats on the seabed instead of
        // balancing on a point, and adds a small outward flare (buried talus).
        public static Vector3 SeatBase(Vector3 p, float y, float flare)
        {
            if (p.y >= y) return p;
            float under = y - p.y;
            p.y = y - under * 0.15f;
            Vector3 lateral = new Vector3(p.x, 0f, p.z);
            if (lateral.sqrMagnitude > 1e-6f)
                p += lateral.normalized * (flare * under);
            return p;
        }
    }
}
