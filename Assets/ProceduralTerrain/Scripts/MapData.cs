using UnityEngine;

/// <summary>
/// Runtime map data passed between threads.
/// colourMap has been removed; terrain colouring is now handled entirely
/// by the Material assigned to each TerrainChunk's MeshRenderer.
/// </summary>
public struct MapData
{
    public readonly float[,] HeightMap;

    public MapData(float[,] heightMap)
    {
        HeightMap = heightMap;
    }
}

/// <summary>
/// Defines a named terrain region with a height threshold and representative colour.
/// The colour is only used in editor preview (ColourMap draw mode); it is not baked
/// into a runtime texture.
/// </summary>
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}