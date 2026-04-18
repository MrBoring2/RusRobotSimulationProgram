using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.AxisModes;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public enum AxisMode
{
    Local,
    Global
}

public class GyzmoManupulator : MonoBehaviour
{
    public TextMeshPro angleTextPrefab;
    private Transform _targer;
    // public Transform Target { get; private set; }
    public Transform Target
    { 
        get => _targer;
        private set
        {
            _targer = value;
        }
    }
    public IManipulatorMode CurrentManipulatorMode { get; private set; }
    public AxisMode? CurrentAxisMode => _axisModeManager?.Mode;
    public bool CameraModeActive { get; private set; } = false;
    public GameObject moveHandlesGroup;
    public GameObject rotateHandlesGroup;
    private SceneManipulatorModeManager _manipulatorModeManager;

    public Camera cam;
    public float gizmoScaleKoeficient = 0.1f;

    public Transform gizmoRoot;
    [HideInInspector] public Quaternion gizmoRootStartRotation;
    public event Action<Transform> OnTargetTransformChanged;
    public event Action<Transform> OnDragStart;
    public event Action<Transform> OnDragEnd;
    private EventBus _eventBus;
    private AxisModeManager _axisModeManager;
    public SceneManipulatorMode CurrentSceneMode => _manipulatorModeManager.Mode;

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
        _manipulatorModeManager = ServiceManager.Current.Get<SceneManipulatorModeManager>();
        SetManipulatorMode(_manipulatorModeManager.Mode);
        if (cam == null)
        {
            cam = Camera.main;
        }
        SetAxisMode(AxisMode.Global);
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
    private bool IsNode(Transform t)
    {
        if (t.TryGetComponent<IPropertyProvider>(out var provider))
        {
            var obj = ServiceManager.Current
                .Get<SceneObjectsManager>()
                .GetById(provider.Id);

            return obj != null && obj.Type == ObjectType.Node;
        }
        return false;
    }
    private void Update()
    {
        if (Target != null)
        {
            if (IsNode(Target))
                gizmoRoot.position = CalculateGeometricCenter(Target);
            else
                gizmoRoot.position = Target.position;
        }

        float dist = Vector3.Distance(cam.transform.position, gizmoRoot.position);
        if (dist > 3)
        {
            gizmoRoot.localScale = Vector3.one * dist * gizmoScaleKoeficient;
            angleTextPrefab.gameObject.transform.localScale = Vector3.one * dist * gizmoScaleKoeficient;
        }
        else
        {
            gizmoRoot.localScale = Vector3.one * 3 * gizmoScaleKoeficient;
            angleTextPrefab.gameObject.transform.localScale = Vector3.one * 3 * gizmoScaleKoeficient;
        }
        UpdateHandlesOrientation();
    }

    public void Attach(Transform t)
    {
        Target = t;
        gizmoRoot.position = t.position;

        CurrentManipulatorMode?.OnObjectSelected(Target, this);
    }
    public void AttachNode(Transform nodeRoot)
    {
        Target = nodeRoot;

        Vector3 center = CalculateGeometricCenter(nodeRoot);
        gizmoRoot.position = center;

        CurrentManipulatorMode?.OnObjectSelected(Target, this);
    }
    public Vector3 CalculateGeometricCenter(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return root.position;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    public void Detach()
    {
        Target = null;
        gameObject.SetActive(false);
    }


    public void SetManipulatorModeCamera()
    {
        SetManipulatorMode(SceneManipulatorMode.Drag);
        CameraModeActive = true;
    }
    public void SetManipulatorMode(SceneManipulatorMode mode)
    {
        IManipulatorMode manipulatorMode = null;
        switch (mode)
        {
            case SceneManipulatorMode.Drag:
                manipulatorMode = null;
                break;
            case SceneManipulatorMode.Move:
                manipulatorMode = new MoveMode();
                break;
            case SceneManipulatorMode.Rotation:
                var rotateMode = new RotateMode();
                rotateMode.cursorAngleText = angleTextPrefab; // angleTextPrefab — это уже TextMeshProUGUI на Canvas
                rotateMode.cursorAngleText.gameObject.SetActive(false);
                manipulatorMode = rotateMode;
                break;
            case SceneManipulatorMode.JOG:
                manipulatorMode = null;
                break;
            default:
                break;
        }
        CameraModeActive = false;
        CurrentManipulatorMode = manipulatorMode;
        if (manipulatorMode == null) return;

        moveHandlesGroup.SetActive(manipulatorMode is MoveMode);
        rotateHandlesGroup.SetActive(manipulatorMode is RotateMode);

        if (Target != null)
            manipulatorMode.OnObjectSelected(Target, this);
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
