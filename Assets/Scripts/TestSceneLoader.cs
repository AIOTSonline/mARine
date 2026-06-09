using UnityEngine;
using UnityEngine.AddressableAssets;

public class TestSceneLoader : MonoBehaviour
{
    async void Start()
    {
        Debug.Log("[TestSceneLoader] Requesting permissions...");
        await PermissionManager.RequestAllAsync();

        Debug.Log("[TestSceneLoader] Loading AR_Spawn via Addressables...");
        var handle = Addressables.LoadSceneAsync("AR_Spawn");
        await handle.Task;

        Debug.Log("[TestSceneLoader] Scene Loaded!");
    }
}
