using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GyzmoManipulazotr
{
    public class JOGObjectPicker : MonoBehaviour
    {
        public JOGManipulator manipulator;
        private AxisHandleJOG currentHandle;
        //[SerializeField] private UIBlocker uIBlocker;
        //public PropertiesPanelEvents propertiesPanel;
        private UIStatusManager _uiStatusManager;
        private IPropertyProvider currentProvider;
        private Vector3 startPos;
        private Vector3 startRot;
        private EventBus _eventBus;
        private SceneObjectsManager _sceneObjectsManager;
        private UndoRedoManager _undoRedoManager;
        //public bool IsDraggingManipulator => currentHandle != null;
        private void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            //_eventBus.Subscribe<PickObjectSignal>(OnPickObject);
            //_eventBus.Subscribe<UnpickObjectSignal>(OnUnpickObject);
            _uiStatusManager = ServiceManager.Current.Get<UIStatusManager>();
            _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
            if (manipulator != null)
            {
                //manipulator.gameObject.SetActive(false);
                manipulator.OnTargetTransformChanged += HandleTransformChanged;
                manipulator.OnDragEnd += Manipulator_OnDragEnd;
                manipulator.OnDragStart += Manipulator_OnDragStart;
            }
        }

        //private void OnUnpickObject(UnpickObjectSignal signal)
        //{
        //    UnpickObject();
        //}

        //private void OnPickObject(PickObjectSignal signal)
        //{
        //    PickObject(signal.Object);
        //}

        private void OnSetManipulatorMode(SetGyzmoManipulatorModeSignal signal)
        {

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
            //if (currentProvider != null)
            //{
            //    if (startPos != endPos)
            //    {
            //        _undoRedoManager.Execute(
            //            new PropertyChangeCommand(currentProvider, nameof(IPropertyProvider.Position), startPos, endPos)
            //        );
            //    }

            //    if (startRot != endRot)
            //    {
            //        _undoRedoManager.Execute(
            //            new PropertyChangeCommand(currentProvider, nameof(IPropertyProvider.Rotation), startRot, endRot)
            //        );
            //    }
            //}

        }

        private void HandleTransformChanged(Transform transform)
        {
            if (transform == null) return;

            if (currentProvider != null)
                _eventBus.Invoke(new PropertiesTransformUpdateSignal());
            //propertiesPanel.UpdateTransform(currentProvider);
        }

        private void Update()
        {
            //if (manipulator.CameraModeActive)
            //{
            //    if (currentHandle != null) UnpickObject();
            //    return;
            //}

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int manipLayerMask = LayerMask.GetMask("Manipulator");

            if (Input.GetMouseButtonDown(0) && !_uiStatusManager.isPointerOverUI)
            {
                // Клик по манипулятору
                if (Physics.Raycast(ray, out RaycastHit hitHandle, Mathf.Infinity, manipLayerMask))
                {
                    //Debug.Log(hitHandle.transform.gameObject.name);
                    AxisHandleJOG handle = hitHandle.collider.GetComponent<AxisHandleJOG>();
                    if (handle != null)
                    {
                        currentHandle = handle;
                        currentHandle.StartDrag();
                    }
                }
                // Клик по объекту сцены
                //else
                //{
                //    manipulator.gameObject.SetActive(false);
                //}
            }

            if (Input.GetMouseButton(0) && currentHandle != null)
                currentHandle.UpdateDrag();

            // Отпускание
            if (Input.GetMouseButtonUp(0) && currentHandle != null)
            {
                currentHandle.EndDrag();
                currentHandle = null;
            }
        }

        //public void PickObject(GameObject gameObject)
        //{
        //    IPropertyProvider provider = null;
        //    GameObject target = gameObject;

        //    // Ищем провайдер на самом объекте
        //    if (!target.TryGetComponent<IPropertyProvider>(out provider))
        //    {
        //        Debug.LogWarning($"На объекте {target.name} нет IPropertyProvider");
        //        return;
        //    }
        //    manipulator.Attach(gameObject.transform);
        //    currentProvider = gameObject.GetComponent<IPropertyProvider>();
        //    manipulator.gameObject.SetActive(true);
        //}
        //public void UnpickObject()
        //{
        //    manipulator.Detach();
        //}

        //public IManipulatorMode GetManipulatorMode()
        //{
        //    return manipulator.CurrentManipulatorMode;
        //}
    }
}
