using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.AxisModes;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
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

        public Camera cam;
        public float gizmoScaleKoeficient = 0.1f;
        public Vector3 textOffset = new Vector3(0, 0.15f, 0);
        public Transform gizmoRoot;
        [HideInInspector] public Quaternion gizmoRootStartRotation;
        public event Action<Transform> OnTargetTransformChanged;
        public event Action<Transform> OnDragStart;
        public event Action<Transform> OnDragEnd;
        private EventBus _eventBus;
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
            Target = transform;
            
            gizmoRoot = transform;
            _pointProvider = gameObject.GetComponent<JOGPropertyProvider>();
            gameObject.SetActive(false);
            angleTextPrefab = GameObject.Find("PreviewRotationText").GetComponent<TextMeshPro>();
        }
        private void Start()
        {
           
            _eventBus = ServiceManager.Current.Get<EventBus>();
            _manipulatorModeManager = ServiceManager.Current.Get<SceneManipulatorModeManager>();
            //SetManipulatorMode(new MoveMode());
            if (cam == null)
            {
                cam = Camera.main;
            }
            //SetAxisMode(AxisMode.Global);
            CurrentAxisMode = AxisMode.Local;
            
            CurrentManipulatorMode = new JOGMode();
            CurrentManipulatorMode.cursorAngleText = angleTextPrefab;
            //angleTextPrefab.gameObject.SetActive(true);
        }
        private void FixedUpdate()
        {
            //if (Target != null)
            //    gizmoRoot.position = Target.position;

            float dist = Vector3.Distance(cam.transform.position, gizmoRoot.position);
            if (dist > 2)
            {
                gizmoRoot.localScale = Vector3.one * dist * gizmoScaleKoeficient;
                angleTextPrefab.gameObject.transform.localScale = Vector3.one * dist * gizmoScaleKoeficient;
            }

            UpdateHandlesOrientation();
            UpdateJogText();
        }


        private void UpdateHandlesOrientation()
        {
            if (Target == null) return;

            //if (CurrentAxisMode == AxisMode.Local)
            gizmoRoot.rotation = Target.rotation;
            // привязка moveHandles и plane к локальной системе объекта
            moveHandlesGroup.transform.localRotation = Quaternion.identity;

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
