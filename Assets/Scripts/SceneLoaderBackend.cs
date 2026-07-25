using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public static class SceneLoaderBackend
{
    public static async Task<bool> LoadLocalSceneAsync(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName, loadMode);

        if (operation == null)
        {
            Debug.LogError($"Failed to load local scene '{sceneName}'.");
            return false;
        }

        while (!operation.isDone)
            await Task.Yield();

        return true;
    }

    public static async Task<bool> LoadAddressableSceneAsync(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(sceneName, loadMode);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load Addressable scene '{sceneName}'.");
            return false;
        }

        return true;
    }
}