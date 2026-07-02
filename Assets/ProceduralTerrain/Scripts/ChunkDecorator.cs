using UnityEngine;

// Base class for anything that decorates streamed terrain chunks with extra
// objects (prefab scatter, procedural rock formations, kelp, ...). EndlessTerrain
// calls PopulateChunk when the viewer comes near a chunk and destroys the
// returned root once the viewer leaves; implementations must be deterministic
// per chunk so the world rebuilds identically on return.
public abstract class ChunkDecorator : MonoBehaviour
{
    [Tooltip("Props spawn on chunks within this world distance of the viewer. " +
             "Keep ≤ the LOD0 distance so a collider exists to seat on.")]
    public float placementDistance = 16f;

    [Tooltip("Extra distance beyond placementDistance before a chunk's props are destroyed.")]
    public float placementHysteresis = 4f;

    // Builds this decorator's objects for one chunk. Returns the root GameObject
    // holding everything spawned (parented under `parent`), or null if the chunk
    // stays bare. `surface` is the chunk's LOD0 collider for ground raycasts.
    public abstract GameObject PopulateChunk(int chunkX, int chunkZ, Vector3 worldCentre,
                                             float worldHalfSize, Collider surface, Transform parent);
}
