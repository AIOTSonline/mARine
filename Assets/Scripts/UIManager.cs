using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;

public class UIManager : MonoBehaviour
{
    public void LoadARScene()
    {
        Addressables.LoadSceneAsync("AR_Spawn");
    }

    public void LoadFreeExploreEcosystem()
    {
        SceneManager.LoadScene("FreeExplore");
    }

    public void LoadModuleWise()
    {
        SceneManager.LoadScene("ModuleWise");
    }

    public void LoadCustomCreateScene()
    {
        SceneManager.LoadScene("StartScreenScene");
    }

    public void LoadHumanInteractionScene()
    {
        SceneManager.LoadScene("Pose Landmark Detection");
    }
}
