using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CreateEnv.EditorTools
{
    // Generates the small set of vector-ish sprites the Explore UI needs (rounded
    // rects, capsule, circle, star, check, chevron, and a placeholder preview for
    // user-made environments) as PNG assets. Everything is drawn from signed
    // distance functions with anti-aliasing, so the art is deterministic and can be
    // regenerated at any time — no hand-authored textures to lose.
    public static class EnvUiSprites
    {
        public const string Dir = "Assets/ProceduralTerrain/CreateEnv/UI/Art";

        public static Sprite RoundedRect => Load("RoundedRect");
        public static Sprite Capsule     => Load("Capsule");
        public static Sprite Circle      => Load("Circle");
        public static Sprite Star        => Load("Star");
        public static Sprite Check       => Load("Check");
        public static Sprite Chevron     => Load("Chevron");
        public static Sprite CustomPreview => Load("CustomEnvPreview");

        static Sprite Load(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{name}.png");
            if (sprite == null)
                throw new InvalidOperationException(
                    $"Sprite '{name}' missing — EnvUiSprites.GenerateAll() must run first.");
            return sprite;
        }

        public static void GenerateAll()
        {
            Directory.CreateDirectory(Dir);

            // 9-slice shapes are white so Image.color tints them per use.
            WriteShape("RoundedRect", 64, 64, RoundedBoxAlpha(20f), new Vector4(24, 24, 24, 24));
            WriteShape("Capsule",     64, 64, RoundedBoxAlpha(31f), new Vector4(31, 31, 31, 31));
            WriteShape("Circle",      64, 64, CircleAlpha(31f),     Vector4.zero);
            WriteShape("Star",        64, 64, StarAlpha(),          Vector4.zero);
            WriteShape("Check",       64, 64, PolylineAlpha(6.5f, new Vector2(14, 36), new Vector2(26, 22), new Vector2(50, 46)), Vector4.zero);
            WriteShape("Chevron",     64, 64, PolylineAlpha(6.5f, new Vector2(40, 14), new Vector2(24, 32), new Vector2(40, 50)), Vector4.zero);
            WritePreview();

            AssetDatabase.Refresh();
        }

        // ── shape coverage functions (x,y in pixels, y up) ───────────────────
        static Func<float, float, float> RoundedBoxAlpha(float radius) => (x, y) =>
        {
            var p = new Vector2(x - 32f, y - 32f);
            var b = new Vector2(31f, 31f) - Vector2.one * radius;
            var q = new Vector2(Mathf.Max(Mathf.Abs(p.x) - b.x, 0f), Mathf.Max(Mathf.Abs(p.y) - b.y, 0f));
            return Coverage(q.magnitude - radius);
        };

        static Func<float, float, float> CircleAlpha(float radius) => (x, y) =>
            Coverage(new Vector2(x - 32f, y - 32f).magnitude - radius);

        static Func<float, float, float> StarAlpha()
        {
            // 5-point star, point up, as a 10-vertex polygon; even-odd fill with
            // 3x3 supersampling (winding math per subpixel is cheap at 64x64).
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float r = i % 2 == 0 ? 30f : 12.5f;
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                pts[i] = new Vector2(32f + r * Mathf.Cos(a), 32f + r * Mathf.Sin(a));
            }
            return (x, y) =>
            {
                int hits = 0, samples = 0;
                for (int sy = 0; sy < 3; sy++)
                for (int sx = 0; sx < 3; sx++)
                {
                    samples++;
                    if (InPolygon(pts, new Vector2(x - 0.5f + (sx + 0.5f) / 3f, y - 0.5f + (sy + 0.5f) / 3f)))
                        hits++;
                }
                return (float)hits / samples;
            };
        }

        static Func<float, float, float> PolylineAlpha(float thickness, params Vector2[] pts) => (x, y) =>
        {
            var p = new Vector2(x, y);
            float d = float.MaxValue;
            for (int i = 0; i < pts.Length - 1; i++)
                d = Mathf.Min(d, DistanceToSegment(p, pts[i], pts[i + 1]));
            return Coverage(d - thickness * 0.5f);
        };

        static float Coverage(float signedDistance) => Mathf.Clamp01(0.5f - signedDistance);

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + t * ab)).magnitude;
        }

        static bool InPolygon(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
                if (poly[i].y > p.y != poly[j].y > p.y &&
                    p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            return inside;
        }

        // ── writers ──────────────────────────────────────────────────────────
        static void WriteShape(string name, int w, int h, Func<float, float, float> alphaAt, Vector4 border)
        {
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)(255f * alphaAt(x + 0.5f, y + 0.5f)));
            WritePng(name, w, h, pixels, border);
        }

        // Placeholder card image for user-made environments: an underwater gradient
        // with two dune silhouettes — on-theme without pretending to be a photo.
        static void WritePreview()
        {
            const int w = 640, h = 360;
            Color top = new Color32(0x3B, 0xB4, 0xCC, 0xFF), bottom = new Color32(0x06, 0x3A, 0x57, 0xFF);
            Color duneFar = new Color32(0x0A, 0x4A, 0x66, 0xFF), duneNear = new Color32(0x05, 0x2E, 0x45, 0xFF);

            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = Color.Lerp(bottom, top, (float)y / h);
                float far  = 120f + 30f * Mathf.Sin(x * 0.017f) + 18f * Mathf.Sin(x * 0.041f + 2.1f);
                float near =  70f + 26f * Mathf.Sin(x * 0.023f + 4.2f) + 14f * Mathf.Sin(x * 0.056f + 1.3f);
                if (y < far)  c = Color.Lerp(c, duneFar, 0.85f);
                if (y < near) c = duneNear;
                pixels[y * w + x] = c;
            }
            WritePng("CustomEnvPreview", w, h, pixels, Vector4.zero);
        }

        static void WritePng(string name, int w, int h, Color32[] pixels, Vector4 border)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            string path = $"{Dir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
