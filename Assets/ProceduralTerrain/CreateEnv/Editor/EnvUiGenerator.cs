using CreateEnv.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.EditorTools
{
    // Rebuilds the Explore configuration UI (FreeExploreConfig + CustomEnvBuilder
    // scenes and the form row prefabs) in the shared design language defined by
    // EnvUiKit. Idempotent: run it again after tweaking EnvUiKit/EnvUiSprites and
    // both screens are regenerated and rewired from scratch.
    public static class EnvUiGenerator
    {
        const string ConfigScenePath  = "Assets/Scenes/FreeExploreConfig.unity";
        const string BuilderScenePath = "Assets/Scenes/CustomEnvBuilder.unity";
        const string UiDir            = "Assets/ProceduralTerrain/CreateEnv/UI";

        [MenuItem("Tools/Marine AR/Rebuild Explore UI (both screens)")]
        public static void RebuildAll()
        {
            EnvUiSprites.GenerateAll();
            RebuildRowPrefabs();
            RebuildConfigScreen();
            RebuildBuilderScreen();
            Debug.Log("[EnvUiGenerator] Explore UI rebuilt: config screen, builder screen, row prefabs.");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  FreeExploreConfig — mode toggle, environment card carousel, actions
        // ══════════════════════════════════════════════════════════════════════
        static void RebuildConfigScreen()
        {
            var scene = EditorSceneManager.OpenScene(ConfigScenePath, OpenSceneMode.Single);
            var controller = Object.FindFirstObjectByType<FreeExploreConfigController>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (controller == null || canvas == null)
            {
                Debug.LogError("[EnvUiGenerator] FreeExploreConfig scene needs a FreeExploreConfigController and a Canvas.");
                return;
            }

            EnvUiKit.ConfigureCanvas(canvas);
            EnvUiKit.ClearChildren(canvas.transform);

            var screen = EnvUiKit.Panel(canvas.transform, "Screen", null, EnvUiKit.Background);
            screen.raycastTarget = false;
            EnvUiKit.Stretch(EnvUiKit.Rt(screen));

            var title = EnvUiKit.Text(screen.transform, "Title", "Configure your exploration",
                                      52, EnvUiKit.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            EnvUiKit.Place(EnvUiKit.Rt(title), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(980f, 80f));

            // ── mode selector pill ────────────────────────────────────────────
            var mode = EnvUiKit.Panel(screen.transform, "ModeSelector", EnvUiSprites.Capsule, EnvUiKit.Surface);
            EnvUiKit.Place(EnvUiKit.Rt(mode), new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(640f, 110f));
            EnvUiKit.DropShadow(mode);

            var boundless = EnvUiKit.SolidButton(mode.transform, "BoundlessButton", "Boundless",
                                                 EnvUiKit.Primary, Color.white, 32, EnvUiSprites.Capsule);
            EnvUiKit.Place(EnvUiKit.Rt(boundless), new Vector2(0.5f, 0.5f), new Vector2(-155f, 0f), new Vector2(295f, 86f));
            var portal = EnvUiKit.SolidButton(mode.transform, "PortalButton", "Portal",
                                              EnvUiKit.Surface, EnvUiKit.TextSecondary, 32, EnvUiSprites.Capsule);
            EnvUiKit.Place(EnvUiKit.Rt(portal), new Vector2(0.5f, 0.5f), new Vector2(155f, 0f), new Vector2(295f, 86f));

            // ── environment card ──────────────────────────────────────────────
            var card = EnvUiKit.Panel(screen.transform, "EnvironmentCard", EnvUiSprites.RoundedRect, EnvUiKit.Surface);
            EnvUiKit.Place(EnvUiKit.Rt(card), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(920f, 1180f));
            EnvUiKit.DropShadow(card);

            // Name row: [★] Name [Custom] — star and tag only shown for user envs.
            var nameRow = EnvUiKit.Group(card.transform, "NameRow");
            EnvUiKit.Place(EnvUiKit.Rt(nameRow), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(860f, 64f));
            var nameLayout = nameRow.AddComponent<HorizontalLayoutGroup>();
            nameLayout.childAlignment = TextAnchor.MiddleCenter;
            nameLayout.spacing = 14f;
            nameLayout.childControlWidth = true;
            nameLayout.childControlHeight = true;
            nameLayout.childForceExpandWidth = false;
            nameLayout.childForceExpandHeight = false;

            var star = EnvUiKit.Panel(nameRow.transform, "CustomStar", EnvUiSprites.Star, EnvUiKit.Star);
            star.raycastTarget = false;
            var starLayout = star.gameObject.AddComponent<LayoutElement>();
            starLayout.preferredWidth = starLayout.preferredHeight = 44f;

            var envName = EnvUiKit.Text(nameRow.transform, "EnvironmentName", "Environment",
                                        42, EnvUiKit.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            envName.enableWordWrapping = false;
            envName.overflowMode = TextOverflowModes.Ellipsis;

            var customTag = EnvUiKit.Panel(nameRow.transform, "CustomTag", EnvUiSprites.Capsule, EnvUiKit.AccentSoft);
            customTag.raycastTarget = false;
            var tagLayout = customTag.gameObject.AddComponent<LayoutElement>();
            tagLayout.preferredWidth = 160f;
            tagLayout.preferredHeight = 56f;
            var tagText = EnvUiKit.Text(customTag.transform, "Text", "Custom", 26, EnvUiKit.Accent,
                                        TextAlignmentOptions.Center, FontStyles.Bold);
            EnvUiKit.Stretch(EnvUiKit.Rt(tagText));

            // Preview: rounded mask, image fills without distortion.
            var frame = EnvUiKit.Panel(card.transform, "PreviewFrame", EnvUiSprites.RoundedRect, Color.white);
            EnvUiKit.Place(EnvUiKit.Rt(frame), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(840f, 500f));
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var preview = EnvUiKit.Panel(frame.transform, "PreviewImage", null, Color.white);
            preview.raycastTarget = false;
            EnvUiKit.Stretch(EnvUiKit.Rt(preview));
            var fitter = preview.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            // Description bullets in a muted box.
            var descBox = EnvUiKit.Panel(card.transform, "DescriptionBox", EnvUiSprites.RoundedRect, EnvUiKit.SurfaceMuted);
            EnvUiKit.Place(EnvUiKit.Rt(descBox), new Vector2(0.5f, 1f), new Vector2(0f, -668f), new Vector2(840f, 330f));
            var description = EnvUiKit.Text(descBox.transform, "Description", "",
                                            28, EnvUiKit.TextSecondary, TextAlignmentOptions.TopLeft);
            EnvUiKit.Stretch(EnvUiKit.Rt(description), 32f, 32f, 26f, 26f);
            description.lineSpacing = 14f;

            // Page dots.
            var dots = EnvUiKit.Group(card.transform, "Dots");
            var dotsRt = EnvUiKit.Rt(dots);
            EnvUiKit.Place(dotsRt, new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(700f, 24f));
            var dotsLayout = dots.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.childAlignment = TextAnchor.MiddleCenter;
            dotsLayout.spacing = 14f;
            dotsLayout.childControlWidth = false;
            dotsLayout.childControlHeight = false;
            var dotTemplate = EnvUiKit.Panel(dots.transform, "DotTemplate", EnvUiSprites.Circle, EnvUiKit.DotInactive);
            dotTemplate.raycastTarget = false;
            EnvUiKit.Rt(dotTemplate).sizeDelta = new Vector2(18f, 18f);
            dotTemplate.gameObject.SetActive(false);

            // ── carousel arrows ───────────────────────────────────────────────
            var left  = CircleArrow(screen.transform, "LeftArrow",  new Vector2(0f, 0.5f), 72f,  false);
            var right = CircleArrow(screen.transform, "RightArrow", new Vector2(1f, 0.5f), -72f, true);

            // ── bottom action bar ─────────────────────────────────────────────
            var bar = EnvUiKit.Group(screen.transform, "BottomBar");
            var barRt = EnvUiKit.Rt(bar);
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.offsetMin = new Vector2(60f, 60f);
            barRt.offsetMax = new Vector2(-60f, 170f);
            var barLayout = bar.AddComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 24f;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;
            barLayout.childForceExpandWidth = true;
            barLayout.childForceExpandHeight = true;

            var newEnv = EnvUiKit.OutlineButton(bar.transform, "NewEnvironmentButton", "New Environment", EnvUiKit.Accent, 30);
            newEnv.gameObject.AddComponent<CustomEnvEntry>();
            newEnv.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.5f;

            var edit = EnvUiKit.OutlineButton(bar.transform, "EditButton", "Edit", EnvUiKit.Accent, 30);
            edit.gameObject.AddComponent<LayoutElement>().flexibleWidth = 0.8f;

            var play = EnvUiKit.SolidButton(bar.transform, "PlayButton", "Play", EnvUiKit.Primary, Color.white, 34);
            play.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.2f;

            // ── wire the controller ───────────────────────────────────────────
            var so = new SerializedObject(controller);
            Set(so, "boundlessButton", boundless);
            Set(so, "portalButton", portal);
            Set(so, "boundlessLabel", boundless.GetComponentInChildren<TMP_Text>());
            Set(so, "portalLabel", portal.GetComponentInChildren<TMP_Text>());
            so.FindProperty("selectedColor").colorValue = EnvUiKit.Primary;
            so.FindProperty("unselectedColor").colorValue = EnvUiKit.Surface;
            so.FindProperty("selectedTextColor").colorValue = Color.white;
            so.FindProperty("unselectedTextColor").colorValue = EnvUiKit.TextSecondary;
            Set(so, "previewImage", preview);
            Set(so, "environmentName", envName);
            Set(so, "descriptionText", description);
            Set(so, "leftArrow", left);
            Set(so, "rightArrow", right);
            Set(so, "playButton", play);
            Set(so, "dotsContainer", dotsRt);
            Set(so, "dotTemplate", dotTemplate.gameObject);
            so.FindProperty("dotActiveColor").colorValue = EnvUiKit.Primary;
            so.FindProperty("dotInactiveColor").colorValue = EnvUiKit.DotInactive;
            Set(so, "customizeButton", newEnv.gameObject);
            Set(so, "customStar", star.gameObject);
            Set(so, "customTag", customTag.gameObject);
            Set(so, "editButton", edit);
            Set(so, "customEnvPreview", EnvUiSprites.CustomPreview);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static Button CircleArrow(Transform parent, string name, Vector2 anchor, float x, bool pointRight)
        {
            var bg = EnvUiKit.Panel(parent, name, EnvUiSprites.Circle, EnvUiKit.Surface);
            EnvUiKit.Place(EnvUiKit.Rt(bg), anchor, new Vector2(x, 70f), new Vector2(100f, 100f));
            EnvUiKit.DropShadow(bg);
            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            var icon = EnvUiKit.Icon(bg.transform, "Icon", EnvUiSprites.Chevron, EnvUiKit.TextPrimary, new Vector2(40f, 40f));
            if (pointRight) icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            return button;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CustomEnvBuilder — editor form, save/cancel, dialogs
        // ══════════════════════════════════════════════════════════════════════
        static void RebuildBuilderScreen()
        {
            var scene = EditorSceneManager.OpenScene(BuilderScenePath, OpenSceneMode.Single);
            var host = Object.FindFirstObjectByType<StartScreenUI>();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (host == null || canvas == null)
            {
                Debug.LogError("[EnvUiGenerator] CustomEnvBuilder scene needs a StartScreenUI and a Canvas.");
                return;
            }

            EnvUiKit.ConfigureCanvas(canvas);
            EnvUiKit.ClearChildren(canvas.transform);

            var screen = EnvUiKit.Panel(canvas.transform, "Screen", null, EnvUiKit.Background);
            screen.raycastTarget = false;
            EnvUiKit.Stretch(EnvUiKit.Rt(screen));

            var panel = EnvUiKit.Group(screen.transform, "EditorPanel");
            EnvUiKit.Stretch(EnvUiKit.Rt(panel));
            var editor = panel.AddComponent<EnvironmentEditorUI>();

            var title = EnvUiKit.Text(panel.transform, "Title", "Create Environment",
                                      52, EnvUiKit.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            EnvUiKit.Place(EnvUiKit.Rt(title), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(980f, 80f));

            // ── form card ─────────────────────────────────────────────────────
            var card = EnvUiKit.Panel(panel.transform, "FormCard", EnvUiSprites.RoundedRect, EnvUiKit.Surface);
            EnvUiKit.Stretch(EnvUiKit.Rt(card), 60f, 60f, 220f, 210f);
            EnvUiKit.DropShadow(card);

            var nameLabel = EnvUiKit.Text(card.transform, "NameLabel", "Environment Name",
                                          30, EnvUiKit.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            EnvUiKit.TopStretch(EnvUiKit.Rt(nameLabel), 48f, 48f, 34f, 42f);

            var nameInput = BuildNameInput(card.transform);
            EnvUiKit.TopStretch(EnvUiKit.Rt(nameInput), 40f, 40f, 88f, 92f);

            var formContainer = BuildFormScroll(card.transform);

            // ── bottom action bar ─────────────────────────────────────────────
            var bar = EnvUiKit.Group(panel.transform, "BottomBar");
            var barRt = EnvUiKit.Rt(bar);
            barRt.anchorMin = new Vector2(0f, 0f);
            barRt.anchorMax = new Vector2(1f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.offsetMin = new Vector2(60f, 60f);
            barRt.offsetMax = new Vector2(-60f, 170f);
            var barLayout = bar.AddComponent<HorizontalLayoutGroup>();
            barLayout.spacing = 24f;
            barLayout.childControlWidth = true;
            barLayout.childControlHeight = true;
            barLayout.childForceExpandWidth = true;
            barLayout.childForceExpandHeight = true;

            var cancel = EnvUiKit.OutlineButton(bar.transform, "CancelButton", "Cancel", EnvUiKit.TextPrimary, 32);
            var save = EnvUiKit.SolidButton(bar.transform, "SaveButton", "Save", EnvUiKit.Primary, Color.white, 34);

            // ── dialogs (last siblings, so they render on top) ────────────────
            var savedDialog = BuildSavedDialog(panel.transform, out var savedMessage, out var savedOk);
            var unsavedDialog = BuildUnsavedDialog(panel.transform, out var unsavedSave, out var unsavedDiscard, out var unsavedCancel);

            // ── wire the editor and its host ──────────────────────────────────
            editor.formContainer = formContainer;
            editor.sliderRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiDir}/SliderRow.prefab");
            editor.dropdownRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiDir}/DropdownRow.prefab");
            editor.headerRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UiDir}/HeaderRow.prefab");
            editor.nameInput = nameInput;
            editor.titleText = title;
            editor.saveButton = save;
            editor.cancelButton = cancel;
            editor.savedDialog = savedDialog;
            editor.savedMessage = savedMessage;
            editor.savedOkButton = savedOk;
            editor.unsavedDialog = unsavedDialog;
            editor.unsavedSaveButton = unsavedSave;
            editor.unsavedDiscardButton = unsavedDiscard;
            editor.unsavedCancelButton = unsavedCancel;
            EditorUtility.SetDirty(editor);

            host.editorPanel = panel;
            EditorUtility.SetDirty(host);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static TMP_InputField BuildNameInput(Transform parent)
        {
            var bg = EnvUiKit.Panel(parent, "NameInput", EnvUiSprites.RoundedRect, EnvUiKit.SurfaceMuted);
            var input = bg.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;

            var area = EnvUiKit.Group(bg.transform, "Text Area");
            EnvUiKit.Stretch(EnvUiKit.Rt(area), 28f, 28f, 12f, 12f);
            area.AddComponent<RectMask2D>();

            var placeholder = EnvUiKit.Text(area.transform, "Placeholder", "My Environment",
                                            30, EnvUiKit.TextSecondary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            EnvUiKit.Stretch(EnvUiKit.Rt(placeholder));
            var text = EnvUiKit.Text(area.transform, "Text", "",
                                     30, EnvUiKit.TextPrimary, TextAlignmentOptions.MidlineLeft);
            text.enableWordWrapping = false;
            EnvUiKit.Stretch(EnvUiKit.Rt(text));

            input.textViewport = EnvUiKit.Rt(area);
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        static RectTransform BuildFormScroll(Transform parent)
        {
            var root = EnvUiKit.Group(parent, "FormScroll");
            EnvUiKit.Stretch(EnvUiKit.Rt(root), 24f, 24f, 200f, 24f);
            var scroll = root.AddComponent<ScrollRect>();

            var viewport = EnvUiKit.Panel(root.transform, "Viewport", null, Color.clear);
            EnvUiKit.Stretch(EnvUiKit.Rt(viewport));
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = EnvUiKit.Group(viewport.transform, "Content");
            var contentRt = EnvUiKit.Rt(content);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 4, 24);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = EnvUiKit.Rt(viewport);
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return contentRt;
        }

        static GameObject BuildSavedDialog(Transform parent, out TMP_Text message, out Button ok)
        {
            var overlay = BuildOverlay(parent, "SavedDialog", 680f, out var card);

            var iconCircle = EnvUiKit.Panel(card, "IconCircle", EnvUiSprites.Circle, EnvUiKit.SuccessSoft);
            EnvUiKit.Place(EnvUiKit.Rt(iconCircle), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(150f, 150f));
            EnvUiKit.Icon(iconCircle.transform, "Check", EnvUiSprites.Check, EnvUiKit.Primary, new Vector2(72f, 72f));

            var title = EnvUiKit.Text(card, "Title", "Environment Saved!",
                                      40, EnvUiKit.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            EnvUiKit.Place(EnvUiKit.Rt(title), new Vector2(0.5f, 1f), new Vector2(0f, -260f), new Vector2(640f, 60f));

            message = EnvUiKit.Text(card, "Message", "",
                                    28, EnvUiKit.TextSecondary, TextAlignmentOptions.Center);
            EnvUiKit.Place(EnvUiKit.Rt(message), new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(620f, 90f));

            ok = EnvUiKit.SolidButton(card, "OkButton", "OK", EnvUiKit.Primary, Color.white, 32);
            EnvUiKit.Place(EnvUiKit.Rt(ok), new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(620f, 100f));

            return overlay;
        }

        static GameObject BuildUnsavedDialog(Transform parent, out Button save, out Button discard, out Button cancel)
        {
            var overlay = BuildOverlay(parent, "UnsavedDialog", 700f, out var card);

            var title = EnvUiKit.Text(card, "Title", "Unsaved Changes",
                                      40, EnvUiKit.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            EnvUiKit.Place(EnvUiKit.Rt(title), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(640f, 60f));

            var message = EnvUiKit.Text(card, "Message", "Do you want to save the edited environment?",
                                        28, EnvUiKit.TextSecondary, TextAlignmentOptions.Center);
            EnvUiKit.Place(EnvUiKit.Rt(message), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(620f, 90f));

            save = EnvUiKit.SolidButton(card, "SaveButton", "Save", EnvUiKit.SuccessSoft, EnvUiKit.Primary, 32);
            EnvUiKit.Place(EnvUiKit.Rt(save), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(620f, 100f));

            discard = EnvUiKit.SolidButton(card, "DiscardButton", "Discard", EnvUiKit.DangerSoft, EnvUiKit.Danger, 32);
            EnvUiKit.Place(EnvUiKit.Rt(discard), new Vector2(0.5f, 1f), new Vector2(0f, -370f), new Vector2(620f, 100f));

            cancel = EnvUiKit.OutlineButton(card, "CancelButton", "Cancel", EnvUiKit.TextPrimary, 32);
            EnvUiKit.Place(EnvUiKit.Rt(cancel), new Vector2(0.5f, 1f), new Vector2(0f, -490f), new Vector2(620f, 100f));

            return overlay;
        }

        static GameObject BuildOverlay(Transform parent, string name, float cardHeight, out Transform card)
        {
            var dim = EnvUiKit.Panel(parent, name, null, EnvUiKit.Dim); // raycast target: blocks the form
            EnvUiKit.Stretch(EnvUiKit.Rt(dim));

            var cardImage = EnvUiKit.Panel(dim.transform, "Card", EnvUiSprites.RoundedRect, EnvUiKit.Surface);
            EnvUiKit.Place(EnvUiKit.Rt(cardImage), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, cardHeight));
            EnvUiKit.DropShadow(cardImage);

            card = cardImage.transform;
            dim.gameObject.SetActive(false);
            return dim.gameObject;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  Form row prefabs (same paths as before, so GUIDs are stable)
        // ══════════════════════════════════════════════════════════════════════
        static void RebuildRowPrefabs()
        {
            SavePrefab(BuildHeaderRow(), $"{UiDir}/HeaderRow.prefab");
            SavePrefab(BuildSliderRow(), $"{UiDir}/SliderRow.prefab");
            SavePrefab(BuildDropdownRow(), $"{UiDir}/DropdownRow.prefab");
        }

        static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static GameObject NewRow(string name, float preferredHeight)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = LayerMask.NameToLayer("UI");
            root.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            return root;
        }

        static GameObject BuildHeaderRow()
        {
            var root = NewRow("HeaderRow", 84f);
            var label = EnvUiKit.Text(root.transform, "Label", "Header",
                                      34, EnvUiKit.TextPrimary, TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            EnvUiKit.Stretch(EnvUiKit.Rt(label), 8f, 8f, 8f, 4f);
            return root;
        }

        static GameObject BuildSliderRow()
        {
            var root = NewRow("SliderRow", 128f);

            var label = EnvUiKit.Text(root.transform, "Label", "Label",
                                      30, EnvUiKit.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            var labelRt = EnvUiKit.Rt(label);
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(0.62f, 1f);
            labelRt.pivot = new Vector2(0f, 1f);
            labelRt.offsetMin = new Vector2(8f, -48f);
            labelRt.offsetMax = new Vector2(0f, -6f);

            var value = EnvUiKit.Text(root.transform, "Value", "50%",
                                      28, EnvUiKit.TextSecondary, TextAlignmentOptions.TopRight);
            var valueRt = EnvUiKit.Rt(value);
            valueRt.anchorMin = new Vector2(0.62f, 1f);
            valueRt.anchorMax = new Vector2(1f, 1f);
            valueRt.pivot = new Vector2(1f, 1f);
            valueRt.offsetMin = new Vector2(0f, -48f);
            valueRt.offsetMax = new Vector2(-8f, -6f);

            BuildSlider(root.transform);
            return root;
        }

        static void BuildSlider(Transform parent)
        {
            var go = EnvUiKit.Group(parent, "Slider");
            var rt = EnvUiKit.Rt(go);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(10f, 16f);
            rt.offsetMax = new Vector2(-10f, 60f);
            var slider = go.AddComponent<Slider>();

            var track = EnvUiKit.Panel(go.transform, "Background", EnvUiSprites.Capsule, EnvUiKit.Border);
            var trackRt = EnvUiKit.Rt(track);
            trackRt.anchorMin = new Vector2(0f, 0.5f);
            trackRt.anchorMax = new Vector2(1f, 0.5f);
            trackRt.offsetMin = new Vector2(0f, -7f);
            trackRt.offsetMax = new Vector2(0f, 7f);

            var fillArea = EnvUiKit.Group(go.transform, "Fill Area");
            var fillAreaRt = EnvUiKit.Rt(fillArea);
            fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRt.offsetMin = new Vector2(0f, -7f);
            fillAreaRt.offsetMax = new Vector2(0f, 7f);
            var fill = EnvUiKit.Panel(fillArea.transform, "Fill", EnvUiSprites.Capsule, EnvUiKit.Primary);
            var fillRt = EnvUiKit.Rt(fill);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.sizeDelta = new Vector2(14f, 0f);

            var handleArea = EnvUiKit.Group(go.transform, "Handle Slide Area");
            EnvUiKit.Stretch(EnvUiKit.Rt(handleArea), 12f, 12f, 0f, 0f);
            var handle = EnvUiKit.Panel(handleArea.transform, "Handle", EnvUiSprites.Circle, EnvUiKit.Border);
            EnvUiKit.Rt(handle).sizeDelta = new Vector2(44f, 44f);
            var handleFill = EnvUiKit.Panel(handle.transform, "Fill", EnvUiSprites.Circle, EnvUiKit.Surface);
            handleFill.raycastTarget = false;
            EnvUiKit.Stretch(EnvUiKit.Rt(handleFill), 3f, 3f, 3f, 3f);

            slider.fillRect = fillRt;
            slider.handleRect = EnvUiKit.Rt(handle);
            slider.targetGraphic = handle;
        }

        static GameObject BuildDropdownRow()
        {
            var root = NewRow("DropdownRow", 116f);

            var label = EnvUiKit.Text(root.transform, "Label", "Label",
                                      30, EnvUiKit.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            var labelRt = EnvUiKit.Rt(label);
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.42f, 1f);
            labelRt.offsetMin = new Vector2(8f, 0f);
            labelRt.offsetMax = Vector2.zero;

            BuildDropdown(root.transform);
            return root;
        }

        static void BuildDropdown(Transform parent)
        {
            var bg = EnvUiKit.Panel(parent, "Dropdown", EnvUiSprites.RoundedRect, EnvUiKit.SurfaceMuted);
            var rt = EnvUiKit.Rt(bg);
            rt.anchorMin = new Vector2(0.42f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.offsetMin = new Vector2(8f, -42f);
            rt.offsetMax = new Vector2(-8f, 42f);
            var dropdown = bg.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = bg;

            var caption = EnvUiKit.Text(bg.transform, "Label", "Option",
                                        28, EnvUiKit.TextPrimary, TextAlignmentOptions.MidlineLeft);
            caption.enableWordWrapping = false;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            EnvUiKit.Stretch(EnvUiKit.Rt(caption), 24f, 60f, 6f, 6f);

            var arrow = EnvUiKit.Icon(bg.transform, "Arrow", EnvUiSprites.Chevron, EnvUiKit.TextSecondary, new Vector2(26f, 26f));
            EnvUiKit.Place(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(-32f, 0f), new Vector2(26f, 26f));
            arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, 90f); // "<" rotated to point down

            // ── template (expanded list) ──────────────────────────────────────
            var template = EnvUiKit.Panel(bg.transform, "Template", EnvUiSprites.RoundedRect, EnvUiKit.Surface);
            var templateRt = EnvUiKit.Rt(template);
            templateRt.anchorMin = new Vector2(0f, 0f);
            templateRt.anchorMax = new Vector2(1f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.anchoredPosition = new Vector2(0f, -8f);
            templateRt.sizeDelta = new Vector2(0f, 380f);
            EnvUiKit.DropShadow(template);
            var templateScroll = template.gameObject.AddComponent<ScrollRect>();

            var viewport = EnvUiKit.Panel(template.transform, "Viewport", null, Color.clear);
            EnvUiKit.Stretch(EnvUiKit.Rt(viewport), 4f, 4f, 4f, 4f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = EnvUiKit.Group(viewport.transform, "Content");
            var contentRt = EnvUiKit.Rt(content);
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 72f);

            var item = EnvUiKit.Group(content.transform, "Item");
            var itemRt = EnvUiKit.Rt(item);
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 72f);
            var toggle = item.AddComponent<Toggle>();

            var itemBg = EnvUiKit.Panel(item.transform, "Item Background", EnvUiSprites.RoundedRect, EnvUiKit.Surface);
            EnvUiKit.Stretch(EnvUiKit.Rt(itemBg), 2f, 2f, 2f, 2f);

            var itemCheck = EnvUiKit.Icon(item.transform, "Item Checkmark", EnvUiSprites.Check, EnvUiKit.Primary, new Vector2(30f, 30f));
            EnvUiKit.Place(itemCheck.rectTransform, new Vector2(0f, 0.5f), new Vector2(34f, 0f), new Vector2(30f, 30f));

            var itemLabel = EnvUiKit.Text(item.transform, "Item Label", "Option",
                                          28, EnvUiKit.TextPrimary, TextAlignmentOptions.MidlineLeft);
            EnvUiKit.Stretch(EnvUiKit.Rt(itemLabel), 68f, 24f, 4f, 4f);

            toggle.targetGraphic = itemBg;
            toggle.graphic = itemCheck;
            var colors = toggle.colors;
            colors.highlightedColor = EnvUiKit.AccentSoft;
            colors.pressedColor = EnvUiKit.AccentSoft;
            colors.selectedColor = EnvUiKit.AccentSoft;
            toggle.colors = colors;

            templateScroll.viewport = EnvUiKit.Rt(viewport);
            templateScroll.content = contentRt;
            templateScroll.horizontal = false;
            templateScroll.movementType = ScrollRect.MovementType.Clamped;

            dropdown.template = templateRt;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            template.gameObject.SetActive(false);
        }

        static void Set(SerializedObject so, string property, Object value) =>
            so.FindProperty(property).objectReferenceValue = value;
    }
}
