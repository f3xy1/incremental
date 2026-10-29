using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace VhsInheritance
{
    public static class VhsUIFactory
    {
        private static TMP_FontAsset runtimeFont;

        public static TMP_FontAsset Font
        {
            get
            {
                if (runtimeFont == null)
                {
                    runtimeFont = Resources.Load<TMP_FontAsset>("Fonts/SalonRounded");
                    if (runtimeFont == null) runtimeFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    if (runtimeFont != null)
                    {
                        return runtimeFont;
                    }

                    UnityEngine.Font sourceFont = UnityEngine.Font.CreateDynamicFontFromOSFont(
                        new[] { "Segoe UI", "Arial", "Liberation Sans" }, 36);
                    if (sourceFont == null)
                    {
                        sourceFont = Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
                    }

                    if (sourceFont != null)
                    {
                        runtimeFont = TMP_FontAsset.CreateFontAsset(sourceFont);
                        if (runtimeFont != null)
                        {
                            runtimeFont.name = "VHS Runtime Cyrillic Font";
                            runtimeFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                            runtimeFont.hideFlags = HideFlags.DontSave;
                        }
                    }
                }

                return runtimeFont;
            }
        }

        public static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(parent, false);
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        public static UnityEngine.UI.Image Panel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            bool fillsScreen = anchorMin == Vector2.zero && anchorMax == Vector2.one && sizeDelta == Vector2.zero;
            image.sprite = fillsScreen ? VhsSpriteFactory.White() : VhsVectorArt.RoundedPanel();
            image.type = fillsScreen ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
            image.raycastTarget = color.a > 0.01f;
            if (!fillsScreen && sizeDelta.x > 90 && sizeDelta.y > 45 && color.a > 0.1f) AddBorder(rect);
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string value, float fontSize, Color color, TextAlignmentOptions alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, bool bold = false)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = fontSize * 0.8f;
            text.fontSizeMax = fontSize;
            return text;
        }

        public static UnityEngine.UI.Button Button(string name, Transform parent, string label, Color normalColor, Color textColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, out TextMeshProUGUI labelText)
        {
            RectTransform rect = Rect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = VhsVectorArt.RoundedPanel();
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.color = normalColor;
            image.raycastTarget = true;
            AddBorder(rect);

            UnityEngine.UI.Button button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = image;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Automatic };
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            labelText = Text("Label", rect, label, Mathf.Min(34f, sizeDelta.y * 0.45f), textColor, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, -8f), true);
            return button;
        }

        private static void AddBorder(RectTransform parent)
        {
            var rect = Rect("InkOutline", parent, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            var outline = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            outline.sprite = VhsVectorArt.PanelBorder();
            outline.type = UnityEngine.UI.Image.Type.Sliced;
            outline.color = new Color32(48, 39, 53, 255);
            outline.raycastTarget = false;
        }

        public static TextMeshPro WorldText(string name, Transform parent, string value, float fontSize, Color color, Vector3 localPosition, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            TextMeshPro text = gameObject.GetComponent<TextMeshPro>();
            text.font = Font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.sortingOrder = 250;
            text.fontStyle = FontStyles.Bold;
            text.outlineWidth = 0.15f;
            text.outlineColor = new Color32(19, 27, 35, 255);
            return text;
        }
    }
}
