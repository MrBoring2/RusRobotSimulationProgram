using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.AxisModes;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Режим системы координат для гиджмо (манипулятора).
/// Local - локальные оси выбранного объекта.
/// Global - глобальные оси мира.
/// </summary>
public enum AxisMode
{
    Local,
    Global
}

/// <summary>
/// Главный класс манипулятора (гизмо) для перемещения/вращения объектов в 3D пространстве.
/// Реализует визуальные контроллеры (стрелки, кольца) для трансформации объектов.
/// Поддерживает разные режимы: Move, Rotate, Drag.
/// </summary>
public class GizmoManupulator : MonoBehaviour
{
    /// <summary>
    /// Префаб текстового поля для отображения угла поворота (при вращении).
    /// </summary>
    public TextMeshPro angleTextPrefab;
    /// <summary>
    /// Текущий целевой объект, которым управляет манипулятор.
    /// </summary>
    private Transform _targer;
    public Transform Target
    { 
        get => _targer;
        private set
        {
            _targer = value;
        }
    }
    /// <summary>
    /// Текущий режим манипулятора (Move/Rotate).
    /// </summary>
    public IManipulatorMode CurrentManipulatorMode { get; private set; }
    /// <summary>
    /// Текущий режим системы координат (Local/Global).
    /// </summary>
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

    /// <summary>
    /// Уведомляет подписчиков об изменении трансформации целевого объекта.
    /// </summary>
    public void NotifyTransformChanged()
    {
        if (Target != null)
            OnTargetTransformChanged?.Invoke(Target);
    }

    /// <summary>
    /// Уведомляет подписчиков о начале перетаскивания гиджмо.
    /// </summary>
    public void NotifyStartDrag()
    {
        if (Target != null)
            OnDragStart?.Invoke(Target);
    }

    /// <summary>
    /// Уведомляет подписчиков об окончании перетаскивания гиджмо.
    /// </summary>
    public void NotifyDragEnd()
    {
        if (Target != null)
            OnDragEnd?.Invoke(Target);
    }

    /// <summary>
    /// Вызывается при создании компонента.
    /// Создает корневой объект для гиджмо и настраивает иерархию.
    /// </summary>
    private void Awake()
    {
        gizmoRoot = new GameObject("GizmoRoot").transform;
        gizmoRoot.SetParent(transform, true);

        moveHandlesGroup.transform.SetParent(gizmoRoot, true);
        rotateHandlesGroup.transform.SetParent(gizmoRoot, true);
    }

    /// <summary>
    /// Вызывается при старте компонента.
    /// Инициализирует сервисы, подписывается на события и настраивает начальное состояние.
    /// </summary>
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

    /// <summary>
    /// Вызывается каждый кадр.
    /// Обновляет позицию и масштаб гиджмо в зависимости от целевого объекта.
    /// </summary>
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

    /// <summary>
    /// Обработчик сигнала смены режима осей.
    /// </summary>
    /// <param name="signal">Сигнал с новым режимом</param>
    private void OnSetAxisMode(SetAxisModeSignal signal)
    {
        SetAxisMode(signal.Mode);
    }

    /// <summary>
    /// Обработчик сигнала смены режима манипулятора.
    /// </summary>
    /// <param name="signal">Сигнал с новым режимом</param>
    private void OnSetManipulatorMode(SetGyzmoManipulatorModeSignal signal)
    {
        SetManipulatorMode(signal.Mode);
    }

    /// <summary>
    /// Проверяет, является ли объект узлом (Node).
    /// Узлы имеют особое поведение - гизмо центрируется по геометрическому центру.
    /// </summary>
    /// <param name="t">Transform проверяемого объекта</param>
    /// <returns>True, если объект является узлом</returns>
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

    /// <summary>
    /// Прикрепляет манипулятор к указанному объекту.
    /// </summary>
    /// <param name="t">Transform целевого объекта</param>
    public void Attach(Transform t)
    {
        Target = t;
        gizmoRoot.position = t.position;

        CurrentManipulatorMode?.OnObjectSelected(Target, this);
    }

    /// <summary>
    /// Прикрепляет манипулятор к узлу с центрированием по геометрическому центру.
    /// </summary>
    /// <param name="nodeRoot">Корневой Transform узла</param>
    public void AttachNode(Transform nodeRoot)
    {
        Target = nodeRoot;

        Vector3 center = CalculateGeometricCenter(nodeRoot);
        gizmoRoot.position = center;

        CurrentManipulatorMode?.OnObjectSelected(Target, this);
    }

    /// <summary>
    /// Вычисляет геометрический центр объекта на основе всех его Renderer'ов.
    /// </summary>
    /// <param name="root">Корневой объект</param>
    /// <returns>Центр ограничивающего бокса всех визуальных компонентов</returns>
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

    /// <summary>
    /// Открепляет манипулятор от текущего объекта.
    /// </summary>
    public void Detach()
    {
        Target = null;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Устанавливает режим манипулятора в режим камеры.
    /// </summary>
    public void SetManipulatorModeCamera()
    {
        SetManipulatorMode(SceneManipulatorMode.Drag);
        CameraModeActive = true;
    }

    /// <summary>
    /// Устанавливает режим манипулятора (Move, Rotate и т.д.).
    /// </summary>
    /// <param name="mode">Новый режим манипуляции</param>
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
                rotateMode.cursorAngleText = angleTextPrefab;
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

    /// <summary>
    /// Устанавливает режим системы координат (Local/Global).
    /// </summary>
    /// <param name="mode">Новый режим осей</param>
    public void SetAxisMode(AxisMode mode)
    {
        if (Target == null) return;

        if (mode == AxisMode.Local)
        {
            gizmoRoot.rotation = Target.rotation;
        }
        else
        {
            gizmoRoot.rotation = Quaternion.identity;
            gizmoRootStartRotation = gizmoRoot.rotation;
        }
    }

    /// <summary>
    /// Обновляет ориентацию визуальных элементов гиджмо.
    /// Учитывает текущий режим осей и начальное вращение.
    /// </summary>
    private void UpdateHandlesOrientation()
    {
        if (Target == null) return;

        if (_axisModeManager.Mode == AxisMode.Local)
        {
            gizmoRoot.rotation = Target.rotation;
            moveHandlesGroup.transform.localRotation = Quaternion.identity;
        }
        else
        {
            if (gizmoRootStartRotation == Quaternion.identity)
                gizmoRootStartRotation = gizmoRoot.rotation;

            moveHandlesGroup.transform.rotation = Quaternion.identity;
        }
    }
}
