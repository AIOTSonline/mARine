using UnityEngine;

// Ties the lifetime of a runtime-built Mesh to the GameObject that renders it.
//
// Unity does not own meshes through MeshFilter.sharedMesh: destroying the object
// leaves the Mesh sitting in memory. That is harmless for the shared feature
// meshes (built once, reused forever) but not for the per-chunk merged meshes —
// talus and contact shadows are rebuilt for every chunk that streams in, so
// without this each chunk that comes and goes leaks its merged mesh for the rest
// of the session.
[DisallowMultipleComponent]
public class GeneratedMeshOwner : MonoBehaviour
{
    [SerializeField] Mesh _mesh;

    public static void Attach(GameObject go, Mesh mesh)
    {
        if (go == null || mesh == null) return;
        go.AddComponent<GeneratedMeshOwner>()._mesh = mesh;
    }

    void OnDestroy()
    {
        if (_mesh == null) return;
        if (Application.isPlaying) Destroy(_mesh);
        else DestroyImmediate(_mesh);
        _mesh = null;
    }
}
