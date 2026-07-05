using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreateEnv.UI
{
    // Create/Edit form. Thin: it instantiates styled ROW PREFABS (built by the Editor
    // generator) into a scroll container and binds them to the working profile. No UI
    // construction here — only data binding. Every control's range comes from
    // EnvironmentBounds, so the form can't express an out-of-range value; Save/Play
    // clamp once more (steps + cross-field invariants).
    public class EnvironmentEditorUI : MonoBehaviour
    {
        [Header("Wiring (set by the Editor generator)")]
        public RectTransform formContainer;    // ScrollView content
        public GameObject sliderRowPrefab;     // children: Label(TMP), Slider, Value(TMP)
        public GameObject dropdownRowPrefab;   // children: Label(TMP), Dropdown(TMP_Dropdown)
        public GameObject headerRowPrefab;     // child:   Label(TMP)
        public TMP_InputField nameInput;
        public TMP_Text titleText;
        public Button saveButton, playButton, cancelButton;

        EnvironmentProfile _work;
        Action _onDone;
        string _editingId;

        // Host-supplied routing for "Save & Play". When set (marine app), the saved
        // profile is handed back so the host can drive its own scene flow (session +
        // FreeExploreEndless). When null, falls back to the source app's direct load.
        public Action<EnvironmentProfile> PlayRouter;

        void Awake()
        {
            if (saveButton != null)   saveButton.onClick.AddListener(() => { Save(); Hide(); _onDone?.Invoke(); });
            if (playButton != null)   playButton.onClick.AddListener(() => {
                var s = Save();
                if (PlayRouter != null) { PlayRouter(s); return; }
                EnvironmentSession.Selected = s.Clone();
                SceneManager.LoadScene(ExploreScene()); });
            if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
        }

        public void Open(EnvironmentProfile existing, Action onDone)
        {
            _onDone   = onDone;
            _editingId = (existing != null && !existing.isBuiltIn) ? existing.id : null;
            _work     = existing != null ? existing.Clone() : EnvironmentBounds.Default();
            if (existing == null) _work.displayName = "My Environment";
            EnvironmentBounds.Clamp(_work);

            gameObject.SetActive(true);
            if (titleText != null) titleText.text = _editingId != null ? "Edit Environment" : "Create Environment";
            if (nameInput != null)
            {
                nameInput.SetTextWithoutNotify(_work.displayName);
                nameInput.onValueChanged.RemoveAllListeners();
                nameInput.onValueChanged.AddListener(s => _work.displayName = s);
            }
            BuildForm();
        }

        void Hide() => gameObject.SetActive(false);

        void BuildForm()
        {
            for (int i = formContainer.childCount - 1; i >= 0; i--)
                Destroy(formContainer.GetChild(i).gameObject);

            Header("Terrain shape");
            IntSlider("Detail (octaves)", EnvironmentBounds.Octaves, _work.octaves, v => _work.octaves = v);
            FSlider("Feature size", EnvironmentBounds.NoiseScale, _work.noiseScale, v => _work.noiseScale = v);
            FSlider("Roughness", EnvironmentBounds.Persistance, _work.persistance, v => _work.persistance = v);
            FSlider("Frequency growth", EnvironmentBounds.Lacunarity, _work.lacunarity, v => _work.lacunarity = v);
            FSlider("Height (relief)", EnvironmentBounds.MeshHeightMultiplier, _work.meshHeightMultiplier, v => _work.meshHeightMultiplier = v);
            IntSlider("Seed", new EnvironmentBounds.Range(0, 9999, 0, 1), _work.seed, v => _work.seed = v);

            Header("Biome style");
            Dropdown("Style", BiomeStyles.Names, _work.styleIndex, v => { _work.styleIndex = v; BuildForm(); });
            if (_work.styleIndex == 1)
            {
                FSlider("Warp strength", EnvironmentBounds.WarpStrength, _work.warpStrength, v => _work.warpStrength = v);
                FSlider("Warp scale", EnvironmentBounds.WarpScale, _work.warpScale, v => _work.warpScale = v);
                FSlider("Ridge weight", EnvironmentBounds.RidgeWeight, _work.ridgeWeight, v => _work.ridgeWeight = v);
                FSlider("Ridge sharpness", EnvironmentBounds.RidgeSharpness, _work.ridgeSharpness, v => _work.ridgeSharpness = v);
                IntSlider("Terrace steps", EnvironmentBounds.TerraceSteps, _work.terraceSteps, v => _work.terraceSteps = v);
                FSlider("Terrace sharpness", EnvironmentBounds.TerraceSharpness, _work.terraceSharpness, v => _work.terraceSharpness = v);
                FSlider("Terrace strength", EnvironmentBounds.TerraceStrength, _work.terraceStrength, v => _work.terraceStrength = v);
            }

            Header("Water & atmosphere");
            FSlider("Water level", EnvironmentBounds.WaterLevel, _work.waterLevel, v => _work.waterLevel = v);
            FSlider("Fog density", EnvironmentBounds.FogDensity, _work.fogDensity, v => _work.fogDensity = v);
            FSlider("Fog fade start", EnvironmentBounds.FadeStart, _work.fadeStart, v => _work.fadeStart = v);
            FSlider("Fog fade end", EnvironmentBounds.FadeEnd, _work.fadeEnd, v => _work.fadeEnd = v);
            FSlider("Sun glow", EnvironmentBounds.SunGlowIntensity, _work.sunGlowIntensity, v => _work.sunGlowIntensity = v);
            FSlider("Ambient light", EnvironmentBounds.AmbientIntensity, _work.ambientIntensity, v => _work.ambientIntensity = v);
            IntSlider("Water detail", EnvironmentBounds.WaterResolution, _work.waterResolution, v => _work.waterResolution = v);
            ColorRows("Water colour", () => _work.waterColor, c => _work.waterColor = c);
            ColorRows("Surface glow", () => _work.surfaceGlowColor, c => _work.surfaceGlowColor = c);
            ColorRows("Deep colour", () => _work.deepColor, c => _work.deepColor = c);

            Header("Life");
            var lib = LifePackLibrary.Load();
            string[] packs = lib != null ? lib.PackNames() : new[] { "None" };
            Dropdown("Life pack", packs, Mathf.Clamp(_work.lifePackIndex, 0, packs.Length - 1),
                     v => { _work.lifePackIndex = v; BuildForm(); });
            FSlider("Overall density", EnvironmentBounds.LifeDensity, _work.lifeDensity, v => _work.lifeDensity = v);
            IntSlider("Max props / chunk", EnvironmentBounds.MaxPropsPerChunk, _work.maxPropsPerChunk, v => _work.maxPropsPerChunk = v);
            FSlider("Water tint on props", EnvironmentBounds.WaterTint, _work.waterTint, v => _work.waterTint = v);
            SpeciesRows(lib);

            Header("Streaming");
            Dropdown("View distance", new[] { "Near", "Medium", "Far" }, _work.viewDistanceIndex,
                     v => _work.viewDistanceIndex = v);
        }

        void SpeciesRows(LifePackLibrary lib)
        {
            var pack = lib != null ? lib.GetPack(_work.lifePackIndex) : null;
            if (pack == null || pack.rules == null || pack.rules.Length == 0) return;

            var arr = new float[pack.rules.Length];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = (_work.speciesDensity != null && i < _work.speciesDensity.Length) ? _work.speciesDensity[i] : 1f;
            _work.speciesDensity = arr;

            for (int i = 0; i < pack.rules.Length; i++)
            {
                int idx = i;
                string nm = pack.rules[i] != null && !string.IsNullOrEmpty(pack.rules[i].name)
                            ? pack.rules[i].name : $"Species {i + 1}";
                FSliderRaw("• " + nm, EnvironmentBounds.SpeciesDensity.Min, EnvironmentBounds.SpeciesDensity.Max,
                           arr[i], false, v => _work.speciesDensity[idx] = v);
            }
        }

        EnvironmentProfile Save()
        {
            if (_editingId != null) _work.id = _editingId;
            EnvironmentBounds.Clamp(_work);
            return EnvironmentRepository.Save(_work);
        }

        // ── row builders (instantiate the styled prefabs, then bind) ─────────
        void Header(string text)
        {
            var row = Instantiate(headerRowPrefab, formContainer);
            Set(row, "Label", text);
        }

        void FSlider(string cap, EnvironmentBounds.Range r, float value, Action<float> set)
            => FSliderRaw(cap, r.Min, r.Max, r.Clamp(value), false, set);

        void IntSlider(string cap, EnvironmentBounds.Range r, int value, Action<int> set)
            => FSliderRaw(cap, r.Min, r.Max, r.ClampInt(value), true, v => set(Mathf.RoundToInt(v)));

        void FSliderRaw(string cap, float min, float max, float value, bool integer, Action<float> set)
        {
            var row = Instantiate(sliderRowPrefab, formContainer);
            Set(row, "Label", cap);
            var valueText = Find<TMP_Text>(row, "Value");
            var slider = Find<Slider>(row, "Slider");
            if (slider == null) return;
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = integer;
            slider.SetValueWithoutNotify(value);
            if (valueText != null) valueText.text = Fmt(value, integer);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v => { if (valueText != null) valueText.text = Fmt(v, integer); set(v); });
        }

        void Dropdown(string cap, string[] options, int value, Action<int> onChange)
        {
            var row = Instantiate(dropdownRowPrefab, formContainer);
            Set(row, "Label", cap);
            var dd = Find<TMP_Dropdown>(row, "Dropdown");
            if (dd == null) return;
            dd.ClearOptions();
            dd.AddOptions(new List<string>(options));
            dd.SetValueWithoutNotify(Mathf.Clamp(value, 0, options.Length - 1));
            dd.onValueChanged.RemoveAllListeners();
            dd.onValueChanged.AddListener(v => onChange(v));
        }

        void ColorRows(string caption, Func<Color> get, Action<Color> set)
        {
            Header(caption);
            var c = get();
            FSliderRaw("R", 0, 1, c.r, false, v => { var x = get(); x.r = v; set(x); });
            FSliderRaw("G", 0, 1, c.g, false, v => { var x = get(); x.g = v; set(x); });
            FSliderRaw("B", 0, 1, c.b, false, v => { var x = get(); x.b = v; set(x); });
        }

        static string Fmt(float v, bool integer) => integer ? Mathf.RoundToInt(v).ToString() : v.ToString("0.###");

        static void Set(GameObject root, string child, string value)
        {
            var t = Find<TMP_Text>(root, child);
            if (t != null) t.text = value;
        }

        static T Find<T>(GameObject root, string child) where T : Component
        {
            var t = root.transform.Find(child);
            return t != null ? t.GetComponent<T>() : null;
        }

        static string ExploreScene()
        {
            var s = FindFirstObjectByType<StartScreenUI>();
            return s != null ? s.endlessSceneName : "FreeExploreEndless";
        }
    }
}
