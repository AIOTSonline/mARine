using System.Collections.Generic;

// Host-agnostic list of things that should appear on the minimap.
// Our own props (corals/seaweed) self-register via MinimapMarker, so the radar
// needs no colliders and no knowledge of what the objects actually are.
public static class MinimapRegistry
{
    static readonly List<MinimapMarker> _all = new List<MinimapMarker>();
    public static IReadOnlyList<MinimapMarker> All => _all;

    public static void Add(MinimapMarker m)
    {
        if (m != null && !_all.Contains(m)) _all.Add(m);
    }

    public static void Remove(MinimapMarker m) => _all.Remove(m);
}
