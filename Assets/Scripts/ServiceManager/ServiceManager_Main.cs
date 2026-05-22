using UnityEngine;
using Assets.Scripts.CustomEventBus;
using Assets.Scripts.Managers;

namespace Assets.Scripts.CustomServiceManager
{
    /// <summary>
    /// Главный менеджер сервисов для основной сцены.
    /// Отвечает за инициализацию, регистрацию и настройку всех сервисов приложения.
    /// Является MonoBehaviour-компонентом, который должен быть размещен на игровом объекте в сцене.
    /// </summary>
    public class ServiceManager_Main : MonoBehaviour
    {
        /// <summary>
        /// Экземпляры менеджеров и шины событий
        /// </summary>
        private EventBus _eventBus;
        [SerializeField] private SceneObjectsManager _sceneObjectManager;
        [SerializeField] private LineManager _lineManager;
        [SerializeField] private UIStatusManager _uiStatusManager;
        [SerializeField] private SceneManipulatorModeManager _sceneManipulatorModeManager;
        [SerializeField] private AxisModeManager _axisModeManager;
        [SerializeField] private UndoRedoManager _undoRedoManager;
        [SerializeField] private SaveLoadManager _saveLoadManager;
        [SerializeField] private NotificationSystemManager _notificationSystemManager;
        [SerializeField] private ModalWindowServiceManager _modalWindowServiceManager;
        [SerializeField] private SimulationManager _simulationManager;
        [SerializeField] private LogicSignalBus _logicSignalBus;

        /// <summary>
        /// Вызывается Unity при активации игрового объекта.
        /// Выполняется до метода Start, подходит для инициализации сервисов.
        /// </summary>
        private void Awake()
        {
            _eventBus = new EventBus();
            RegisterServices();
            Init();
        }

        /// <summary>
        /// Регистрирует все сервисы в глобальном ServiceManager.
        /// Порядок регистрации важен, так как сервисы могут зависеть друг от друга.
        /// </summary>
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
            ServiceManager.Current.Register(_modalWindowServiceManager);
            ServiceManager.Current.Register(_logicSignalBus);
        }

        /// <summary>
        /// Инициализирует все зарегистрированные сервисы.
        /// Вызывает метод Init() у каждого сервиса для их внутренней настройки.
        /// </summary>
        private void Init()
        {
            _eventBus.Init();
            _uiStatusManager.Init();
            _lineManager.Init();
            _sceneObjectManager.Init();
            _sceneManipulatorModeManager.Init();
            _axisModeManager.Init();
            _undoRedoManager.Init();
            _saveLoadManager.Init();
            _notificationSystemManager.Init();
            _modalWindowServiceManager.Init();
        }

    }
}
