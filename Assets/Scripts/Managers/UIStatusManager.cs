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
        public bool IsPropertiesPanelVisible { get; private set; } = false;
        public bool IsObjectsListVisible { get; private set; } = true;
        public bool IsCommandsListVisible { get; private set; } = false;
        public bool isPointerOverUI { get; private set; }
        public bool isInputMode { get; private set; }
        private EventBus _eventBus;
        public void Init()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            isInputMode = false;
            isPointerOverUI = false;
        }

        public void SetInputMode(bool isInputMode)
        {
            this.isInputMode = isInputMode;
        }
        public void SetPointerOberUI(bool isPointerOverUI)
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
            IsObjectsListVisible = !IsObjectsListVisible;
            _eventBus.Invoke(new ToggleCommandsListSignal());
        }
    }
}
