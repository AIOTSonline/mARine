using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedActorData
{
    public string prefabName;
    public Vector3 localPosition;
    public Quaternion localRotation;
    public bool isMainPlayer = false;
    public string uniqueID;
    public List<string> addedScripts = new();
    public string foodTargetUniqueID;
    public int layerIndex = 0; // 0 = surface, 1 = mid-water, 2 = sea floor
}