using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LostPage
{
    public static class UiFactory
    {
        private static Font _font;

        public static readonly Color Background = new Color32(20, 22, 31, 255);
        public static readonly Color Panel = new Color32(36, 40, 54, 245);
        public static readonly Color PanelLight = new Color32(54, 60, 78, 255);
        public static readonly Color TextColor = new Color32(240, 238, 226, 255);
        public static readonly Color Accent = new Color32(211, 167, 78, 255);
        public static readonly Color Disabled = new Color32(74, 76, 82, 255);
        public static readonly Color Red = new Color32(182, 64, 67, 255);
        public static readonly Color Blue = new Color32(65, 119, 181, 255);
        public static readonly Color Yellow = new Color32(215, 174, 55, 255);
        public static readonly Color Purple = new Color32(132, 81, 174, 255);
        public static readonly Color Green = new Color32(68, 145, 100, 255);

        public static Font Font
        {
            get
            {
                if (_font != null)
                {
                    return _font;
                }

                try
                {
                    _font = Font.CreateDynamicFontFromOSFont(
                        new[]
                        {
                            "Yu Gothic UI",
                            "Yu Gothic",
                            "Meiryo",
                            "Noto Sans CJK JP",
                            "Noto Sans JP",
                            "sans-serif"
                        },
                        32);
                }
                catch (Exception)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _font;
            }
        }

        public static Canvas CreateCanvas(string name)
        {
            var canvasObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static RectTransform CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string value,
            int fontSize = 28,
            TextAnchor alignment = TextAnchor.MiddleCenter,
            Color? color = null)
        {
            var rect = CreateRect(name, parent, anchorMin, anchorMax);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color ?? TextColor;
            text.text = value;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string label,
            UnityAction onClick,
            Color? background = null,
            int fontSize = 28)
        {
            var rect = CreatePanel(
                name,
                parent,
                anchorMin,
                anchorMax,
                background ?? PanelLight);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.48f, 0.48f, 0.48f, 0.72f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var labelText = CreateText(
                "Label",
                rect,
                new Vector2(0.04f, 0.04f),
                new Vector2(0.96f, 0.96f),
                label,
                fontSize);
            labelText.raycastTarget = false;
            return button;
        }

        public static ScrollRect CreateScrollView(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool horizontal,
            bool vertical,
            out RectTransform content)
        {
            var root = CreatePanel(name, parent, anchorMin, anchorMax, Panel);
            var viewport = CreateRect(
                "Viewport",
                root,
                new Vector2(0.01f, 0.02f),
                new Vector2(0.99f, 0.98f));
            viewport.gameObject.AddComponent<Image>().color =
                new Color(0f, 0f, 0f, 0.01f);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = CreateRect(
                "Content",
                viewport,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f));
            content.pivot = new Vector2(0f, 0.5f);

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = horizontal;
            scroll.vertical = vertical;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 45f;
            return scroll;
        }

        public static void CreateBar(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float ratio,
            Color fillColor)
        {
            var background = CreatePanel(
                name,
                parent,
                anchorMin,
                anchorMax,
                new Color32(24, 25, 31, 255));
            var clampedRatio = Mathf.Clamp01(ratio);
            CreatePanel(
                "Fill",
                background,
                Vector2.zero,
                new Vector2(clampedRatio, 1f),
                fillColor);
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(eventSystem);
        }

        public static Color GetEtherColor(EtherType type)
        {
            switch (type)
            {
                case EtherType.Red:
                    return Red;
                case EtherType.Blue:
                    return Blue;
                case EtherType.Yellow:
                    return Yellow;
                case EtherType.Purple:
                    return Purple;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static Color GetCardColor(CardKind kind)
        {
            switch (CardCatalog.GetCategory(kind))
            {
                case CardCategory.Attack:
                    return new Color32(125, 55, 60, 255);
                case CardCategory.Defense:
                    return new Color32(53, 89, 133, 255);
                case CardCategory.Charge:
                    return new Color32(137, 110, 44, 255);
                case CardCategory.Persistent:
                    return new Color32(91, 55, 122, 255);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
