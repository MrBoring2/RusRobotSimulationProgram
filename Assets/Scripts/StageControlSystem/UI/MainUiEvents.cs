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
        private VisualElement leftColumn;  // Теперь это left-column, а не left-panel
        private VisualElement resizer;
        private const float MIN_WIDTH = 160f;
        private const float MAX_WIDTH = 480f;
        private bool isResizing = false;
        private float startWidth;
        private float startMouseX;

        private void Start()
        {
            root = GetComponent<UIDocument>().rootVisualElement;

            // Ищем правильные элементы по именам из UXML
            leftColumn = root.Q<VisualElement>("left-column");  // Теперь ищем left-column
            resizer = root.Q<VisualElement>("hierarchy-resizer");

            if (leftColumn == null)
            {
                Debug.LogError("Left column not found!");
                return;
            }

            if (resizer == null)
            {
                Debug.LogError("Resizer not found!");
                return;
            }

            InitEvents();

            // Устанавливаем начальную ширину
            leftColumn.style.minWidth = MIN_WIDTH;
            leftColumn.style.maxWidth = MAX_WIDTH;
        }

        private void InitEvents()
        {
            resizer.RegisterCallback<PointerDownEvent>(OnPointerDown);
            resizer.RegisterCallback<PointerUpEvent>(OnPointerUp);
            resizer.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp);
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            isResizing = true;
            startWidth = leftColumn.resolvedStyle.width;
            startMouseX = evt.position.x;

            resizer.CapturePointer(evt.pointerId);
            resizer.focusable = true;
            resizer.Focus();

            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isResizing) return;

            float deltaX = evt.position.x - startMouseX;
            float newWidth = Mathf.Clamp(startWidth + deltaX, MIN_WIDTH, MAX_WIDTH);

            // Меняем ширину левой колонки
            leftColumn.style.width = newWidth;
            leftColumn.style.minWidth = newWidth;
            leftColumn.style.maxWidth = newWidth;

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!isResizing) return;

            isResizing = false;

            if (resizer.HasPointerCapture(evt.pointerId))
            {
                resizer.ReleasePointer(evt.pointerId);
            }

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
