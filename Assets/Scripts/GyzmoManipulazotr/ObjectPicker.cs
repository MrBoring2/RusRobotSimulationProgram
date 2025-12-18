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
    private void Start()
    {
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
            //propertiesPanel.UpdateTransform(currentProvider);
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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int manipLayerMask = LayerMask.GetMask("Manipulator");
        //if (manipulator.Target == null) manipulator.Detach();
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
            else if (Physics.Raycast(ray, out RaycastHit hitObject))
            {

                var provider = hitObject.transform.gameObject.TryGetComponent<IPropertyProvider>(out IPropertyProvider d);
                if (d != null)
                {
                    currentProvider = d;
                    propertiesPanel.ShowPanel();
                    propertiesPanel.ShowProperties(d);
                }
                PickObject(hitObject.transform.gameObject);
            }
            // Клик по пустому месту сцены
            else
            {
                // Тут мы не проверяем UI вообще — манипулятор пропадёт только при клике на пустую сцену
                manipulator.gameObject.SetActive(false);
            }
        }

        // Перетаскивание манипулятора
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
        manipulator.Attach(gameObject.transform);
        currentProvider = gameObject.GetComponent<IPropertyProvider>();
        manipulator.gameObject.SetActive(true);
    }
    public void UnpickObject()
    {
        manipulator.Detach();
    }
}


