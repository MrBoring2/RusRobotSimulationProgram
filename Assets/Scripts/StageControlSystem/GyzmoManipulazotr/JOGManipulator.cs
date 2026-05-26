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
            //SetAxisMode(AxisMode.Global);
            CurrentAxisMode = AxisMode.Local;

            CurrentManipulatorMode = new JOGMode();
            CurrentManipulatorMode.cursorAngleText = angleTextPrefab;
            gizmoRoot = transform;


            _pointProvider = gameObject.GetComponent<JOGPropertyProvider>();
            //angleTextPrefab = GameObject.Find("PreviewRotationText").GetComponent<TextMeshPro>();
        }
        private void Start()
        {
            _manipulatorModeManager = ServiceManager.Current.Get<SceneManipulatorModeManager>();

            _axisModeManager = ServiceManager.Current.Get<AxisModeManager>();
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _eventBus.Subscribe<SetAxisModeSignal>(OnSetAxisMode);
            _eventBus.Subscribe<SetGyzmoManipulatorModeSignal>(OnSetManipulatorMode);
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
            //if (Target != null)
            //    gizmoRoot.position = Target.position;

            float dist = Vector3.Distance(cam.transform.position, gizmoRoot.position);
            if (dist > 2)
            {
                gizmoRoot.localScale = Vector3.one * dist * gizmoScaleKoeficient;
                //angleTextPrefab.gameObject.transform.localScale = Vector3.one * dist * gizmoScaleKoeficient;
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

            gizmoRoot.rotation = Target.rotation;

            // СТРЕЛКИ
            foreach (Transform handle in moveHandlesGroup.transform)
            {
                AxisHandleJOG axisHandle = handle.GetComponent<AxisHandleJOG>();
                if (axisHandle == null) continue;
                if (axisHandle.directionType == HandleType.Plane) continue;

                if (!_handlesOriginalRotation.ContainsKey(handle))
                {
                    _handlesOriginalRotation[handle] = handle.localRotation;
                }

                // Всегда начинаем с изначального rotation
                handle.localRotation = _handlesOriginalRotation[handle];

                Vector3 axisWorldDirection = Target.TransformDirection(axisHandle.direction.normalized);
                Vector3 toCamera = (cam.transform.position - gizmoRoot.position).normalized;
                float dotProduct = Vector3.Dot(toCamera, axisWorldDirection);

                if (dotProduct < 0)
                {
                    // Поворачиваем на 180° вокруг СВОЕЙ ЛОКАЛЬНОЙ оси
                    if (Mathf.Abs(axisHandle.direction.x) > 0.9f)
                        handle.Rotate(Vector3.up * 180f, Space.Self);
                    else if (Mathf.Abs(axisHandle.direction.y) > 0.9f)
                        handle.Rotate(Vector3.right * 180f, Space.Self);
                    else
                        handle.Rotate(Vector3.right * 180f, Space.Self);
                }
            }

            // ПЛОСКОСТИ — всё остальное без изменений
            foreach (Transform handle in moveHandlesGroup.transform)
            {
                AxisHandleJOG axisHandle = handle.GetComponent<AxisHandleJOG>();
                if (axisHandle == null) continue;
                if (axisHandle.directionType != HandleType.Plane) continue;

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
                    AxisHandleJOG arrowHandle = arrow.GetComponent<AxisHandleJOG>();
                    if (arrowHandle == null || arrowHandle.directionType == HandleType.Plane) continue;

                    if (Vector3.Dot(arrowHandle.direction.normalized, axisHandle.planeNormal.normalized) < 0.1f)
                    {
                        if (arrow1 == null) arrow1 = arrow;
                        else if (arrow2 == null) arrow2 = arrow;
                    }
                }

                if (arrow1 != null && arrow2 != null)
                {
                    Vector3 dir1 = Target.TransformDirection(arrow1.GetComponent<AxisHandleJOG>().direction.normalized);
                    Vector3 dir2 = Target.TransformDirection(arrow2.GetComponent<AxisHandleJOG>().direction.normalized);

                    Vector3 arrow1Forward = arrow1.forward;
                    Vector3 arrow2Forward = arrow2.forward;

                    float dot1 = Vector3.Dot(arrow1Forward, dir1);
                    float dot2 = Vector3.Dot(arrow2Forward, dir2);

                    if (dot1 < 0) dir1 = -dir1;
                    if (dot2 < 0) dir2 = -dir2;

                    Vector3 midDirection = (dir1 + dir2).normalized;
                    float originalDistance = _handlesOriginalPosition[handle].magnitude;
                    float currentScale = gizmoRoot.localScale.x;
                    float scaledDistance = originalDistance * currentScale;

                    handle.position = gizmoRoot.position + midDirection * scaledDistance;
                }

                Vector3 planeWorldNormal = Target.TransformDirection(axisHandle.planeNormal.normalized);
                Vector3 toCamera = (cam.transform.position - gizmoRoot.position).normalized;
                float dotProduct = Vector3.Dot(toCamera, planeWorldNormal);

                if (dotProduct < 0)
                {
                    handle.Rotate(Vector3.forward * 180f, Space.Self);
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
