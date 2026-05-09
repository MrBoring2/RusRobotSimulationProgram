using Assets.Scripts.Models;
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
        private readonly Dictionary<VisualElement, TooltipCallbacks> _callbacks =
            new Dictionary<VisualElement, TooltipCallbacks>();
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
            element.tooltip = text;

            var cb = new TooltipCallbacks
            {
                Enter = evt => OnMouseEnterTooltip(evt, element),
                Move = evt => OnMouseMoveTooltip(evt),
                Leave = evt => OnMouseLeaveTooltip(evt),
                Click = evt => OnMouseClickTooltip(evt, element)
            };
            _callbacks[element] = cb;

            element.RegisterCallback(cb.Enter);
            element.RegisterCallback(cb.Move);
            element.RegisterCallback(cb.Leave);
        }
        private void OnMouseClickTooltip(ClickEvent evt, VisualElement element)
        {
            // Обновляем текст tooltip и показываем его
            tooltipLabel.text = element.tooltip;
            tooltipLabel.style.visibility = Visibility.Visible;
            UpdateTooltipPosition(evt.originalMousePosition);
        }
        private void OnMouseEnterTooltip(MouseEnterEvent evt, VisualElement element)
        {
            tooltipLabel.text = element.tooltip;
            tooltipLabel.style.visibility = Visibility.Visible;
            UpdateTooltipPosition(evt.mousePosition);
        }
        private void OnMouseMoveTooltip(MouseMoveEvent evt)
        {
            UpdateTooltipPosition(evt.mousePosition);
        }
        private void OnMouseLeaveTooltip(MouseLeaveEvent evt)
        {
            tooltipLabel.style.visibility = Visibility.Hidden;
        }

        public void UnregisterTooltip(VisualElement element)
        {
            if (!_callbacks.TryGetValue(element, out var cb))
                return;

            element.UnregisterCallback(cb.Enter);
            element.UnregisterCallback(cb.Move);
            element.UnregisterCallback(cb.Leave);
            element.UnregisterCallback(cb.Click);
            _callbacks.Remove(element);
            tooltipLabel.style.visibility = Visibility.Hidden;
        }

        public void ForceUpdateTooltip(VisualElement element)
        {
            tooltipLabel.text = element.tooltip;
            tooltipLabel.style.visibility = Visibility.Visible;
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
