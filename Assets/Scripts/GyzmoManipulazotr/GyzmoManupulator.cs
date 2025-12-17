using System;
using UnityEngine;

public enum AxisMode
{
    Local,
    Global
}

public class GyzmoManupulator : MonoBehaviour
{
    public Transform Target { get; private set; }
    public IManipulatorMode CurrentManipulatorMode { get; private set; }
    public AxisMode CurrentAxisMode { get; private set; }

    public GameObject moveHandlesGroup;
    public GameObject rotateHandlesGroup;

    public Camera cam;
    public float gizmoScaleKoeficient = 0.1f;

    public Transform gizmoRoot;   // ПУСТЫШКА!
    [HideInInspector] public Quaternion gizmoRootStartRotation; // для глобального режима
    public event Action<Transform> OnTargetTransformChanged;
    public event Action<Transform> OnDragStart;
    public event Action<Transform> OnDragEnd;

    public void NotifyTransformChanged()
    {
        if (Target != null)
            OnTargetTransformChanged?.Invoke(Target);
    }
    public void NotifyStartDrag()
    {
        if (Target != null)
            OnDragStart?.Invoke(Target);
    }
    public void NotifyDragEnd()
    {
        if (Target != null)
            OnDragEnd?.Invoke(Target);
    }
    private void Awake()
    {
        // создаем пустышку
        gizmoRoot = new GameObject("GizmoRoot").transform;
        gizmoRoot.SetParent(transform, false);

        // переносим группы в пустышку
        moveHandlesGroup.transform.SetParent(gizmoRoot, true);
        rotateHandlesGroup.transform.SetParent(gizmoRoot, true);
    }

    private void Start()
    {
        SetManipulatorMode(new MoveMode());
        SetAxisMode(AxisMode.Global);
    }

    private void Update()
    {
        if (Target != null)
            gizmoRoot.position = Target.position;

        float dist = Vector3.Distance(cam.transform.position, gizmoRoot.position);
        gizmoRoot.localScale = Vector3.one * dist * gizmoScaleKoeficient;

        UpdateHandlesOrientation();
    }

    public void Attach(Transform t)
    {
        Target = t;
        gizmoRoot.position = t.position;

        CurrentManipulatorMode?.OnObjectSelected(Target, this);
    }

    public void Detach()
    {
        Target = null;
        gameObject.SetActive(false);
    }

    public void SetManipulatorMode(IManipulatorMode mode)
    {
        CurrentManipulatorMode = mode;

        moveHandlesGroup.SetActive(mode is MoveMode);
        rotateHandlesGroup.SetActive(mode is RotateMode);

        if (Target != null)
            mode.OnObjectSelected(Target, this);
    }

    public void SetAxisMode(AxisMode mode)
    {
        if (CurrentAxisMode == mode) return;

        CurrentAxisMode = mode;

        if (Target == null) return;

        if (mode == AxisMode.Local)
        {
            // локальные оси сразу повторяют объект
            gizmoRoot.rotation = Target.rotation;
        }
        else
        {
            // глобальные оси: сразу выставляем мировую ориентацию
            gizmoRoot.rotation = Quaternion.identity;
            gizmoRootStartRotation = gizmoRoot.rotation; // сохраняем стартовую мировую ориентацию для RotateMode
        }
    }

    private void UpdateHandlesOrientation()
    {
        if (Target == null) return;

        if (CurrentAxisMode == AxisMode.Local)
        {
            gizmoRoot.rotation = Target.rotation;
            // привязка moveHandles и plane к локальной системе объекта
            moveHandlesGroup.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // глобальный: сохраняем начальное вращение и не сбрасываем каждую итерацию
            // только если ещё не сохранено
            if (gizmoRootStartRotation == Quaternion.identity)
                gizmoRootStartRotation = gizmoRoot.rotation;

            //gizmoRoot.rotation = gizmoRootStartRotation;

            // привязка moveHandles и plane к глобальной системе
            moveHandlesGroup.transform.rotation = Quaternion.identity;
            // не трогаем rotation, оставляем его под контролем RotateMode
        }
    }
}
