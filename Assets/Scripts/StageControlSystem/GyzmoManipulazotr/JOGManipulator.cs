using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.AxisModes;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.GyzmoManipulazotr
{
    public class JOGManipulator : MonoBehaviour
    {
        public TextMeshPro angleTextPrefab;
        public JOGMode CurrentManipulatorMode { get; private set; }
        public Transform Target { get; private set; }
        public AxisMode? CurrentAxisMode { get; private set; }
        public bool CameraModeActive { get; private set; } = false;
        public GameObject moveHandlesGroup;
        public GameObject rotateHandlesGroup;
        private SceneManipulatorModeManager _manipulatorModeManager;
        private JOGPropertyProvider _pointProvider;
        private Dictionary<Transform, Quaternion> _handlesOriginalRotation = new Dictionary<Transform, Quaternion>();
        private Dictionary<Transform, Vector3> _handlesOriginalPosition = new Dictionary<Transform, Vector3>();
        public Camera cam;
        public float gizmoScaleKoeficient = 0.1f;
        public Vector3 textOffset = new Vector3(0, 0.15f, 0);
        public Transform gizmoRoot;
        [HideInInspector] public Quaternion gizmoRootStartRotation;
        public event Action<Transform> OnTargetTransformChanged;
        public event Action<Transform> OnDragStart;
        public event Action<Transform> OnDragEnd;
        private EventBus _eventBus;
        //public SceneManipulatorMode CurrentSceneMode => _manipulatorModeManager.Mode;
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
            Target = transform;
            if (cam == null)
            {
                cam = Camera.main;
            }

            CurrentAxisMode = AxisMode.Local;
            CurrentManipulatorMode = new JOGMode();
            CurrentManipulatorMode.cursorAngleText = angleTextPrefab;

            // Создаём отдельный gizmoRoot, не трогаем transform
            gizmoRoot = new GameObject("JOG_GizmoRoot").transform;
            gizmoRoot.SetParent(transform.parent);
            gizmoRoot.position = transform.position;
            gizmoRoot.rotation = transform.rotation;

            // Перемещаем moveHandlesGroup под gizmoRoot
            if (moveHandlesGroup != null)
                moveHandlesGroup.transform.SetParent(gizmoRoot, true);

            // Перемещаем rotateHandlesGroup под gizmoRoot
            if (rotateHandlesGroup != null)
                rotateHandlesGroup.transform.SetParent(gizmoRoot, true);

            _pointProvider = gameObject.GetComponent<JOGPropertyProvider>();
        }

        private void Start()
        {
            _manipulatorModeManager = ServiceManager.Current.Get<SceneManipulatorModeManager>();
            _axisModeManager = ServiceManager.Current.Get<AxisModeManager>();
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _eventBus.Subscribe<SetAxisModeSignal>(OnSetAxisMode);
            _eventBus.Subscribe<SetGyzmoManipulatorModeSignal>(OnSetManipulatorMode);
            CurrentAxisMode = _axisModeManager.Mode;
            foreach (Transform handle in moveHandlesGroup.transform)
            {
                AxisHandleJOG ah = handle.GetComponent<AxisHandleJOG>();
                if (ah != null && ah.directionType != HandleType.Plane)
                {
                    Debug.Log($"{handle.name}: direction={ah.direction}, forward={handle.forward}, up={handle.up}, right={handle.right}");
                }
            }

            //SetManipulatorMode(new MoveMode());

            //angleTextPrefab.gameObject.SetActive(true);
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
        /// Устанавливает режим системы координат (Local/Global).
        /// </summary>
        /// <param name="mode">Новый режим осей</param>
        public void SetAxisMode(AxisMode mode)
        {
            CurrentAxisMode = mode;

            if (Target == null) return;

            if (mode == AxisMode.Local)
            {
                gizmoRoot.rotation = Target.rotation;
            }
            else
            {
                gizmoRoot.rotation = Quaternion.identity;
            }
        }
        private void OnStartSimulation(StartSimulationSignal signal)
        {
            throw new NotImplementedException();
        }

        private void OnSetManipulatorMode(SetGyzmoManipulatorModeSignal signal)
        {
            if (signal.Mode != SceneManipulatorMode.JOG)
            {
                _eventBus.Invoke(new ChangePropertiesProviderSignal(null));
            }
        }

        private void FixedUpdate()
        {
            // Обновляем позицию gizmoRoot на позицию захвата
            gizmoRoot.position = transform.position;

            float dist = Vector3.Distance(cam.transform.position, gizmoRoot.position);
            if (dist > 2)
            {
                gizmoRoot.localScale = Vector3.one * dist * gizmoScaleKoeficient;
            }
            else
            {
                gizmoRoot.localScale = Vector3.one * 2 * gizmoScaleKoeficient;
                angleTextPrefab.gameObject.transform.localScale = Vector3.one * 2 * gizmoScaleKoeficient;
            }

            UpdateHandlesOrientation();
            UpdateJogText();
        }


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

                // В Global режиме кольца не вращаются
                if (rotateHandlesGroup != null)
                {
                    if (!_handlesOriginalRotation.ContainsKey(rotateHandlesGroup.transform))
                    {
                        _handlesOriginalRotation[rotateHandlesGroup.transform] = rotateHandlesGroup.transform.localRotation;
                    }
                    rotateHandlesGroup.transform.localRotation = _handlesOriginalRotation[rotateHandlesGroup.transform];
                }
            }

            Vector3 toCamera = (cam.transform.position - gizmoRoot.position).normalized;
            float currentScale = gizmoRoot.localScale.x;

            // ПЛОСКОСТИ — виртуальные направления стрелок
            foreach (Transform handle in moveHandlesGroup.transform)
            {
                AxisHandleJOG axisHandle = handle.GetComponent<AxisHandleJOG>();
                if (axisHandle == null) continue;
                if (axisHandle.directionType != HandleType.Plane) continue;

                if (!_handlesOriginalPosition.ContainsKey(handle))
                {
                    _handlesOriginalPosition[handle] = handle.localPosition;
                    _handlesOriginalRotation[handle] = handle.localRotation;
                }

                handle.localRotation = _handlesOriginalRotation[handle];

                Vector3 axis1Dir = Vector3.zero;
                Vector3 axis2Dir = Vector3.zero;

                foreach (Transform arrow in moveHandlesGroup.transform)
                {
                    AxisHandleJOG arrowHandle = arrow.GetComponent<AxisHandleJOG>();
                    if (arrowHandle == null || arrowHandle.directionType == HandleType.Plane) continue;

                    if (Vector3.Dot(arrowHandle.direction.normalized, axisHandle.planeNormal.normalized) < 0.1f)
                    {
                        Vector3 dir;
                        if (_axisModeManager.Mode == AxisMode.Local && Target != null)
                            dir = Target.TransformDirection(arrowHandle.direction.normalized);
                        else
                            dir = arrowHandle.direction.normalized;

                        float dotCamera = Vector3.Dot(toCamera, dir);
                        if (dotCamera < 0) dir = -dir;

                        if (axis1Dir == Vector3.zero)
                            axis1Dir = dir;
                        else
                            axis2Dir = dir;
                    }
                }

                if (axis1Dir != Vector3.zero && axis2Dir != Vector3.zero)
                {
                    Vector3 midDirection = (axis1Dir + axis2Dir).normalized;
                    float originalDistance = _handlesOriginalPosition[handle].magnitude;
                    float scaledDistance = originalDistance * currentScale;

                    handle.position = gizmoRoot.position + midDirection * scaledDistance;
                }
            }
        }
        private void UpdateJogText()
        {
            if (angleTextPrefab == null) return;
            Transform cam = Camera.main.transform;

            angleTextPrefab.transform.position = gizmoRoot.position + textOffset;

            angleTextPrefab.transform.rotation = Quaternion.LookRotation(
                angleTextPrefab.transform.position - cam.transform.position, cam.up
            );
        }
    }
}
