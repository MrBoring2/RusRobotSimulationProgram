using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.AxisModes;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using System;
using System.Collections.Generic;
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
    private Dictionary<Transform, Quaternion> _handlesOriginalRotation = new Dictionary<Transform, Quaternion>();
    private Dictionary<Transform, Vector3> _handlesOriginalPosition = new Dictionary<Transform, Vector3>();
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
        if (Target == null || cam == null) return;

        if (_axisModeManager.Mode == AxisMode.Local)
        {
            gizmoRoot.rotation = Target.rotation;
        }
        else
        {
            gizmoRoot.rotation = Quaternion.identity;
            gizmoRootStartRotation = gizmoRoot.rotation;
        }

        foreach (Transform handle in moveHandlesGroup.transform)
        {
            AxisHandle axisHandle = handle.GetComponent<AxisHandle>();
            if (axisHandle == null) continue;
            if (axisHandle.type == HandleType.Plane) continue;

            if (!_handlesOriginalRotation.ContainsKey(handle))
            {
                _handlesOriginalRotation[handle] = handle.localRotation;
            }

            handle.localRotation = _handlesOriginalRotation[handle];

            Vector3 axisWorldDirection;
            if (_axisModeManager.Mode == AxisMode.Local && Target != null)
            {
                axisWorldDirection = Target.TransformDirection(axisHandle.direction.normalized);
            }
            else
            {
                axisWorldDirection = axisHandle.direction.normalized;
            }

            Vector3 handleToCamera = (cam.transform.position - handle.position).normalized;

            float dotProduct = Vector3.Dot(handleToCamera, axisWorldDirection);

            if (dotProduct < 0)
            {
                Vector3 rotationAxis;

                if (Mathf.Abs(axisWorldDirection.x) < 0.9f)
                    rotationAxis = Vector3.Cross(axisWorldDirection, Vector3.right).normalized;
                else if (Mathf.Abs(axisWorldDirection.y) < 0.9f)
                    rotationAxis = Vector3.Cross(axisWorldDirection, Vector3.up).normalized;
                else
                    rotationAxis = Vector3.Cross(axisWorldDirection, Vector3.forward).normalized;

                handle.Rotate(rotationAxis * 180f, Space.World);
            }
        }

        foreach (Transform handle in moveHandlesGroup.transform)
        {
            AxisHandle axisHandle = handle.GetComponent<AxisHandle>();
            if (axisHandle == null) continue;
            if (axisHandle.type != HandleType.Plane) continue;

            if (!_handlesOriginalRotation.ContainsKey(handle))
            {
                _handlesOriginalRotation[handle] = handle.localRotation;
                _handlesOriginalPosition[handle] = handle.localPosition;
            }

            handle.localRotation = _handlesOriginalRotation[handle];
            handle.localPosition = _handlesOriginalPosition[handle];

            Transform arrow1 = null;
            Transform arrow2 = null;

            foreach (Transform arrow in moveHandlesGroup.transform)
            {
                AxisHandle arrowHandle = arrow.GetComponent<AxisHandle>();
                if (arrowHandle == null || arrowHandle.type == HandleType.Plane) continue;

                if (Vector3.Dot(arrowHandle.direction.normalized, axisHandle.planeNormal.normalized) < 0.1f)
                {
                    if (arrow1 == null)
                        arrow1 = arrow;
                    else if (arrow2 == null)
                        arrow2 = arrow;
                }
            }
            if (arrow1 != null && arrow2 != null)
            {
                Vector3 dir1, dir2;
                if (_axisModeManager.Mode == AxisMode.Local && Target != null)
                {
                    dir1 = Target.TransformDirection(arrow1.GetComponent<AxisHandle>().direction.normalized);
                    dir2 = Target.TransformDirection(arrow2.GetComponent<AxisHandle>().direction.normalized);
                }
                else
                {
                    dir1 = arrow1.GetComponent<AxisHandle>().direction.normalized;
                    dir2 = arrow2.GetComponent<AxisHandle>().direction.normalized;
                }

                Vector3 arrow1Forward = arrow1.forward;
                Vector3 arrow2Forward = arrow2.forward;

                float dot1 = Vector3.Dot(arrow1Forward, dir1);
                float dot2 = Vector3.Dot(arrow2Forward, dir2);

                if (dot1 < 0) dir1 = -dir1;
                if (dot2 < 0) dir2 = -dir2;

                Vector3 midDirection = (dir1 + dir2).normalized;

                // Берём изначальную ЛОКАЛЬНУЮ дистанцию и умножаем на текущий scale gizmoRoot
                float originalDistance = _handlesOriginalPosition[handle].magnitude;
                float currentScale = gizmoRoot.localScale.x; // scale одинаковый по всем осям
                float scaledDistance = originalDistance * currentScale;

                handle.position = gizmoRoot.position + midDirection * scaledDistance;
            }

            Vector3 axisWorldDirection;
            if (_axisModeManager.Mode == AxisMode.Local && Target != null)
            {
                axisWorldDirection = Target.TransformDirection(axisHandle.planeNormal.normalized);
            }
            else
            {
                axisWorldDirection = axisHandle.planeNormal.normalized;
            }

            Vector3 handleToCamera = (cam.transform.position - handle.position).normalized;
            float dotProduct = Vector3.Dot(handleToCamera, axisWorldDirection);

            if (dotProduct < 0)
            {
                handle.Rotate(Vector3.forward * 180f, Space.Self);
            }
        }
    }
    private void AdjustHandleTowardsCamera(Transform handleTransform, Vector3 camDirection)
    {
        AxisHandle axisHandle = handleTransform.GetComponent<AxisHandle>();
        if (axisHandle == null) return;

        // Вычисляем мировое направление оси ручки
        Vector3 axisWorldDirection;
        if (_axisModeManager.Mode == AxisMode.Local && Target != null)
        {
            axisWorldDirection = Target.TransformDirection(axisHandle.direction.normalized);
        }
        else
        {
            axisWorldDirection = axisHandle.direction.normalized;
        }

        float dotProduct = Vector3.Dot(camDirection, axisWorldDirection);

        if (dotProduct < 0)
        {
            handleTransform.rotation = Quaternion.AngleAxis(180f, axisWorldDirection) * handleTransform.rotation;
        }
    }
}
