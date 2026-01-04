using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.AxisModes;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
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
    public AxisMode? CurrentAxisMode => _axisModeManager?.Mode;
    public bool CameraModeActive { get; private set; } = false;
    public GameObject moveHandlesGroup;
    public GameObject rotateHandlesGroup;

    public Camera cam;
    public float gizmoScaleKoeficient = 0.1f;

    public Transform gizmoRoot;   // ПУСТЫШКА!
    [HideInInspector] public Quaternion gizmoRootStartRotation; // для глобального режима
    public event Action<Transform> OnTargetTransformChanged;
    public event Action<Transform> OnDragStart;
    public event Action<Transform> OnDragEnd;
    private EventBus _eventBus;
    private AxisModeManager _axisModeManager;

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
        gizmoRoot = new GameObject("GizmoRoot").transform;
        gizmoRoot.SetParent(transform, true);

        moveHandlesGroup.transform.SetParent(gizmoRoot, true);
        rotateHandlesGroup.transform.SetParent(gizmoRoot, true);
    }

    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<SetGyzmoManipulatorModeSignal>(OnSetManipulatorMode);
        _eventBus.Subscribe<SetAxisModeSignal>(OnSetAxisMode);
        _axisModeManager = ServiceManager.Current.Get<AxisModeManager>();
        
        SetManipulatorMode(new MoveMode());
        //SetAxisMode(AxisMode.Global);
        _axisModeManager.SetAxisMode(AxisMode.Global);
    }

    private void OnSetAxisMode(SetAxisModeSignal signal)
    {
        SetAxisMode(signal.Mode);
    }

    private void OnSetManipulatorMode(SetGyzmoManipulatorModeSignal signal)
    {
        SetManipulatorMode(signal.Mode);
        //CurrentManipulatorMode = ;
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


    public void SetManipulatorModeCamera()
    {
        SetManipulatorMode(null);
        CameraModeActive = true;
    }
    public void SetManipulatorMode(IManipulatorMode mode)
    {
        CameraModeActive = false;
        CurrentManipulatorMode = mode;
        if (mode == null) return;

        moveHandlesGroup.SetActive(mode is MoveMode);
        rotateHandlesGroup.SetActive(mode is RotateMode);

        if (Target != null)
            mode.OnObjectSelected(Target, this);
    }

    public void SetAxisMode(AxisMode mode)
    {
        //if (_axisModeManager.Mode == mode) return;
        //if (CurrentAxisMode == mode) return;

        //CurrentAxisMode = mode;

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

        //if (CurrentAxisMode == AxisMode.Local)
        if (_axisModeManager.Mode == AxisMode.Local)
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
