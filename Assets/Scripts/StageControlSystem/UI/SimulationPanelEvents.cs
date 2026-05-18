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
        private SimulationManager _simulationManager;
        private Label _simulationLabel;
        public TooltipEvents tooltipEvents;
        private void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _simulationManager = ServiceManager.Current.Get<SimulationManager>();
            root = GetComponent<UIDocument>().rootVisualElement;
            _startSimulationButton = root.Q<Button>("start-simulation-button");
            _pauseSimulationButton = root.Q<Button>("pause-simulation-button");
            _simulationLabel = root.Q<Label>("simulation-timer");
            RegisterEvents();
            tooltipEvents.RegisterTooltip(_startSimulationButton, "Начать симуляцию");
            tooltipEvents.RegisterTooltip(_pauseSimulationButton, "Поставить симуляцию на паузу");
            SetStartIcon(false);
            _pauseSimulationButton.SetEnabled(false);
        }

        private void FixedUpdate()
        {
            var a = _simulationManager.GetTimeSimulation();
            _simulationLabel.text = $"{a.Hours:D2}:{a.Minute:D2}:{a.Seconds:D2}";
        }

        private void RegisterEvents()
        {
            _startSimulationButton.RegisterCallback<ClickEvent>(e =>
            {
                var a = _simulationManager.GetStatusSim();
                if (_simulationManager.GetStatusSim() == SIM_STAT.STOP)
                {
                   
                    _eventBus.Invoke(new StartSimulationSignal());
                    var b = _simulationManager.GetStatusSim();
                    if (_simulationManager.GetStatusSim() == SIM_STAT.PLAY)
                    {
                        tooltipEvents.UnregisterTooltip(_startSimulationButton);
                        tooltipEvents.RegisterTooltip(_startSimulationButton, "Остановить симуляцию");
                        tooltipEvents.ForceUpdateTooltip(_startSimulationButton);
                        SetStartIcon(true);
                        _pauseSimulationButton.SetEnabled(true);
                    }
                }
                else if (_simulationManager.GetStatusSim() == SIM_STAT.PLAY ||
                            _simulationManager.GetStatusSim() == SIM_STAT.PAUSE)
                {
                    _eventBus.Invoke(new StopSimulationSignal());
                    if (_simulationManager.GetStatusSim() == SIM_STAT.STOP)
                    {
                        tooltipEvents.UnregisterTooltip(_startSimulationButton);
                        tooltipEvents.RegisterTooltip(_startSimulationButton, "Начать симуляцию");
                        tooltipEvents.ForceUpdateTooltip(_startSimulationButton);
                        SetStartIcon(false);
                        _pauseSimulationButton.SetEnabled(false);
                    }
                }
            });
            _pauseSimulationButton.RegisterCallback<ClickEvent>(e =>
            {
                var a = _simulationManager.GetStatusSim();
                if (_simulationManager.GetStatusSim() == SIM_STAT.PLAY)
                {
                    _eventBus.Invoke(new PauseSimulationSignal());
                    _pauseSimulationButton.AddToClassList("active");
                }
                else
                {
                    _eventBus.Invoke(new StartSimulationSignal());
                    _pauseSimulationButton.RemoveFromClassList("active");
                }
            });
        }
        private void SetStartIcon(bool isRunning)
        {
            _startSimulationButton.RemoveFromClassList("start");
            _startSimulationButton.RemoveFromClassList("stop");
            _startSimulationButton.AddToClassList(isRunning ? "stop" : "start");
        }
    }

}
