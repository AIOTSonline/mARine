using UnityEngine;

public class AIWindowToggle : MonoBehaviour
{
    public GameObject aiPanel;

    public void ToggleAI()
    {
        aiPanel.SetActive(!aiPanel.activeSelf);
    }
}