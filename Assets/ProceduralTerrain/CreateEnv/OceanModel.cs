using UnityEngine;

namespace CreateEnv
{
    // What the ocean actually does to light, so environments can be points in a
    // physical space instead of a list of hand-picked art presets.
    //
    // Everything visual about being underwater — how far you see, what colour the
    // water is, how fast red drains out of things, how much light reaches the
    // seabed, what grows on the rock — falls out of two numbers: how turbid the
    // water is, and how deep you are. This class holds that model; the mapper
    // reads it and fills in the technical EnvironmentProfile.
    //
    // ── Water type ───────────────────────────────────────────────────────────
    // Turbidity is expressed as the Jerlov optical water type, the standard
    // oceanographic classification: I / IA / IB / II / III for open ocean
    // (I clearest), and 1C..9C for coastal water (1C clearest, 9C most turbid).
    // The classification is defined on the downwelling diffuse attenuation
    // coefficient Kd — how fast light of a given wavelength dies with depth.
    //
    // Published Kd(490 nm) anchors this table: IA 0.035-0.040, IB 0.042-0.065,
    // II 0.07-0.10, III 0.115-0.14 m^-1, with measured coastal water running
    // ~0.15-0.35 m^-1 and dirtier inshore water well above that.
    //
    // The important structure is the *crossover*, and it is why this is worth
    // modelling rather than art-directing. In clear open ocean the minimum Kd sits
    // in the blue, so blue survives longest and the water reads blue. Toward
    // coastal water, CDOM and phytoplankton absorb blue hard, the minimum shifts
    // to the green, and the water reads green. "Tropical blue" and "coastal green"
    // are not two palettes — they are the same physics at two turbidities, and in
    // this table the hue swap emerges on its own between III and 1C.
    public static class OceanModel
    {
        public static readonly string[] WaterTypeNames =
        {
            "I  — clearest open ocean",
            "IA — clear open ocean",
            "IB — open ocean",
            "II — open ocean, slight bloom",
            "III — turbid open ocean",
            "1C — clearest coastal",
            "3C — coastal",
            "5C — turbid coastal",
            "7C — very turbid coastal",
            "9C — muddy inshore",
        };

        // Diffuse attenuation coefficient Kd, 1/m, sampled at representative
        // red (~650 nm), green (~550 nm) and blue (~450 nm) wavelengths.
        // Red is high everywhere: pure water itself absorbs red strongly, which is
        // why red drains out of everything within the first few metres regardless
        // of how clean the water is.
        static readonly Vector3[] Kd =
        {
            new Vector3(0.35f, 0.063f, 0.019f), // I
            new Vector3(0.36f, 0.075f, 0.038f), // IA
            new Vector3(0.37f, 0.089f, 0.058f), // IB
            new Vector3(0.40f, 0.115f, 0.098f), // II
            new Vector3(0.44f, 0.155f, 0.155f), // III   <- blue/green cross over here
            new Vector3(0.47f, 0.185f, 0.225f), // 1C
            new Vector3(0.53f, 0.245f, 0.345f), // 3C
            new Vector3(0.60f, 0.320f, 0.480f), // 5C
            new Vector3(0.70f, 0.430f, 0.660f), // 7C
            new Vector3(0.85f, 0.580f, 0.890f), // 9C
        };

        public static int TypeCount => Kd.Length;

        public static Vector3 Attenuation(int waterType)
            => Kd[Mathf.Clamp(waterType, 0, Kd.Length - 1)];

        // Fraction of each channel surviving `metres` of water.
        public static Vector3 Transmission(int waterType, float metres)
        {
            Vector3 k = Attenuation(waterType);
            return new Vector3(Mathf.Exp(-k.x * metres),
                               Mathf.Exp(-k.y * metres),
                               Mathf.Exp(-k.z * metres));
        }

        // The colour the water takes on: whatever light is left after a path
        // length, normalised so the surviving channel is full. This is where the
        // blue-to-green shift comes from — nothing selects it, it is just which
        // channel has the smallest Kd.
        public static Color WaterColour(int waterType, float metres, float valueScale = 1f)
        {
            Vector3 t = Transmission(waterType, metres);
            float peak = Mathf.Max(t.x, Mathf.Max(t.y, t.z));
            if (peak < 1e-5f) return Color.black;
            return new Color(t.x / peak * valueScale,
                             t.y / peak * valueScale,
                             t.z / peak * valueScale, 1f);
        }

        // Visual range in metres, via the Secchi relation z ~ 1.44 / Kd.
        //
        // Keyed off the *surviving* channel, not the average: you see by whatever
        // light is left, so in clear ocean that is the blue and in coastal water
        // the green. Averaging instead lets the huge red coefficient — which is
        // high in every water type because water itself eats red — drag clear
        // ocean down to a dozen metres, which is wrong by a factor of five.
        //
        // Approximate, and flagged as such, but it makes clarity mean metres
        // rather than an unitless slider: II lands near 15 m, coastal 3C near 6 m,
        // muddy inshore near 2.5 m, which is what those waters actually dive like.
        public static float VisibilityMetres(int waterType)
        {
            Vector3 k = Attenuation(waterType);
            float best = Mathf.Min(k.x, Mathf.Min(k.y, k.z));
            return 1.44f / Mathf.Max(best, 1e-4f);
        }

        // The shader's medium term is exp(-(distance * density)^2), so pick the
        // density that leaves ~2% contrast at the visual range: (V*d)^2 ~ 3.9.
        public static float FogDensityFor(int waterType)
            => 1.98f / Mathf.Max(VisibilityMetres(waterType), 0.01f);

        // How much harder the weak channels are absorbed than the strong one.
        // UnderwaterCommon scales per-channel absorption by (1 + absorbTint *
        // (1 - hue)), so matching the real Kd spread means feeding it the ratio
        // between the fastest- and slowest-dying channels.
        public static float AbsorbTintFor(int waterType)
        {
            Vector3 k = Attenuation(waterType);
            float lo = Mathf.Min(k.x, Mathf.Min(k.y, k.z));
            float hi = Mathf.Max(k.x, Mathf.Max(k.y, k.z));
            return Mathf.Clamp(hi / Mathf.Max(lo, 1e-4f) - 1f, 0f, 4f);
        }

        // Fraction of surface daylight reaching a given depth, averaged over the
        // visible band. This is what actually decides whether the seabed is a lit
        // algal habitat or a dim filter-feeder one.
        public static float LightAtDepth(int waterType, float depthMetres)
        {
            Vector3 t = Transmission(waterType, Mathf.Max(depthMetres, 0f));
            return Mathf.Clamp01((t.x + t.y + t.z) / 3f);
        }

        // ── Benthic community ────────────────────────────────────────────────
        // Encrusting cover on hard substrate is near-total in the photic zone:
        // corallines, bryozoans and tubeworms take bare rock within 1-4 months,
        // so "bare grey rock" is not a state submerged stone stays in.
        //
        // What depth changes is *who*, not *how much*. Shallow lit rock is algal
        // turf and coralline; as light runs out the algae drop away and sponges,
        // ascidians and bryozoans take over — still fully covered, just a
        // different palette. So coverage stays high with depth and only the
        // colours move.
        public struct Benthos
        {
            public float coverage;   // fraction of rock surface encrusted
            public Color colorA;     // dominant lit community
            public Color colorB;     // secondary lit community
            // The cryptic community — sponges, ascidians, bryozoans — that lives on
            // undersides, overhangs and crevice walls. It is a separate axis from
            // depth: even on a sunlit reef flat the shaded face of a boulder carries
            // filter feeders, not algae. The shader selects it by face orientation,
            // so it is not simply "what colorA fades to as you go deeper".
            public Color colorC;
            // How much physical thickness the crust reads as. Coral turf is knobbly;
            // a sediment-dulled film on sand is nearly flat.
            public float relief;

            // The continuous filamentous algal mat that underlies the colonies —
            // the fuzzy green coat on a shallow boulder. Unlike `coverage`, which
            // stays high at any depth because filter feeders take over from algae,
            // this is strictly light-limited: it is dense on a sunlit reef flat and
            // simply absent below the photic zone.
            public float turf;
            public Color turfBase;   // shaded base of the mat
            public Color turfTip;    // sunlit filament tips
        }

        public static Benthos BenthosAt(int waterType, float depthMetres, int habitat,
                                        float lifeDensity01)
        {
            float light = LightAtDepth(waterType, depthMetres);

            // Even in the dark, hard substrate stays colonised — the floor here is
            // filter feeders, not bare rock.
            float cover = Mathf.Lerp(0.55f, 0.95f, Mathf.Clamp01(lifeDensity01));

            // Algal (lit) palette vs filter-feeder (dim) palette per habitat.
            // `relief` is how proud of the rock the crust stands: reef turf and
            // coralline build real thickness, a silted film on a sand plain does not.
            Color litA, litB;
            float relief;
            // Turf is a different palette from the colonies: the mat is green
            // wherever there is light, because it is algae. On a reef it is the
            // duller olive-brown film between the corals, not the corals' own pinks.
            Color turfBase, turfTip;
            float turfDensity;
            switch (habitat)
            {
                case 1: // Kelp forest — olive turf over rust-brown holdfast rock
                    litA = new Color(0.36f, 0.42f, 0.22f);
                    litB = new Color(0.62f, 0.44f, 0.20f);
                    relief = 0.85f;
                    turfBase = new Color(0.11f, 0.24f, 0.10f);
                    turfTip = new Color(0.42f, 0.62f, 0.18f);
                    turfDensity = 1.0f;   // cool nutrient-rich water; the mat is thick
                    break;
                case 2: // Sandy bottom — sparse, sediment-dulled film
                    litA = new Color(0.55f, 0.52f, 0.42f);
                    litB = new Color(0.68f, 0.62f, 0.48f);
                    cover *= 0.6f;
                    relief = 0.30f;
                    turfBase = new Color(0.22f, 0.26f, 0.16f);
                    turfTip = new Color(0.46f, 0.50f, 0.30f);
                    turfDensity = 0.35f;  // little hard substrate to grow on
                    break;
                default: // Coral reef — coralline pink/purple and orange turf
                    litA = new Color(0.62f, 0.28f, 0.42f);
                    litB = new Color(0.82f, 0.48f, 0.26f);
                    relief = 1.0f;
                    turfBase = new Color(0.14f, 0.22f, 0.12f);
                    turfTip = new Color(0.44f, 0.56f, 0.24f);
                    turfDensity = 0.75f;  // grazers keep reef turf cropped short
                    break;
            }

            // Sponge / ascidian assemblage: deeper reds and dull violets that keep
            // reading as life rather than as dirty stone once the algae are gone.
            Color dimA = new Color(0.44f, 0.20f, 0.26f);
            Color dimB = new Color(0.30f, 0.30f, 0.38f);

            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(light * 3.2f));

            // The cryptic palette does not brighten with depth the way the lit one
            // does — a shaded underside is a shaded underside at 3 m or 30 m — so
            // colorC only warms slightly rather than tracking `t` all the way.
            Color cryptic = Color.Lerp(dimA, new Color(0.58f, 0.26f, 0.30f), t * 0.45f);

            return new Benthos
            {
                coverage = Mathf.Clamp01(cover),
                colorA = Color.Lerp(dimA, litA, t),
                colorB = Color.Lerp(dimB, litB, t),
                colorC = cryptic,
                // Crust thickness falls off with light along with the algae, but
                // never to nothing: filter feeders are lumpier than they are flat.
                relief = Mathf.Clamp01(relief * Mathf.Lerp(0.55f, 1f, t)),

                // Strictly light-limited, so this goes to zero in the dark rather
                // than handing over to another community the way `coverage` does.
                turf = Mathf.Clamp01(turfDensity * t * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(lifeDensity01))),
                turfBase = turfBase,
                turfTip = turfTip,
            };
        }
    }
}
