using UnityEngine;

namespace CreateEnv
{
    // The "locked" life presets. Each pack is a hand-authored set of scatter rules
    // (with real coral/kelp prefabs) that the user chooses by name — they never edit
    // prefab arrays directly, which is what prevents empty or overpacked worlds.
    //
    // Author this once in the Editor and drop it in a Resources folder as
    // "LifePackLibrary" so both the editor UI and the Explore loader can find it.
    // Convention: index 0 should be an empty "None" pack. If the asset is missing,
    // the loader gracefully keeps whatever TerrainDetailScatter is already in the scene.
    [CreateAssetMenu(menuName = "CreateEnv/Life Pack Library", fileName = "LifePackLibrary")]
    public class LifePackLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Pack
        {
            public string name = "None";
            [Tooltip("Scatter rules for this pack. Leave empty for the 'None' pack.")]
            public TerrainDetailScatter.ScatterRule[] rules;
        }

        public Pack[] packs = new Pack[0];

        static LifePackLibrary _cached;
        static bool _tried;

        public static LifePackLibrary Load()
        {
            if (_tried) return _cached;
            _tried = true;
            _cached = Resources.Load<LifePackLibrary>("LifePackLibrary");
            return _cached;
        }

        public string[] PackNames()
        {
            if (packs == null || packs.Length == 0) return new[] { "None" };
            var names = new string[packs.Length];
            for (int i = 0; i < packs.Length; i++)
                names[i] = string.IsNullOrEmpty(packs[i].name) ? $"Pack {i}" : packs[i].name;
            return names;
        }

        public Pack GetPack(int index)
        {
            if (packs == null || index < 0 || index >= packs.Length) return null;
            return packs[index];
        }
    }
}
