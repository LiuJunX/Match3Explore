using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Match3.Unity.UI
{
    /// <summary>
    /// Factory for creating UI elements at runtime.
    /// Follows ViewFactory pattern - no prefabs, all objects created dynamically.
    /// </summary>
    public static class UIFactory
    {
        private const int DefaultSortingOrder = 100;

        /// <summary>
        /// Create a Canvas with CanvasScaler for screen space overlay.
        /// </summary>
        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = DefaultSortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        /// <summary>
        /// Create a panel with background color.
        /// </summary>
        public static RectTransform CreatePanel(Transform parent, Color bgColor, string name = "Panel")
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = bgColor;

            return rect;
        }

        /// <summary>
        /// Create TextMeshPro text element.
        /// </summary>
        public static TextMeshProUGUI CreateText(
            Transform parent,
            string text,
            int fontSize,
            Color? color = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            string name = "Text")
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color ?? Color.white;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = false;

            return tmp;
        }

        /// <summary>
        /// Create a button with text.
        /// </summary>
        public static Button CreateButton(
            Transform parent,
            string text,
            UnityAction onClick,
            Color? bgColor = null,
            Color? textColor = null,
            int fontSize = 24,
            string name = "Button")
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = bgColor ?? new Color(0.2f, 0.2f, 0.2f, 0.9f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            // Set button colors
            var colors = button.colors;
            colors.normalColor = bgColor ?? new Color(0.2f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            colors.selectedColor = colors.normalColor;
            button.colors = colors;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            // Add text
            var textObj = CreateText(rect, text, fontSize, textColor ?? Color.white, name: "Label");
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            return button;
        }

        /// <summary>
        /// Create a slider with min/max range.
        /// </summary>
        public static Slider CreateSlider(
            Transform parent,
            float min,
            float max,
            float value,
            string name = "Slider")
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(200, 20);

            var slider = go.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;

            // Background
            var bgGo = new GameObject("Background");
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.SetParent(rect, false);
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Fill area
            var fillAreaGo = new GameObject("Fill Area");
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.SetParent(rect, false);
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // Fill
            var fillGo = new GameObject("Fill");
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.SetParent(fillAreaRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.sizeDelta = new Vector2(10, 0);
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f, 1f);
            slider.fillRect = fillRect;

            // Handle area
            var handleAreaGo = new GameObject("Handle Slide Area");
            var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRect.SetParent(rect, false);
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Handle
            var handleGo = new GameObject("Handle");
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.SetParent(handleAreaRect, false);
            handleRect.sizeDelta = new Vector2(20, 0);
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;
            slider.handleRect = handleRect;

            slider.targetGraphic = handleImage;

            return slider;
        }

        /// <summary>
        /// Create an Image element.
        /// </summary>
        public static Image CreateImage(
            Transform parent,
            Sprite sprite = null,
            Color? color = null,
            string name = "Image")
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color ?? Color.white;

            return image;
        }

        /// <summary>
        /// Create a horizontal layout group.
        /// </summary>
        public static HorizontalLayoutGroup CreateHorizontalLayout(
            Transform parent,
            float spacing = 10f,
            TextAnchor childAlignment = TextAnchor.MiddleCenter,
            string name = "HorizontalLayout")
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = childAlignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return layout;
        }

        /// <summary>
        /// Create a vertical layout group.
        /// </summary>
        public static VerticalLayoutGroup CreateVerticalLayout(
            Transform parent,
            float spacing = 10f,
            TextAnchor childAlignment = TextAnchor.MiddleCenter,
            string name = "VerticalLayout")
        {
            var go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = childAlignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return layout;
        }

        /// <summary>
        /// Configure RectTransform anchors for common layouts.
        /// </summary>
        public static void SetAnchors(RectTransform rect, AnchorPreset preset)
        {
            switch (preset)
            {
                case AnchorPreset.TopStretch:
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    break;

                case AnchorPreset.BottomStretch:
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    break;

                case AnchorPreset.Center:
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;

                case AnchorPreset.Stretch:
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
            }
        }
    }

    public enum AnchorPreset
    {
        TopStretch,
        BottomStretch,
        Center,
        Stretch
    }
}
