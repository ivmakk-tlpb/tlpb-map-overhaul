using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MapOverhaul.Game
{
    // Builds the map legend in the top-right corner of the map window, and keeps its toggles, its progress line, and its
    // collapse state. Expanded, it is a panel with the title "Legend" and one row for each kind; collapsed, one small
    // button. The collapse state lives in memory only, and each save load starts expanded.
    // An EventTrigger with no entries on the panel and on the button handles each pointer, drag, and scroll event on
    // their area, so the event system stops there and the map does not pan or zoom.
    internal static class LegendView
    {
        private const string Name = "MapOverhaulLegend";
        private const float Margin = 8f;
        private const float Padding = 6f;
        private const float Width = 160f;
        private const float HeaderHeight = 24f;
        private const float RowHeight = 22f;
        private const float BoxSize = 12f;
        private const float IconSize = 16f;
        private const float ButtonSize = 26f;
        private const float FontSize = 15f;
        private const float TitleSize = 17f;

        private static readonly Color Background = new Color(0.15f, 0.11f, 0.07f, 0.6f);
        private static readonly Color Text = new Color(0.89f, 0.80f, 0.63f, 1f);
        private static readonly Color TextOff = new Color(0.89f, 0.80f, 0.63f, 0.45f);
        private static readonly Color ProgressText = new Color(0.89f, 0.80f, 0.63f, 0.6f);
        private static readonly Color Line = new Color(0.89f, 0.80f, 0.63f, 0.25f);
        private static readonly Color Ink = new Color(52f / 255f, 38f / 255f, 24f / 255f, 1f);
        private static readonly Color FogIcon = new Color(0f, 0f, 0f, 0.7f);

        private static LegendModel model;
        private static GameObject root;
        private static GameObject expanded;
        private static RectTransform expandedRect;
        private static GameObject collapsedButton;
        private static GameObject[] checks;
        private static TextMeshProUGUI[] names;
        private static TextMeshProUGUI progress;
        private static string progressShown;
        private static bool collapsed;

        internal static void Build(MapUI ui)
        {
            model = new LegendModel(Plugin.Settings);
            var font = ui.handwrittenFont ?? ui.mapFont;
            var material = SharpMaterial(font);
            var label = ItemLabel();
            var rowFont = label?.font ?? font;
            var rowMaterial = label != null ? label.fontSharedMaterial : material;
            root = new GameObject(Name);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.SetParent(ui.body.transform, false);
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.anchoredPosition = new Vector2(-Margin, -Margin);
            rootRect.sizeDelta = new Vector2(Width, ButtonSize);

            int rows = model.Names.Length;
            expandedRect = Corner(rootRect, "Expanded", Width, HeaderHeight + rows * RowHeight + 2f * Padding + 4f);
            expanded = expandedRect.gameObject;
            Panel(expanded);
            var header = Part(expandedRect, "Header", Padding, -Padding, Width - 2f * Padding, HeaderHeight);
            Clickable(header, (Action)Collapse);
            var title = AddText(header, "Title", 0f, 0f, Width - 2f * Padding - 20f, HeaderHeight, font, material, TitleSize);
            title.text = "Legend";
            Lines(header, Width - 2f * Padding - 14f, -(HeaderHeight - 10f) / 2f);
            Graphic(Part(expandedRect, "Divider", Padding, -(Padding + HeaderHeight + 1f), Width - 2f * Padding, 1f), Line);

            checks = new GameObject[rows];
            names = new TextMeshProUGUI[rows];
            for (int row = 0; row < rows; row++) AddRow(row, rowFont, rowMaterial);
            progress = AddText(expandedRect, "Progress", Padding, -RowTop(rows), Width - 2f * Padding, RowHeight, rowFont, rowMaterial, FontSize - 2f);
            progress.color = ProgressText;
            progress.gameObject.SetActive(false);
            progressShown = null;

            var button = Corner(rootRect, "Collapsed", ButtonSize, ButtonSize);
            collapsedButton = button.gameObject;
            Panel(collapsedButton);
            // A GameObject takes one graphic only, so the button and its tint go on a child over the background.
            Clickable(Part(button, "Hit", 0f, 0f, ButtonSize, ButtonSize), (Action)Expand);
            Lines(button, (ButtonSize - 12f) / 2f, -(ButtonSize - 10f) / 2f);

            collapsed = false;
            Refresh(false);
            Plugin.Debug("Legend built, fonts " + (font == null ? "none" : font.name) + " and " + (rowFont == null ? "none" : rowFont.name));
        }

        // Shows the state of each row and the progress of the first scan. A network game hides the legend.
        internal static void Refresh(bool hidden)
        {
            if (root == null) return;
            root.SetActive(!hidden);
            if (hidden) return;
            expanded.SetActive(!collapsed);
            collapsedButton.SetActive(collapsed);
            for (int row = 0; row < checks.Length; row++)
            {
                bool on = model.IsOn(row);
                checks[row].SetActive(on);
                names[row].color = on ? Text : TextOff;
            }
            var (read, total) = ScanRunner.Progress;
            var text = LegendModel.ProgressText(read, total);
            if (text == progressShown) return;
            progressShown = text;
            if (text != null) progress.text = text;
            progress.gameObject.SetActive(text != null);
            int lines = checks.Length + (text != null ? 1 : 0);
            expandedRect.sizeDelta = new Vector2(Width, HeaderHeight + lines * RowHeight + 2f * Padding + 4f);
        }

        // Each save load starts with the legend expanded.
        internal static void Expand()
        {
            collapsed = false;
            Refresh(false);
        }

        internal static void Reset()
        {
            model = null;
            root = null;
            expanded = null;
            expandedRect = null;
            collapsedButton = null;
            checks = null;
            names = null;
            progress = null;
            progressShown = null;
            collapsed = false;
        }

        private static void Collapse()
        {
            collapsed = true;
            Refresh(false);
            Plugin.Debug("Legend collapsed");
        }

        // The game's material of the handwritten font has a wide, soft, light outline (softness 0.632, width 0.34,
        // dilate 0.6) that blurs small text on the dark panel. The legend uses a copy with a sharp, thin outline, so
        // the game's own map markers keep their look.
        private static Material SharpMaterial(TMP_FontAsset font)
        {
            if (font?.material == null) return null;
            var material = new Material(font.material);
            material.SetFloat("_OutlineSoftness", 0f);
            material.SetFloat("_OutlineWidth", 0.15f);
            material.SetFloat("_FaceDilate", 0.15f);
            return material;
        }

        // The text of an item label of the world, also of a pooled one: the rows take its font and its material
        // (Newsreader in game version 0.12.7), the same as the item labels and the labels of Proximity Pickup.
        private static TextMeshProUGUI ItemLabel()
        {
            var labels = Resources.FindObjectsOfTypeAll<HudObjectLabel>();
            for (int i = 0; i < labels.Length; i++)
                if (labels[i]?.text?.font != null) return labels[i].text;
            return null;
        }

        private static float RowTop(int row) => Padding + HeaderHeight + 4f + row * RowHeight;

        private static void AddRow(int row, TMP_FontAsset font, Material material)
        {
            var rowRect = Part(expandedRect, "Row " + model.Names[row], Padding, -RowTop(row), Width - 2f * Padding, RowHeight);
            int index = row;
            Clickable(rowRect, (Action)(() => Click(index)));

            var box = Part(rowRect, "Box", 0f, -(RowHeight - BoxSize) / 2f, BoxSize, BoxSize);
            Graphic(box, Text);
            Graphic(Part(box, "Inside", 1.5f, -1.5f, BoxSize - 3f, BoxSize - 3f), Ink);
            var check = Part(box, "Check", 3f, -3f, BoxSize - 6f, BoxSize - 6f);
            Graphic(check, Text);
            checks[row] = check.gameObject;

            var icon = Graphic(Part(rowRect, "Icon", BoxSize + 6f, -(RowHeight - IconSize) / 2f, IconSize, IconSize), Color.white);
            if (row <= (int)PoiKind.Silver) icon.sprite = IconSprites.Get((PoiKind)row);
            else icon.color = FogIcon;

            float textX = BoxSize + IconSize + 12f;
            names[row] = AddText(rowRect, "Name", textX, 0f, Width - 2f * Padding - textX, RowHeight, font, material, FontSize);
            names[row].text = model.Names[row];
        }

        private static void Click(int row)
        {
            model.Flip(row);
            Plugin.Debug("Legend click: " + model.Names[row] + (model.IsOn(row) ? " on" : " off"));
            Refresh(false);
        }

        // A button with a light tint under the pointer: the graphic is opaque, and the tint of the normal state is clear.
        private static void Clickable(RectTransform rect, Action action)
        {
            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = Text;
            var button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = hit;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.12f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.2f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.onClick.AddListener((UnityAction)(Action)(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Plugin.WarnOnce("Map Overhaul legend click", e);
                }
            }));
        }

        private static void Panel(GameObject panel)
        {
            var background = panel.AddComponent<Image>();
            background.color = Background;
            panel.AddComponent<EventTrigger>();
        }

        // Three short lines, the icon of a list, in a box of 12 by 10 at a place of its parent.
        private static void Lines(RectTransform parent, float x, float y)
        {
            var lines = Part(parent, "Lines", x, y, 12f, 10f);
            for (int i = 0; i < 3; i++) Graphic(Part(lines, "Line", 0f, -i * 4f, 12f, 2f), Text);
        }

        // A child at the top-right corner of its parent.
        private static RectTransform Corner(RectTransform parent, string name, float width, float height)
        {
            var rect = new GameObject(name).AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        // A child at a place from the top-left corner of its parent, in UI units.
        private static RectTransform Part(RectTransform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name).AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        private static Image Graphic(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform parent, string name, float x, float y, float width, float height,
            TMP_FontAsset font, Material material, float size)
        {
            var text = Part(parent, name, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            if (material != null) text.fontSharedMaterial = material;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            return text;
        }
    }
}
