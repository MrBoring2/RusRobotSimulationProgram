using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.HierarhyPanel;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.Managers
{
    public class UIStatusManager : MonoBehaviour, IService
    {
        [SerializeField] private UIDocument uiDocument;
        private VisualElement root;
        private List<string> uiElements = new List<string>();
        public bool IsPropertiesPanelVisible { get; private set; } = false;
        public bool IsObjectsListVisible { get; private set; } = true;
        public bool IsCommandsListVisible { get; private set; } = false;
        public bool isPointerOverUI { get; private set; }
        public bool isInputMode { get; private set; }
        private EventBus _eventBus;
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            root = uiDocument.rootVisualElement;
            isInputMode = false;
            isPointerOverUI = false;
            uiElements = new List<string>
        {
            "menu-bar-container",
            "main-menu-container",
            "left-column",
            "properties-container",
            "divider",
            "hierarchy-resizer",
            "FileMenu",
            "ViewMenu",
            "context-menu",
            "perspective-panel-container",
            "notification-container",
            "modal-window-container"
        };
        }
        public bool CheckIsOnUI()
        {
            Vector2 screenPos = new Vector2(Input.mousePosition.x, UnityEngine.Screen.height - Input.mousePosition.y);
            var panel = root.panel;
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenPos);
            VisualElement picked = panel.Pick(panelPos);

            if (picked != null)
            {
                // Проверяем является ли элемент или его родитель одной из UI панелей
                VisualElement current = picked;
                while (current != null)
                {
                    if (uiElements.Contains(current.name) || current.ClassListContains("unity-base-dropdown"))
                    {
                        return true;
                    }
                    current = current.parent;
                }
            }
            return false;
        }

        public void SetInputMode(bool isInputMode)
        {
            this.isInputMode = isInputMode;
        }
        public void SetPointerOverUI(bool isPointerOverUI)
        {
            if (isPointerOverUI == false)
            {

            }
            this.isPointerOverUI = isPointerOverUI;
        }

        public void SetPropertiesPanelVisibility(bool visible)
        {
            IsPropertiesPanelVisible = visible;
            _eventBus.Invoke(new TogglePropertiesSignal());

        }


        public void SetObjectsListPanelVisibility(bool visible)
        {
            IsObjectsListVisible = visible;
            _eventBus.Invoke(new ToggleObjectsListSignal());
        }

        public void SetCommandsListPanelVisibility(bool visible)
        {
            IsCommandsListVisible = visible;
            _eventBus.Invoke(new ToggleCommandsListSignal());
        }

        public void TogglePropertiesPanel()
        {
            IsPropertiesPanelVisible = !IsPropertiesPanelVisible;
            _eventBus.Invoke(new TogglePropertiesSignal());
        }

        public void ToggleObjectsListPanel()
        {
            IsObjectsListVisible = !IsObjectsListVisible;
            _eventBus.Invoke(new ToggleObjectsListSignal());
        }

        public void ToggleCommandsListPanel()
        {
            IsCommandsListVisible = !IsCommandsListVisible;
            _eventBus.Invoke(new ToggleCommandsListSignal());
        }
    }
}
