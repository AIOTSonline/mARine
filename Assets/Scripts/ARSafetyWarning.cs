using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ARSafetyWarning : MonoBehaviour
{
    // Static flag to track if the warning was already shown this app launch
    private static bool warningShown = false;

    [Header("UI References")]
    public GameObject safetyModal;   // full-screen modal (panel)
    public Button continueButton;    // "I Understand / Continue"
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Header("Content (optional override)")]
    [TextArea(3, 6)]
    public string titleDefault = "Safety Warning — Augmented Reality";
    [TextArea(4, 8)]
    public string bodyDefault =
        "• Parental supervision: This AR experience may be unsuitable for young children without adult supervision.\n\n" +
        "• Be aware of your surroundings: Use caution and watch for real-world hazards (stairs, traffic, obstacles) while using AR.";

    void Start()
    {
        // Set text if TMPro fields present
        if (titleText != null) titleText.text = titleDefault;
        if (bodyText != null) bodyText.text = bodyDefault;

        // If warning was already shown this app session, skip
        if (warningShown)
        {
            if (safetyModal != null) safetyModal.SetActive(false);
            return;
        }

        // Show modal and block interaction
        if (safetyModal != null) safetyModal.SetActive(true);

        // Hook button
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    private void OnContinueClicked()
    {
        // Mark as shown for this app session
        warningShown = true;

        if (safetyModal != null) safetyModal.SetActive(false);
    }
}