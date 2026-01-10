using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.SystemManager;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

public class ObjectPicker : MonoBehaviour
{
    public GyzmoManupulator manipulator;
    private AxisHandle currentHandle;
    //[SerializeField] private UIBlocker uIBlocker;
    //public PropertiesPanelEvents propertiesPanel;
    private UIStatusManager _uiStatusManager;
    private IPropertyProvider currentProvider;
    private Vector3 startPos;
    private Vector3 startRot;
    private EventBus _eventBus;
    private SceneObjectsManager _sceneObjectsManager;
    private UndoRedoManager _undoRedoManager;
    private SceneManipulatorModeManager _manipulatorModeManager;
    //public bool IsDraggingManipulator => currentHandle != null;
    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<PickObjectSignal>(OnPickObject);
        _eventBus.Subscribe<UnpickObjectSignal>(OnUnpickObject);
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

    private void OnUnpickObject(UnpickObjectSignal signal)
    {
        UnpickObject();
    }

    private void OnPickObject(PickObjectSignal signal)
    {
        PickObject(signal.Object.Reference);
       
    }

    private void OnSetManipulatorMode(SetGyzmoManipulatorModeSignal signal)
    {

    }

    private void Manipulator_OnDragStart(Transform obj)
    {
        startPos = obj.position;
        startRot = obj.eulerAngles;
    }

    private void Manipulator_OnDragEnd(Transform obj)
    {
        if (transform == null) return;
        Vector3 endPos = obj.position;
        Vector3 endRot = obj.eulerAngles;
        if (currentProvider != null)
        {
            if (startPos != endPos)
            {
                _undoRedoManager.Execute(
                    new PropertyChangeCommand(currentProvider, nameof(IPropertyProvider.Position), startPos, endPos)
                );
            }

            if (startRot != endRot)
            {
                _undoRedoManager.Execute(
                    new PropertyChangeCommand(currentProvider, nameof(IPropertyProvider.Rotation), startRot, endRot)
                );
            }
        }

    }

    private void HandleTransformChanged(Transform transform)
    {
        if (transform == null) return;

        if (currentProvider != null)
            _eventBus.Invoke(new PropertiesTransformUpdateSignal());
        //propertiesPanel.UpdateTransform(currentProvider);
    }

    private void Update()
    {
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
        if (manipulator.CameraModeActive)
        {
            if (currentHandle != null) UnpickObject();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int manipLayerMask = LayerMask.GetMask("Manipulator");

        if (Input.GetMouseButtonDown(0) && !_uiStatusManager.isPointerOverUI)
        {
            // Клик по манипулятору
            if (Physics.Raycast(ray, out RaycastHit hitHandle, Mathf.Infinity, manipLayerMask))
            {
                //Debug.Log(hitHandle.transform.gameObject.name);
                AxisHandle handle = hitHandle.collider.GetComponent<AxisHandle>();
                if (handle != null)
                {
                    currentHandle = handle;
                    currentHandle.StartDrag();
                }
            }
            // Клик по объекту сцены
            else
            {
                // ИСПРАВЛЕНИЕ: Используем RaycastAll вместо Raycast
                RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

                if (hits.Length > 0)
                {
                    // Сортируем по расстоянию (от ближнего к дальнему)
                    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                    // Ищем первый объект с IPropertyProvider
                    foreach (RaycastHit hit in hits)
                    {
                        // Проверяем, есть ли IPropertyProvider на этом объекте
                        IPropertyProvider provider = hit.collider.GetComponentInParent<IPropertyProvider>();
                        if (provider != null)
                        {
                            // Получаем Transform с провайдером
                            Transform providerTransform = (provider as MonoBehaviour)?.transform;

                            if (providerTransform != null)
                            {
                                currentProvider = provider;
                                //var a = _sceneObjectsManager.GetById(currentProvider.Id);
                                _eventBus.Invoke(new SelectObjectInScene(currentProvider.Id));
                                _eventBus.Invoke(new ChangePropertiesProviderSignal(provider));
                                //propertiesPanel.ShowPanel();
                                //propertiesPanel.ShowProperties(provider);
                                PickObject(providerTransform.gameObject);
                                break; // Выходим после нахождения первого подходящего объекта
                            }
                        }
                    }
                }
                else
                {
                    if(!_uiStatusManager.isPointerOverUI)
                        manipulator.Detach();
                }
            }
        }

        if (Input.GetMouseButton(0) && currentHandle != null)
            currentHandle.UpdateDrag();

        // Отпускание
        if (Input.GetMouseButtonUp(0) && currentHandle != null)
        {
            currentHandle.EndDrag();
            currentHandle = null;
        }
    }

    public void PickObject(GameObject gameObject)
    {
        manipulator.gameObject.SetActive(true);
        IPropertyProvider provider = null;
        GameObject target = gameObject;

        // Ищем провайдер на самом объекте
        if (!target.TryGetComponent<IPropertyProvider>(out provider))
        {
            Debug.LogWarning($"На объекте {target.name} нет IPropertyProvider");
            return;
        }
        var obj = _sceneObjectsManager.GetById(provider.Id);
        if (obj != null)
        {
            if (obj.Type == ObjectType.LinearMoveCommand)
            {
                _eventBus.Invoke(new PickCommandSignal(obj));
            }
        }
        currentProvider = provider;
        manipulator.Attach(gameObject.transform);
       
        
    }
    public void UnpickObject()
    {
        manipulator.Detach();
    }

    public IManipulatorMode GetManipulatorMode()
    {
        return manipulator.CurrentManipulatorMode;
    }
}


