using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.UI.CustomElements
{
    [UxmlElement]
    public partial class CustomScrollView : VisualElement
    {
        private VisualElement contentContainer;
        private VisualElement verticalScroller;
        private VisualElement horizontalScroller;
        private VisualElement verticalTrack;
        private VisualElement horizontalTrack;
        private VisualElement verticalThumb;
        private VisualElement horizontalThumb;

        private float contentWidth;
        private float contentHeight;
        private float viewportWidth;
        private float viewportHeight;

        public CustomScrollView()
        {
            // Основной стиль
            style.flexGrow = 1;
            style.flexShrink = 1;
            style.minWidth = 0;
            style.minHeight = 0;
            style.overflow = Overflow.Hidden;

            // Viewport (видимая область)
            var viewport = new VisualElement();
            viewport.name = "viewport";
            viewport.style.flexGrow = 1;
            viewport.style.overflow = Overflow.Hidden;
            Add(viewport);

            // Контент
            contentContainer = new VisualElement();
            contentContainer.name = "content-container";
            contentContainer.style.position = Position.Relative;
            viewport.Add(contentContainer);

            // Вертикальный скроллбар
            verticalScroller = new VisualElement();
            verticalScroller.name = "vertical-scroller";
            verticalScroller.style.position = Position.Absolute;
            verticalScroller.style.right = 0;
            verticalScroller.style.top = 0;
            verticalScroller.style.bottom = 0;
            verticalScroller.style.width = 8;
            verticalScroller.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            Add(verticalScroller);

            // Дорожка вертикального скроллбара
            verticalTrack = new VisualElement();
            verticalTrack.name = "vertical-track";
            verticalTrack.style.flexGrow = 1;
            verticalTrack.style.marginBottom = 2;
            verticalTrack.style.marginLeft = 2;
            verticalTrack.style.marginRight = 2;
            verticalTrack.style.marginTop = 2;
            verticalScroller.Add(verticalTrack);

            // Ползунок вертикального скроллбара
            verticalThumb = new VisualElement();
            verticalThumb.name = "vertical-thumb";
            verticalThumb.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            verticalThumb.style.borderBottomWidth = 4;
            verticalThumb.style.borderTopWidth = 4;
            verticalThumb.style.borderLeftWidth = 4;
            verticalThumb.style.borderRightWidth = 4;
            verticalThumb.style.minHeight = 20;
            verticalTrack.Add(verticalThumb);

            // Горизонтальный скроллбар
            horizontalScroller = new VisualElement();
            horizontalScroller.name = "horizontal-scroller";
            horizontalScroller.style.position = Position.Absolute;
            horizontalScroller.style.left = 0;
            horizontalScroller.style.right = 8; // Отступ под вертикальный скроллбар
            horizontalScroller.style.bottom = 0;
            horizontalScroller.style.height = 8;
            horizontalScroller.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            Add(horizontalScroller);

            // Дорожка горизонтального скроллбара
            horizontalTrack = new VisualElement();
            horizontalTrack.name = "horizontal-track";
            horizontalTrack.style.flexGrow = 1;
            horizontalTrack.style.marginBottom = 2;
            horizontalTrack.style.marginLeft = 2;
            horizontalTrack.style.marginRight = 2;
            horizontalTrack.style.marginTop = 2;
            horizontalScroller.Add(horizontalTrack);

            // Ползунок горизонтального скроллбара
            horizontalThumb = new VisualElement();
            horizontalThumb.name = "horizontal-thumb";
            horizontalThumb.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            horizontalThumb.style.borderBottomWidth = 4;
            horizontalThumb.style.borderTopWidth = 4;
            horizontalThumb.style.borderLeftWidth = 4;
            horizontalThumb.style.borderRightWidth = 4;
            horizontalThumb.style.minWidth = 20;
            horizontalTrack.Add(horizontalThumb);

            // Регистрация событий
            RegisterCallbacks();

            // Изначально скрываем скроллбары
            UpdateScrollbars();
        }

        public VisualElement ContentContainer => contentContainer;

        private void RegisterCallbacks()
        {
            // Прокрутка колесиком мыши
            this.RegisterCallback<WheelEvent>(OnWheel);

            // Drag для вертикального ползунка
            verticalThumb.RegisterCallback<PointerDownEvent>(OnVerticalThumbDown);
            verticalThumb.RegisterCallback<PointerUpEvent>(OnVerticalThumbUp);

            // Drag для горизонтального ползунка
            horizontalThumb.RegisterCallback<PointerDownEvent>(OnHorizontalThumbDown);
            horizontalThumb.RegisterCallback<PointerUpEvent>(OnHorizontalThumbUp);

            // Клик по дорожке для быстрой прокрутки
            verticalTrack.RegisterCallback<PointerDownEvent>(OnVerticalTrackClick);
            horizontalTrack.RegisterCallback<PointerDownEvent>(OnHorizontalTrackClick);

            // Изменение размера
            this.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateScrollbars();
        }

        private void UpdateScrollbars()
        {
            // Получаем размеры
            viewportWidth = contentContainer.parent.resolvedStyle.width;
            viewportHeight = contentContainer.parent.resolvedStyle.height;
            contentWidth = contentContainer.resolvedStyle.width;
            contentHeight = contentContainer.resolvedStyle.height;

            // Проверяем нужны ли скроллбары
            bool needVertical = contentHeight > viewportHeight;
            bool needHorizontal = contentWidth > viewportWidth;

            // Показываем/скрываем скроллбары
            verticalScroller.style.display = needVertical ? DisplayStyle.Flex : DisplayStyle.None;
            horizontalScroller.style.display = needHorizontal ? DisplayStyle.Flex : DisplayStyle.None;

            // Обновляем размеры ползунков
            if (needVertical)
            {
                float thumbHeight = (viewportHeight / contentHeight) * (viewportHeight - 4);
                verticalThumb.style.height = Mathf.Max(thumbHeight, 20);
            }

            if (needHorizontal)
            {
                float thumbWidth = (viewportWidth / contentWidth) * (viewportWidth - 4);
                horizontalThumb.style.width = Mathf.Max(thumbWidth, 20);
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            // Вертикальная прокрутка колесиком
            contentContainer.style.top = contentContainer.style.top.value.value - evt.delta.y * 20;
            ClampScrollPosition();
            UpdateThumbPositions();
            evt.StopPropagation();
        }

        private void OnVerticalThumbDown(PointerDownEvent evt)
        {
            // Начало drag вертикального ползунка
            verticalThumb.CapturePointer(evt.pointerId);
            verticalThumb.RegisterCallback<PointerMoveEvent>(OnVerticalThumbMove);
            evt.StopPropagation();
        }

        private void OnVerticalThumbUp(PointerUpEvent evt)
        {
            // Конец drag
            verticalThumb.ReleasePointer(evt.pointerId);
            verticalThumb.UnregisterCallback<PointerMoveEvent>(OnVerticalThumbMove);
            evt.StopPropagation();
        }

        private void OnVerticalThumbMove(PointerMoveEvent evt)
        {
            // Drag вертикального ползунка
            float deltaY = evt.deltaPosition.y;
            float trackHeight = verticalTrack.resolvedStyle.height - verticalThumb.resolvedStyle.height;

            if (trackHeight > 0)
            {
                float percent = deltaY / trackHeight;
                ScrollVertical(percent);
            }
            evt.StopPropagation();
        }

        private void OnHorizontalThumbDown(PointerDownEvent evt)
        {
            horizontalThumb.CapturePointer(evt.pointerId);
            horizontalThumb.RegisterCallback<PointerMoveEvent>(OnHorizontalThumbMove);
            evt.StopPropagation();
        }

        private void OnHorizontalThumbUp(PointerUpEvent evt)
        {
            horizontalThumb.ReleasePointer(evt.pointerId);
            horizontalThumb.UnregisterCallback<PointerMoveEvent>(OnHorizontalThumbMove);
            evt.StopPropagation();
        }

        private void OnHorizontalThumbMove(PointerMoveEvent evt)
        {
            float deltaX = evt.deltaPosition.x;
            float trackWidth = horizontalTrack.resolvedStyle.width - horizontalThumb.resolvedStyle.width;

            if (trackWidth > 0)
            {
                float percent = deltaX / trackWidth;
                ScrollHorizontal(percent);
            }
            evt.StopPropagation();
        }

        private void OnVerticalTrackClick(PointerDownEvent evt)
        {
            // Быстрая прокрутка при клике на дорожку
            float localY = evt.localPosition.y;
            float thumbHeight = verticalThumb.resolvedStyle.height;
            float trackHeight = verticalTrack.resolvedStyle.height;

            if (localY < verticalThumb.style.top.value.value)
            {
                // Клик выше ползунка - прокрутка вверх
                ScrollVertical(-0.1f);
            }
            else if (localY > verticalThumb.style.top.value.value + thumbHeight)
            {
                // Клик ниже ползунка - прокрутка вниз
                ScrollVertical(0.1f);
            }
            evt.StopPropagation();
        }

        private void OnHorizontalTrackClick(PointerDownEvent evt)
        {
            float localX = evt.localPosition.x;
            float thumbWidth = horizontalThumb.resolvedStyle.width;
            float trackWidth = horizontalTrack.resolvedStyle.width;

            if (localX < horizontalThumb.style.left.value.value)
            {
                ScrollHorizontal(-0.1f);
            }
            else if (localX > horizontalThumb.style.left.value.value + thumbWidth)
            {
                ScrollHorizontal(0.1f);
            }
            evt.StopPropagation();
        }

        private void ScrollVertical(float percent)
        {
            float maxScroll = contentHeight - viewportHeight;
            if (maxScroll > 0)
            {
                float currentTop = contentContainer.style.top.value.value;
                float newTop = currentTop - (percent * maxScroll);
                contentContainer.style.top = Mathf.Clamp(newTop, -maxScroll, 0);
                UpdateThumbPositions();
            }
        }

        private void ScrollHorizontal(float percent)
        {
            float maxScroll = contentWidth - viewportWidth;
            if (maxScroll > 0)
            {
                float currentLeft = contentContainer.style.left.value.value;
                float newLeft = currentLeft - (percent * maxScroll);
                contentContainer.style.left = Mathf.Clamp(newLeft, -maxScroll, 0);
                UpdateThumbPositions();
            }
        }

        private void ClampScrollPosition()
        {
            float maxVertical = Mathf.Max(0, contentHeight - viewportHeight);
            float maxHorizontal = Mathf.Max(0, contentWidth - viewportWidth);

            float currentTop = contentContainer.style.top.value.value;
            float currentLeft = contentContainer.style.left.value.value;

            contentContainer.style.top = Mathf.Clamp(currentTop, -maxVertical, 0);
            contentContainer.style.left = Mathf.Clamp(currentLeft, -maxHorizontal, 0);
        }

        private void UpdateThumbPositions()
        {
            // Обновляем позиции ползунков
            float maxVertical = Mathf.Max(0, contentHeight - viewportHeight);
            float maxHorizontal = Mathf.Max(0, contentWidth - viewportWidth);

            if (maxVertical > 0)
            {
                float scrollPercent = -contentContainer.style.top.value.value / maxVertical;
                float trackHeight = verticalTrack.resolvedStyle.height - verticalThumb.resolvedStyle.height;
                verticalThumb.style.top = scrollPercent * trackHeight;
            }

            if (maxHorizontal > 0)
            {
                float scrollPercent = -contentContainer.style.left.value.value / maxHorizontal;
                float trackWidth = horizontalTrack.resolvedStyle.width - horizontalThumb.resolvedStyle.width;
                horizontalThumb.style.left = scrollPercent * trackWidth;
            }
        }
        public void Refresh()
        {
            UpdateScrollbars();
            UpdateThumbPositions();
        }
        // Публичные методы для управления
        public void ScrollToTop()
        {
            contentContainer.style.top = 0;
            UpdateThumbPositions();
        }

        public void ScrollToBottom()
        {
            float maxScroll = Mathf.Max(0, contentHeight - viewportHeight);
            contentContainer.style.top = -maxScroll;
            UpdateThumbPositions();
        }

        public void ScrollToLeft()
        {
            contentContainer.style.left = 0;
            UpdateThumbPositions();
        }

        public void ScrollToRight()
        {
            float maxScroll = Mathf.Max(0, contentWidth - viewportWidth);
            contentContainer.style.left = -maxScroll;
            UpdateThumbPositions();
        }
    }
}
