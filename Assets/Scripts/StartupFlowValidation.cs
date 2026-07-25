using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartupFlowValidation : MonoBehaviour
{
    private static bool warningShown = false;

    [Header("UI References")]
    public GameObject safetyModal;
    public Button continueButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Header("Content (optional override)")]
    [TextArea(3, 6)]
    public string titleDefault = "Safety Warning";

    [TextArea(4, 8)]
    public string bodyDefault =
        "• Parental supervision: This AR experience may be unsuitable for young children without adult supervision.\n\n" +
        "• Be aware of your surroundings: Use caution and watch for real-world hazards (stairs, traffic, obstacles) while using AR.";

    private bool showingDownloadScreen = false;
    private bool packageInstalled = false;

    private TextMeshProUGUI buttonLabel;

    void Start()
    {
        Debug.Log("StartupFlowValidation: Start()");

        if (continueButton != null)
            buttonLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>();

        if (continueButton == null)
            Debug.LogError("StartupFlowValidation: continueButton is NULL.");

        if (buttonLabel == null)
            Debug.LogError("StartupFlowValidation: Button label (TextMeshProUGUI) is NULL.");

        if (titleText == null)
            Debug.LogError("StartupFlowValidation: titleText is NULL.");

        if (bodyText == null)
            Debug.LogError("StartupFlowValidation: bodyText is NULL.");

        if (titleText != null)
            titleText.text = titleDefault;

        if (bodyText != null)
            bodyText.text = bodyDefault;

        if (warningShown)
        {
            if (safetyModal != null)
                safetyModal.SetActive(false);

            return;
        }

        if (safetyModal != null)
            safetyModal.SetActive(true);

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void OnContinueClicked()
    {
        Debug.Log("StartupFlowValidation: Continue clicked.");

        if (!showingDownloadScreen)
        {
            ShowDownloadPrompt();
            return;
        }

        if (packageInstalled)
        {
            warningShown = true;

            if (safetyModal != null)
                safetyModal.SetActive(false);
        }
    }

    private void ShowDownloadPrompt()
    {
        Debug.Log("StartupFlowValidation: Showing download prompt.");

        showingDownloadScreen = true;

        titleText.text =
            "Essential Resources Required";

        bodyText.text =
            "Marine Biology AR requires essential resources before use.\n\n" +
            "This package contains:\n" +
            "• AR Experiences\n" +
            "• 3D Models\n" +
            "• Learning Content\n\n" +
            "The required resources will be downloaded after you continue.";

        if (buttonLabel != null)
            buttonLabel.text = "Download";

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(StartPackageDownload);
    }

    private async void StartPackageDownload()
    {
        Debug.Log("========== StartPackageDownload ==========");

        Debug.Log($"continueButton = {continueButton}");
        Debug.Log($"buttonLabel = {buttonLabel}");
        Debug.Log($"titleText = {titleText}");
        Debug.Log($"bodyText = {bodyText}");
        Debug.Log($"PackageManager.Instance = {PackageManager.Instance}");

        if (continueButton == null)
        {
            Debug.LogError("Continue Button reference is NULL.");
            return;
        }

        if (buttonLabel == null)
        {
            Debug.LogError("Button label is NULL.");
            return;
        }

        if (titleText == null)
        {
            Debug.LogError("Title Text reference is NULL.");
            return;
        }

        if (bodyText == null)
        {
            Debug.LogError("Body Text reference is NULL.");
            return;
        }

        if (PackageManager.Instance == null)
        {
            Debug.LogError("PackageManager.Instance is NULL.");
            return;
        }

        // FOR TESTING ONLY
        // Remove later
        // Caching.ClearCache();

        buttonLabel.text = "Continue";

        continueButton.interactable = false;

        titleText.text =
            "Downloading Resources";

        bodyText.text =
            "Preparing download...";

        await System.Threading.Tasks.Task.Yield();

        try
        {
            Debug.Log("STEP 1: Initialize PackageManager");

            if (!await PackageManager.Instance.InitializeAsync())
            {
                Debug.LogError("Package Manager initialization failed.");
                return;
            }

            Debug.Log("STEP 2: Fetch Package Metadata");

            PackageMetadata package =
                await PackageManager.Instance.GetPackageMetadataAsync(
                    PackageIds.Essential);

            Debug.Log($"Package Metadata = {package}");

            if (package == null)
            {
                Debug.LogError("Package metadata not found.");
                return;
            }

            Debug.Log($"Package Name = {package.PackageName}");
            Debug.Log($"Catalog URL = {package.CatalogUrl}");
            Debug.Log($"Settings URL = {package.SettingsUrl}");

            string packageName = package.PackageName;

            // Download all assets tagged with the EssentialPackage label.
            string addressableKey = PackageIds.EssentialDownloadKey;

            Debug.Log($"Addressable Key = {addressableKey}");

            Debug.Log("STEP 3: Load Catalog");

            if (!await PackageManager.Instance.LoadCatalogAsync(package))
            {
                Debug.LogError("Failed to load content catalog.");
                return;
            }

            Debug.Log("STEP 4: Get Download Size");

            long downloadSize =
                await PackageManager.Instance.GetDownloadSizeAsync(addressableKey);

            if (downloadSize < 0)
            {
                Debug.LogError("Failed to determine download size.");
                return;
            }

            Debug.Log($"Download Size: {downloadSize} bytes");
            Debug.Log($"Download Size: {downloadSize / 1024f / 1024f:F2} MB");

            bodyText.text =
                $"{packageName}\n\n" +
                $"Download Size: {downloadSize / 1024f / 1024f:F2} MB";

            Debug.Log("STEP 5: Download Dependencies");

            bool downloadSucceeded =
                await PackageManager.Instance.DownloadDependenciesAsync(
                    addressableKey,
                    progress =>
                    {
                        bodyText.text =
                            $"{packageName}\n\n" +
                            $"Downloading... {progress:F0}%";
                    });

            if (!downloadSucceeded)
            {
                throw new System.Exception("Dependency download failed.");
            }

            Debug.Log("STEP 6: Dependencies Downloaded");

            packageInstalled = true;

            titleText.text =
                "Resources Installed";

            bodyText.text =
                $"{packageName}\n\n" +
                "All required resources have been installed successfully.";

            continueButton.interactable = true;

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("========== DOWNLOAD FAILED ==========");
            Debug.LogError(ex);

            titleText.text =
                "Download Failed";

            bodyText.text =
                "Unable to download required resources.\n\nPlease try again.";

            continueButton.interactable = true;

            if (buttonLabel != null)
                buttonLabel.text = "Download";

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(StartPackageDownload);
        }
    }
}