using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.EditorTools
{
    // The Explore UI design language in one place: palette, type scale, and the
    // widget builders the screen generator composes. Everything is sized for a
    // 1080x1920 portrait reference canvas.
    public static class EnvUiKit
    {
        // ── palette ──────────────────────────────────────────────────────────
        public static readonly Color Background      = Hex(0xEEF2F7);
        public static readonly Color Surface         = Color.white;
        public static readonly Color SurfaceMuted    = Hex(0xF1F4F8);
        public static readonly Color Border          = Hex(0xDCE3EC);
        public static readonly Color Primary         = Hex(0x2BA84A);
        public static readonly Color SuccessSoft     = Hex(0xDFF3E5);
        public static readonly Color Accent          = Hex(0x2B6BE4);
        public static readonly Color AccentSoft      = Hex(0xE4EDFF);
        public static readonly Color Danger          = Hex(0xE14D4D);
        public static readonly Color DangerSoft      = Hex(0xFDE8E8);
        public static readonly Color TextPrimary     = Hex(0x1E2A36);
        public static readonly Color TextSecondary   = Hex(0x66788C);
        public static readonly Color Star            = Hex(0xF5B301);
        public static readonly Color DotInactive     = Hex(0xC9D4E0);
        public static readonly Color Dim             = new Color(0.06f, 0.10f, 0.14f, 0.55f);
        public static readonly Color ShadowTint      = new Color(0.12f, 0.16f, 0.20f, 0.10f);

        static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

        static TMP_FontAsset Font => TMP_Settings.defaultFontAsset;

        // ── primitives ───────────────────────────────────────────────────────
        public static GameObject Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        public static Image Panel(Transform parent, string name, Sprite sprite, Color color)
        {
            var image = Group(parent, name).AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size,
                                           Color color, TextAlignmentOptions align,
                                           FontStyles style = FontStyles.Normal)
        {
            var tmp = Group(parent, name).AddComponent<TextMeshProUGUI>();
            tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ── layout helpers ───────────────────────────────────────────────────
        public static RectTransform Rt(Component c) => (RectTransform)c.transform;
        public static RectTransform Rt(GameObject go) => (RectTransform)go.transform;

        // Anchor min == max == pivot; position/size in reference pixels.
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        // Stretch horizontally at a fixed distance from the top edge.
        public static void TopStretch(RectTransform rt, float left, float right, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -(top + height));
            rt.offsetMax = new Vector2(-right, -top);
        }

        // ── widgets ──────────────────────────────────────────────────────────
        public static Button SolidButton(Transform parent, string name, string label,
                                         Color background, Color textColor,
                                         float textSize = 34, Sprite sprite = null)
        {
            var image = Panel(parent, name, sprite != null ? sprite : EnvUiSprites.RoundedRect, background);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Text(image.transform, "Text", label, textSize, textColor,
                            TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(Rt(text));
            return button;
        }

        // White button with a 3px border ring (border colour behind, surface in front).
        public static Button OutlineButton(Transform parent, string name, string label,
                                           Color textColor, float textSize = 32)
        {
            var ring = Panel(parent, name, EnvUiSprites.RoundedRect, Border);
            var fill = Panel(ring.transform, "Fill", EnvUiSprites.RoundedRect, Surface);
            Stretch(Rt(fill), 3, 3, 3, 3);
            fill.raycastTarget = false;

            var button = ring.gameObject.AddComponent<Button>();
            button.targetGraphic = ring;
            var text = Text(ring.transform, "Text", label, textSize, textColor,
                            TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(Rt(text));
            return button;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, Color color, Vector2 size)
        {
            var image = Panel(parent, name, sprite, color);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            Place(Rt(image), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return image;
        }

        public static void DropShadow(Graphic graphic)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowTint;
            shadow.effectDistance = new Vector2(0f, -6f);
        }

        public static Canvas ConfigureCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(t.GetChild(i).gameObject);
        }
    }
}
