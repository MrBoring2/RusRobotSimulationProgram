using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class TooltipEvents : MonoBehaviour
    {
        public VisualElement root; // Root вашего UI
        private Label tooltipLabel;
        private const float OFFSET_X = 12f;
        private const float OFFSET_Y = 30f;

        void Awake()
        {
            if (root == null)
                root = GetComponent<UIDocument>().rootVisualElement;

            // Создаём Label для tooltip
            tooltipLabel = new Label();
            tooltipLabel.style.position = Position.Absolute;
            tooltipLabel.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            tooltipLabel.style.color = Color.white;
            tooltipLabel.style.paddingLeft = 4;
            tooltipLabel.style.paddingRight = 4;
            tooltipLabel.style.paddingTop = 2;
            tooltipLabel.style.paddingBottom = 2;
            tooltipLabel.style.borderBottomLeftRadius = 4;
            tooltipLabel.style.borderTopRightRadius = 4;
            tooltipLabel.style.borderBottomRightRadius = 4;
            tooltipLabel.style.borderTopLeftRadius = 4;
            tooltipLabel.style.visibility = Visibility.Hidden;
            tooltipLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

            root.Add(tooltipLabel);
        }

        /// <summary>
        /// Привязывает tooltip к кнопке или любому элементу
        /// </summary>
        public void RegisterTooltip(VisualElement element, string text)
        {
            element.tooltip = text; // сохраняем текст

            element.RegisterCallback<MouseEnterEvent>(evt =>
            {
                tooltipLabel.text = element.tooltip;
                tooltipLabel.style.visibility = Visibility.Visible;
                UpdateTooltipPosition(evt.mousePosition);
            });

            element.RegisterCallback<MouseMoveEvent>(evt =>
            {
                UpdateTooltipPosition(evt.mousePosition);
            });

            element.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                tooltipLabel.style.visibility = Visibility.Hidden;
            });
        }

        private void UpdateTooltipPosition(Vector2 mousePos)
        {
            Vector2 panelPos = root.WorldToLocal(mousePos);
            float scale = root.panel.scaledPixelsPerPoint;
            tooltipLabel.style.left = panelPos.x + OFFSET_X / scale;
            tooltipLabel.style.top = panelPos.y + OFFSET_Y / scale;
        }
    }
}
