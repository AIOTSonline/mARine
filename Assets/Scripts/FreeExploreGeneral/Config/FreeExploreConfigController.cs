using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FreeExploreConfigController : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private Button boundlessButton;
    [SerializeField] private Button portalButton;

    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color unselectedColor = new Color(0.75f, 0.75f, 0.75f);

    [Header("Environment")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text environmentName;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;

    [SerializeField] private Button playButton;

    [Header("Data")]
    [SerializeField] private EnvironmentInfo[] boundlessEnvironments;
    [SerializeField] private PortalEnvironmentInfo[] portalEnvironments;

    private int boundlessIndex = 0;
    private int portalIndex = 0;

    private bool isBoundless = true;

    private void Start()
    {
        leftArrow.onClick.AddListener(PreviousEnvironment);
        rightArrow.onClick.AddListener(NextEnvironment);

        boundlessButton.onClick.AddListener(SelectBoundless);
        portalButton.onClick.AddListener(SelectPortal);

        playButton.onClick.AddListener(Play);

        RefreshModeUI();
        RefreshUI();
    }

    private void SelectBoundless()
    {
        if (isBoundless)
            return;

        isBoundless = true;

        RefreshModeUI();
        RefreshUI();
    }

    private void SelectPortal()
    {
        if (!isBoundless)
            return;

        isBoundless = false;

        RefreshModeUI();
        RefreshUI();
    }

    private void PreviousEnvironment()
    {
        if (isBoundless)
        {
            if (boundlessEnvironments.Length == 0)
                return;

            boundlessIndex--;

            if (boundlessIndex < 0)
                boundlessIndex = boundlessEnvironments.Length - 1;
        }
        else
        {
            if (portalEnvironments.Length == 0)
                return;

            portalIndex--;

            if (portalIndex < 0)
                portalIndex = portalEnvironments.Length - 1;
        }

        RefreshUI();
    }

    private void NextEnvironment()
    {
        if (isBoundless)
        {
            if (boundlessEnvironments.Length == 0)
                return;

            boundlessIndex++;

            if (boundlessIndex >= boundlessEnvironments.Length)
                boundlessIndex = 0;
        }
        else
        {
            if (portalEnvironments.Length == 0)
                return;

            portalIndex++;

            if (portalIndex >= portalEnvironments.Length)
                portalIndex = 0;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (isBoundless)
        {
            if (boundlessEnvironments.Length == 0)
                return;

            var env = boundlessEnvironments[boundlessIndex];

            previewImage.sprite = env.PreviewImage;
            environmentName.text = env.EnvironmentName;
            descriptionText.text = env.Description;
        }
        else
        {
            if (portalEnvironments.Length == 0)
                return;

            var env = portalEnvironments[portalIndex];

            previewImage.sprite = env.PreviewImage;
            environmentName.text = env.EnvironmentName;
            descriptionText.text = env.Description;
        }
    }

    private void RefreshModeUI()
    {
        bool showArrows = isBoundless
            ? boundlessEnvironments.Length > 1
            : portalEnvironments.Length > 1;

        leftArrow.gameObject.SetActive(showArrows);
        rightArrow.gameObject.SetActive(showArrows);

        boundlessButton.image.color = isBoundless ? selectedColor : unselectedColor;
        portalButton.image.color = isBoundless ? unselectedColor : selectedColor;
    }

    private void Play()
    {
        if (isBoundless)
        {
            ExploreSession.SelectedBoundlessEnvironment =
                boundlessEnvironments[boundlessIndex];

            SceneManager.LoadScene("FreeExploreEndless");
        }
        else
        {
            if (portalEnvironments.Length == 0)
                return;

            SceneManager.LoadScene(portalEnvironments[portalIndex].SceneName);
        }
    }
}