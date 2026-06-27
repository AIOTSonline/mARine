using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Firebase;
using Firebase.Firestore;

using UnityEngine.AddressableAssets;

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

    void Start()
    {
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
        showingDownloadScreen = true;

        titleText.text =
            "Essential Resources Required";

        bodyText.alignment = TextAlignmentOptions.Left;

        bodyText.text =
            "Marine Biology AR requires essential resources before use.\n\n" +
            "This package contains:\n" +
            "• AR Experiences\n" +
            "• 3D Models\n" +
            "• Learning Content\n\n" +
            "Essential Resource Pack";

        continueButton.GetComponentInChildren<TextMeshProUGUI>().text =
            "Download";

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(StartPackageDownload);
    }

    private async void StartPackageDownload()
    {
        // FOR TESTING ONLY
        // Remove later
        // Caching.ClearCache();

        continueButton.GetComponentInChildren<TextMeshProUGUI>().text =
            "Continue";

        continueButton.interactable = false;

        titleText.text =
            "Downloading Resources";

        bodyText.text =
            "Essential Resource Pack\n\n" +
            "Preparing download...";

        bodyText.alignment = 
            TextAlignmentOptions.Center;

        await System.Threading.Tasks.Task.Yield();

        try
        {
            Debug.Log("Checking Firebase...");

            var status =
                await FirebaseApp.CheckAndFixDependenciesAsync();

            Debug.Log("Firebase Status: " + status);

            if (status != DependencyStatus.Available)
            {
                Debug.LogError("Firebase dependency issue: " + status);
                return;
            }

            FirebaseFirestore db =
                FirebaseFirestore.DefaultInstance;

            Debug.Log("Fetching package metadata...");

            DocumentSnapshot snap =
                await db.Collection("packages")
                        .Document("essential")
                        .GetSnapshotAsync();

            if (!snap.Exists)
            {
                Debug.LogError("Package document not found");
                return;
            }

            Debug.Log("Package Found!");

            string catalogUrl =
                snap.GetValue<string>("catalogBinUrl");

            string entryScene =
                snap.GetValue<string>("entryScene");

            Debug.Log("Catalog URL: " + catalogUrl);
            Debug.Log("Entry Scene: " + entryScene);

            Debug.Log("Loading catalog...");

            var catalogHandle =
                Addressables.LoadContentCatalogAsync(catalogUrl);

            await catalogHandle.Task;

            Debug.Log("Catalog Loaded");

            var locationsHandle =
                Addressables.LoadResourceLocationsAsync(entryScene);

            await locationsHandle.Task;

            foreach (var loc in locationsHandle.Result)
            {
                Debug.Log("LOCATION = " + loc.InternalId);
            }

            var sizeHandle =
                Addressables.GetDownloadSizeAsync(entryScene);

            await sizeHandle.Task;

            Debug.Log(
                "Download Size: " +
                sizeHandle.Result +
                " bytes");

            bodyText.text =
                "Essential Resource Pack\n\n" +
                $"Download Size: {sizeHandle.Result / 1024f / 1024f:F2} MB";

            Debug.Log("Starting dependency download...");

            var downloadHandle =
                Addressables.DownloadDependenciesAsync(entryScene);

            while (!downloadHandle.IsDone)
            {
                var downloadStatus =
                    downloadHandle.GetDownloadStatus();

                if (downloadStatus.TotalBytes > 0)
                {
                    float progress =
                        (float)downloadStatus.DownloadedBytes /
                        downloadStatus.TotalBytes;

                    Debug.Log(
                        $"Downloaded {downloadStatus.DownloadedBytes / 1024f / 1024f:F2} MB / " +
                        $"{downloadStatus.TotalBytes / 1024f / 1024f:F2} MB " +
                        $"({progress * 100f:F1}%)"
                    );

                    bodyText.text =
                        "Essential Resource Pack\n\n" +
                        $"Downloading... {(progress * 100f):F0}%";
                }
                else
                {
                    Debug.Log("Preparing download...");

                    bodyText.text =
                        "Essential Resource Pack\n\n" +
                        "Preparing download...";
                }

                await System.Threading.Tasks.Task.Delay(500);
            }

            Debug.Log("Dependencies Downloaded");

            packageInstalled = true;

            titleText.text =
                "Resources Installed";

            bodyText.text =
                "Essential Resource Pack\n\n" +
                "All required resources have been installed successfully.";

            continueButton.interactable = true;

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);

            titleText.text =
                "Download Failed";

            bodyText.text =
                "Unable to download required resources.\n\nPlease try again.";

            continueButton.interactable = true;

            continueButton.GetComponentInChildren<TextMeshProUGUI>().text =
                "Download";

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(StartPackageDownload);
        }
    }
}