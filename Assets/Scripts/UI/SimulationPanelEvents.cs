using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.UI
{
    public class SimulationPanelEvents : MonoBehaviour
    {
        private Button _startSimulationButton;
        private Button _pauseSimulationButton;
        private VisualElement root;
        private EventBus _eventBus;
        private void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            root = GetComponent<UIDocument>().rootVisualElement;
            _startSimulationButton = root.Q<Button>("start-simulation-button");
            _pauseSimulationButton = root.Q<Button>("pause-simulation-button");
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            _startSimulationButton.RegisterCallback<ClickEvent>(e =>
            {
                _eventBus.Invoke(new StartSimulationSignal());
            });
            _pauseSimulationButton.RegisterCallback<ClickEvent>(e =>
            {
                _eventBus.Invoke(new PauseSimulationSignal());
            });
        }
    }
}
