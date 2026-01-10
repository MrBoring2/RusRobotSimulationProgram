using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class MainUiEvents : MonoBehaviour
    {
        private VisualElement root;
        private VisualElement leftPanel;
        private VisualElement resizer;
        private const float MIN_WIDTH = 160f;
        private const float MAX_WIDTH = 480f;
        private bool isResizing = false; 
        private float startWidth;
        private float startMouseX;
        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            leftPanel = root.Q<VisualElement>("hierarchy-container");
            resizer = root.Q<VisualElement>("hierarchy-resizer");
            InitEvents();
        }

        private void InitEvents()
        {
            resizer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            resizer.RegisterCallback<PointerUpEvent>(OnPointerUp);
            resizer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp);
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove);

            // Изменяем курсор при наведении
            //resizer.style.cursor = new StyleCursor(CursorAsset.CreateColResizeCursor());

        }
        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return; // Только левая кнопка мыши

            isResizing = true;
            startWidth = leftPanel.resolvedStyle.width;
            startMouseX = evt.position.x;

            // Захватываем указатель
            resizer.CapturePointer(evt.pointerId);

            // Блокируем выделение текста во время ресайза
            resizer.focusable = true;
            resizer.Focus();

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!isResizing) return;

            isResizing = false;

            // Освобождаем указатель
            if (resizer.HasPointerCapture(evt.pointerId))
            {
                resizer.ReleasePointer(evt.pointerId);
            }

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isResizing) return;

            float deltaX = evt.position.x - startMouseX;
            float newWidth = Mathf.Clamp(startWidth + deltaX, MIN_WIDTH, MAX_WIDTH);

            // Применяем новую ширину
            leftPanel.style.width = newWidth;
            leftPanel.style.minWidth = newWidth;
            leftPanel.style.maxWidth = newWidth;

            evt.StopPropagation();
        }

        private void OnDestroy()
        {
            if (resizer != null)
            {
                resizer.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                resizer.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                resizer.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            }

            if (root != null)
            {
                root.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                root.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            }
        }
    }
}
