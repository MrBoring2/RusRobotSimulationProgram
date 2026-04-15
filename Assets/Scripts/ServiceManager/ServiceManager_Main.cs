using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.Managers;

namespace Assets.Scripts.CustomServiceManager
{
    public class ServiceManager_Main : MonoBehaviour
    {
        [SerializeField] private SceneObjectsManager _sceneObjectManager;
        [SerializeField] private LineManager _lineManager;
        [SerializeField] private UIStatusManager _uiStatusManager;
        [SerializeField] private SceneManipulatorModeManager _sceneManipulatorModeManager;
        [SerializeField] private AxisModeManager _axisModeManager;
        [SerializeField] private UndoRedoManager _undoRedoManager;
        [SerializeField] private SaveLoadManager _saveLoadManager;
        [SerializeField] private NotificationSystemManager _notificationSystemManager;
        private CustomEventBus.EventBus _eventBus;
        [SerializeField] private SimulationManager _simulationManager;
        [SerializeField] private LogicSignalBus _logicSignalBus;
        private void Awake()
        {
            _eventBus = new CustomEventBus.EventBus();

            RegisterServices();
            Init();
        }

        private void RegisterServices()
        {
            ServiceManager.Initialize();
            ServiceManager.Current.Register(_eventBus);
            ServiceManager.Current.Register(_sceneObjectManager);
            ServiceManager.Current.Register(_uiStatusManager);
            ServiceManager.Current.Register(_lineManager);
            ServiceManager.Current.Register(_sceneManipulatorModeManager);
            ServiceManager.Current.Register(_axisModeManager);
            ServiceManager.Current.Register(_undoRedoManager);
            ServiceManager.Current.Register(_saveLoadManager);
            ServiceManager.Current.Register(_simulationManager);
            ServiceManager.Current.Register(_notificationSystemManager);
            ServiceManager.Current.Register(_logicSignalBus);
        }

        private void Init()
        {
            _uiStatusManager.Init();
            _lineManager.Init();
            _sceneObjectManager.Init();
            _sceneManipulatorModeManager.Init();
            _axisModeManager.Init();
            _undoRedoManager.Init();
            _saveLoadManager.Init();
            _notificationSystemManager.Init();
            _logicSignalBus.Init();
        }

    }
}
