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
using UnityEngine;

namespace Assets.Scripts.GyzmoManipulazotr
{
    public class JOGManipulator : MonoBehaviour
    {
        public JOGMode CurrentManipulatorMode { get; private set; }
        public Transform Target { get; private set; }
        public AxisMode? CurrentAxisMode { get; private set; }
        public bool CameraModeActive { get; private set; } = false;
        public GameObject moveHandlesGroup;
        public GameObject rotateHandlesGroup;
        private SceneManipulatorModeManager _manipulatorModeManager;
        private LinearPointPropertyProvider _pointProvider;

        public Camera cam;
        public float gizmoScaleKoeficient = 0.1f;

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
            CurrentManipulatorMode = new JOGMode();
            gizmoRoot = transform;
            _pointProvider = gameObject.GetComponent<LinearPointPropertyProvider>();
            gameObject.SetActive(false);
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
        }
        private void Update()
        {
            //if (Target != null)
            //    gizmoRoot.position = Target.position;

            float dist = Vector3.Distance(cam.transform.position, gizmoRoot.position);
            gizmoRoot.localScale = Vector3.one * dist * gizmoScaleKoeficient;

            UpdateHandlesOrientation();
        }


        private void UpdateHandlesOrientation()
        {
            if (Target == null) return;

            //if (CurrentAxisMode == AxisMode.Local)
            gizmoRoot.rotation = Target.rotation;
            // привязка moveHandles и plane к локальной системе объекта
            moveHandlesGroup.transform.localRotation = Quaternion.identity;

        }
    }
}
