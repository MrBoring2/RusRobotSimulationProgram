using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
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
    [SerializeField]
    private UIBlocker uIBlocker;
    public PropertiesPanelEvents propertiesPanel;
    private IPropertyProvider currentProvider;
    private Vector3 startPos;
    private Vector3 startRot;
    private EventBus _eventBus;
    private SceneObjectsManager _sceneObjectsManager;
    public bool IsDraggingManipulator => currentHandle != null;
    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
        if (manipulator != null)
        {
            manipulator.gameObject.SetActive(false);
            manipulator.OnTargetTransformChanged += HandleTransformChanged;
            manipulator.OnDragEnd += Manipulator_OnDragEnd;
            manipulator.OnDragStart += Manipulator_OnDragStart;
        }
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
                UndoRedoSystem.Instance.Execute(
                    new PropertyChangeCommand(currentProvider, nameof(IPropertyProvider.Position), startPos, endPos)
                );
            }

            if (startRot != endRot)
            {
                UndoRedoSystem.Instance.Execute(
                    new PropertyChangeCommand(currentProvider, nameof(IPropertyProvider.Rotation), startRot, endRot)
                );
            }
        }

    }

    private void HandleTransformChanged(Transform transform)
    {
        if (transform == null) return;

        if (currentProvider != null)
            propertiesPanel.UpdateTransform(currentProvider);
    }

    private void Update()
    {
        if (manipulator.CameraModeActive)
        {
            if (currentHandle != null) UnpickObject();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int manipLayerMask = LayerMask.GetMask("Manipulator");

        if (Input.GetMouseButtonDown(0) && !uIBlocker.isPointerOverUI)
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

                                propertiesPanel.ShowPanel();
                                propertiesPanel.ShowProperties(provider);
                                PickObject(providerTransform.gameObject);
                                break; // Выходим после нахождения первого подходящего объекта
                            }
                        }
                    }
                }
                else
                {
                    manipulator.gameObject.SetActive(false);

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
        IPropertyProvider provider = null;
        GameObject target = gameObject;

        // Ищем провайдер на самом объекте
        if (!target.TryGetComponent<IPropertyProvider>(out provider))
        {
            Debug.LogWarning($"На объекте {target.name} нет IPropertyProvider");
            return;
        }
        manipulator.Attach(gameObject.transform);
        currentProvider = gameObject.GetComponent<IPropertyProvider>();
        manipulator.gameObject.SetActive(true);
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


