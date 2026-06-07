using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomEventBus.Signals.UndoRedoSystem;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Компонент для выбора объектов на сцене с помощью мыши.
/// Отвечает за пикинг объектов, управление манипулятором (гиджмо) и обработку пользовательского ввода.
/// Интегрируется с системами Undo/Redo и PropertyProvider.
/// </summary>
public class ObjectPicker : MonoBehaviour
{
    public UIDocument root;
    /// <summary>
    /// Гизмо-манипулятор для перемещения/вращения объектов.
    /// </summary>
    public GizmoManupulator manipulator;
    /// <summary>
    /// Текущая активная рукоятка манипулятора (ось, за которую тянет пользователь).
    /// </summary>
    private AxisHandle currentHandle;
    private UIStatusManager _uiStatusManager;
    /// <summary>
    /// Провайдер свойств текущего выбранного объекта.
    /// </summary>
    private IPropertyProvider currentProvider;
    private Vector3 startPos;
    private Vector3 startRot;
    private EventBus _eventBus;
    private SceneObjectsManager _sceneObjectsManager;
    private UndoRedoManager _undoRedoManager;
    private SceneManipulatorModeManager _manipulatorModeManager;

    /// <summary>
    /// Инициализация компонента при старте.
    /// Подписывается на события, получает необходимые сервисы, настраивает манипулятор.
    /// </summary>
    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<PickObjectSignal>(OnPickObject);
        _eventBus.Subscribe<UnpickObjectSignal>(OnUnpickObject);
        _eventBus.Subscribe<ExecuteCommandSignal>(OnExecuteCommand);
        _eventBus.Subscribe<UndoneCommandSignal>(OnUndoneCommand);
        _uiStatusManager = ServiceManager.Current.Get<UIStatusManager>();
        _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
        _manipulatorModeManager = ServiceManager.Current.Get<SceneManipulatorModeManager>();
        if (manipulator != null)
        {
            manipulator.gameObject.SetActive(false);
            manipulator.OnTargetTransformChanged += HandleTransformChanged;
            manipulator.OnDragEnd += Manipulator_OnDragEnd;
            manipulator.OnDragStart += Manipulator_OnDragStart;
        }
    }
    /// <summary>
    /// Обработка ввода пользователя и выбор объектов каждый кадр.
    /// </summary>
    private void Update()
    {
        if (manipulator == null) return;
        // В режиме JOG манипулятор полностью отключается
        if (_manipulatorModeManager.Mode == SceneManipulatorMode.JOG)
        {
            manipulator.gameObject.SetActive(false);
            manipulator.Detach();
            return;
        }
        else
        {
            if (manipulator.Target != null)
                manipulator.gameObject.SetActive(true);
        }

        // В режиме камеры нельзя взаимодействовать с объектами
        if (manipulator.CameraModeActive)
        {
            if (currentHandle != null) UnpickObject();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int manipLayerMask = LayerMask.GetMask("Manipulator");

        // Нажатие левой кнопки мыши
        if (Input.GetMouseButtonDown(0) && !_uiStatusManager.CheckIsOnUI())
        {
            // Попытка захватить рукоятку манипулятора
            if (Physics.Raycast(ray, out RaycastHit hitHandle, Mathf.Infinity, manipLayerMask))
            {
                AxisHandle handle = hitHandle.collider.GetComponent<AxisHandle>();
                if (handle != null)
                {
                    currentHandle = handle;
                    currentHandle.StartDrag();
                }
            }
            else
            {
                // Если не рукоятка - пытаемся выбрать объект на сцене
                RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

                if (hits.Length > 0)
                {
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    foreach (RaycastHit hit in hits)
                    {
                        // Ищем компонент IPropertyProvider на объекте или его родителе
                        IPropertyProvider provider = hit.collider.GetComponentInParent<IPropertyProvider>();
                        if (provider != null)
                        {
                            Transform providerTransform = (provider as MonoBehaviour)?.transform;

                            if (providerTransform != null)
                            {
                                currentProvider = provider;
                                _eventBus.Invoke(new SelectObjectInScene(currentProvider.Id));
                                _eventBus.Invoke(new ChangePropertiesProviderSignal(provider));
                                PickObject(providerTransform.gameObject);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Если кликнули в пустоту - снимаем выделение
                    if (!_uiStatusManager.isPointerOverUI)
                        manipulator.Detach();
                }
            }
        }

        // Обновление позиции при зажатой кнопке
        if (Input.GetMouseButton(0) && currentHandle != null)
            currentHandle.UpdateDrag();

        // Отпускание рукоятки
        if (Input.GetMouseButtonUp(0) && currentHandle != null)
        {
            currentHandle.EndDrag();
            currentHandle = null;
        }
    }

    /// <summary>
    /// Обработчик выполнения команды.
    /// Если команда деструктивная (удаление объекта) - снимаем выделение.
    /// </summary>
    /// <param name="signal">Сигнал о выполнении команды</param>
    private void OnExecuteCommand(ExecuteCommandSignal signal)
    {
        if (signal.Command is IDestructiveCommand)
        {
            UnpickObject();
        }
    }

    /// <summary>
    /// Обработчик отмены команды.
    /// Если команда деструктивная - снимаем выделение.
    /// </summary>
    /// <param name="signal">Сигнал об отмене команды</param>
    private void OnUndoneCommand(UndoneCommandSignal signal)
    {
        if (signal.Command is IDestructiveCommand)
        {
            UnpickObject();
        }
    }

    /// <summary>
    /// Обработчик сигнала снятия выделения.
    /// </summary>
    /// <param name="signal">Сигнал снятия выделения</param>
    private void OnUnpickObject(UnpickObjectSignal signal)
    {
        UnpickObject();
    }

    /// <summary>
    /// Обработчик сигнала выбора объекта.
    /// </summary>
    /// <param name="signal">Сигнал с данными об объекте</param>
    private void OnPickObject(PickObjectSignal signal)
    {
        PickObject(signal.Object.Reference);
    }

    /// <summary>
    /// Обработчик начала перетаскивания объекта.
    /// Сохраняет начальные позицию и поворот для Undo/Redo.
    /// </summary>
    /// <param name="obj">Transform перемещаемого объекта</param>
    private void Manipulator_OnDragStart(Transform obj)
    {
        startPos = obj.localPosition;
        startRot = obj.eulerAngles;
    }

    /// <summary>
    /// Обработчик окончания перетаскивания объекта.
    /// Создает команды Undo/Redo для изменений позиции и поворота.
    /// </summary>
    /// <param name="obj">Transform перемещенного объекта</param>
    private void Manipulator_OnDragEnd(Transform obj)
    {
        Vector3 endPos = obj.localPosition;
        Vector3 endRot = obj.eulerAngles;

        if (currentProvider != null)
        {
            if (startPos != endPos)
            {
                _undoRedoManager.Execute(
                    new PropertyChangeCommand(
                        currentProvider,
                        nameof(IPropertyProvider.LocalPosition),
                        startPos,
                        endPos
                    )
                );
            }

            if (startRot != endRot)
            {
                _undoRedoManager.Execute(
                    new PropertyChangeCommand(
                        currentProvider,
                        nameof(IPropertyProvider.Rotation),
                        startRot,
                        endRot
                    )
                );
            }
        }
    }

    /// <summary>
    /// Обработчик изменения трансформации объекта.
    /// Уведомляет подписчиков об обновлении свойств объекта.
    /// </summary>
    /// <param name="transform">Transform измененного объекта</param>
    private void HandleTransformChanged(Transform transform)
    {
        if (transform == null) return;

        if (currentProvider != null)
            _eventBus.Invoke(new PropertiesTransformUpdateSignal());
    }

    /// <summary>
    /// Выбирает объект на сцене и активирует манипулятор.
    /// </summary>
    /// <param name="gameObject">GameObject для выбора</param>
    public void PickObject(GameObject gameObject)
    {
        manipulator.gameObject.SetActive(true);
        IPropertyProvider provider = null;
        GameObject target = gameObject;

        if (!target.TryGetComponent<IPropertyProvider>(out provider))
        {
            Debug.LogWarning($"На объекте {target.name} нет IPropertyProvider");
            return;
        }
        // Поиск SceneObject для определения типа
        SceneObject obj;
        var marker = gameObject.GetComponent<SceneObjectMarker>();
        if (marker.type == ObjectType.LinearMoveCommand)
        {
            obj = _sceneObjectsManager.Commands.FindElementById(provider.Id);
        }
        else
        {
            obj = _sceneObjectsManager.GetById(provider.Id);
        }
        // Если найден объект-команда - уведомляем о выборе команды
        if (obj != null)
        {
            if (obj.Type == ObjectType.LinearMoveCommand)
            {
                _eventBus.Invoke(new PickCommandSignal(obj));
            }
        }
        currentProvider = provider;
        // Прикрепляем манипулятор к объекту (с учетом типа объекта)
        if (obj.Type == ObjectType.Node || obj.Type == ObjectType.Work)
        {
            manipulator.AttachNodeOrWork(gameObject.transform);
        }
        else
        {
            manipulator.Attach(gameObject.transform);
        }
    }

    /// <summary>
    /// Снимает выделение с текущего объекта.
    /// </summary>
    public void UnpickObject()
    {
        manipulator.Detach();
    }

    /// <summary>
    /// Возвращает текущий режим манипулятора.
    /// </summary>
    /// <returns>Текущий режим (Move/Rotate)</returns>
    public IManipulatorMode GetManipulatorMode()
    {
        return manipulator.CurrentManipulatorMode;
    }
}