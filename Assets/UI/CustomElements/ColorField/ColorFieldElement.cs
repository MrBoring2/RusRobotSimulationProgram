using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.UI.CustomElements.ColorField
{
    public class ColorFieldElement : VisualElement
    {
        private VisualElement colorSwatch;
        private Label hexLabel;
        private Color currentColor;

        public Color CurrentColor
        {
            get => currentColor;
            set
            {
                currentColor = value;
                UpdateVisuals();
            }
        }

        public Action<ColorFieldElement> OnClicked;

        public ColorFieldElement()
        {
            AddToClassList("base-property");
            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.height = 24;

            // Цветной квадрат
            colorSwatch = new VisualElement();
            colorSwatch.style.width = 36;
            colorSwatch.style.height = 20;
            colorSwatch.style.borderTopLeftRadius = 3;
            colorSwatch.style.borderTopRightRadius = 3;
            colorSwatch.style.borderBottomLeftRadius = 3;
            colorSwatch.style.borderBottomRightRadius = 3;
            colorSwatch.style.borderBottomWidth = 3;
            colorSwatch.style.borderLeftWidth = 3;
            colorSwatch.style.borderTopWidth = 3;
            colorSwatch.style.borderRightWidth = 3;
            colorSwatch.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
            colorSwatch.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
            colorSwatch.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
            colorSwatch.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            colorSwatch.style.marginRight = 8;
            colorSwatch.style.cursor = new StyleCursor(StyleKeyword.Auto);

            // HEX надпись
            hexLabel = new Label();
            hexLabel.style.color = Color.white;
            hexLabel.style.fontSize = 13;
            hexLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            hexLabel.style.marginBottom = 5;
            hexLabel.style.marginTop = 0;
            Add(colorSwatch);
            Add(hexLabel);

            // Клик — вызываем событие
            RegisterCallback<ClickEvent>(evt =>
            {
                OnClicked?.Invoke(this);
                evt.StopPropagation();
            });

            // Визуальная обратная связь при наведении
            RegisterCallback<MouseEnterEvent>(evt =>
            {
                colorSwatch.style.borderBottomColor = new Color(0.7f, 0.7f, 0.7f);
                colorSwatch.style.borderTopColor = new Color(0.7f, 0.7f, 0.7f);
                colorSwatch.style.borderLeftColor = new Color(0.7f, 0.7f, 0.7f);
                colorSwatch.style.borderRightColor = new Color(0.7f, 0.7f, 0.7f);
            });
            RegisterCallback<MouseLeaveEvent>(evt =>
            {
                colorSwatch.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f);
                colorSwatch.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f);
                colorSwatch.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f);
                colorSwatch.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f);
            });

            CurrentColor = Color.white;
        }

        private void UpdateVisuals()
        {
            colorSwatch.style.backgroundColor = new StyleColor(currentColor);
            hexLabel.text = "#" + ColorUtility.ToHtmlStringRGB(currentColor);
        }
    }
}
