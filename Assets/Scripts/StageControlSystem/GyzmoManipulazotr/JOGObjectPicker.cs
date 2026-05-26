using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Manipulator;
using Assets.Scripts.CustomEventBus.Signals.ObjectPicker_;
using Assets.Scripts.CustomEventBus.Signals.ObjectSignals;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.GyzmoManipulazotr
{
    public class JOGObjectPicker : MonoBehaviour
    {
        //public JOGManipulator manipulator;
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
        private SceneManipulatorModeManager _modeManager;

        private JOGManipulator _currentActiveManipulator;
        //public bool IsDraggingManipulator => currentHandle != null;
        private void Start()
        {
            _eventBus = ServiceManager.Current.Get<EventBus>();
            
            //_eventBus.Subscribe<PickObjectSignal>(OnPickObject);
            //_eventBus.Subscribe<UnpickObjectSignal>(OnUnpickObject);
            _uiStatusManager = ServiceManager.Current.Get<UIStatusManager>();
            _sceneObjectsManager = ServiceManager.Current.Get<SceneObjectsManager>();
            _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();
            _modeManager = ServiceManager.Current.Get<SceneManipulatorModeManager>();
            _currentActiveManipulator?.gameObject.SetActive(false);
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
            _currentActiveManipulator.gameObject.SetActive(false);
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
        private void SetActiveManipulator(JOGManipulator newManipulator)
        {
            // Отписываемся от событий старого манипулятора, если он существует
            if (_currentActiveManipulator != null)
            {
                _currentActiveManipulator.OnTargetTransformChanged -= HandleTransformChanged;
                _currentActiveManipulator.OnDragEnd -= Manipulator_OnDragEnd;
                _currentActiveManipulator.OnDragStart -= Manipulator_OnDragStart;

                // Деактивируем старый манипулятор
                _currentActiveManipulator.gameObject.SetActive(false);
            }

            // Устанавливаем новый манипулятор
            _currentActiveManipulator = newManipulator;

            if (_currentActiveManipulator != null)
            {
                // Подписываемся на события нового манипулятора
                _currentActiveManipulator.OnTargetTransformChanged += HandleTransformChanged;
                _currentActiveManipulator.OnDragEnd += Manipulator_OnDragEnd;
                _currentActiveManipulator.OnDragStart += Manipulator_OnDragStart;

                // Активируем новый манипулятор
                _currentActiveManipulator.gameObject.SetActive(true);
            }

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
            if (_modeManager.Mode != SceneManipulatorMode.JOG)
            {
                if (_currentActiveManipulator != null)
                    _currentActiveManipulator.gameObject.SetActive(false);
                return;
            }
            


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
                else
                {
                    // ИСПРАВЛЕНИЕ: Используем RaycastAll вместо Raycast
                    RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity);

                    if (hits.Length > 0)
                    {
                        // Сортируем по расстоянию (от ближнего к дальнему)
                        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                        // Ищем первый объект с IPropertyProvider
                        foreach (RaycastHit hit in hits)
                        {
                            // Проверяем, есть ли IPropertyProvider на этом объекте
                            IPropertyProvider provider = hit.collider.GetComponentInParent<IPropertyProvider>();
                            if (provider != null)
                            {
                                var obj = _sceneObjectsManager.GetById(provider.Id);
                                if (obj.Type != ObjectType.Robot) continue;

                                var jogPoint = FindInChildren(obj.Reference.transform, "JOG_Manipulator");
                                if (jogPoint == null) break;

                                var newManipulator = jogPoint.GetComponent<JOGManipulator>();
                                if (newManipulator != null)
                                {
                                    // Используем метод для смены манипулятора
                                    SetActiveManipulator(newManipulator);
                                }

                                var pointProvier = jogPoint.GetComponent<JOGPropertyProvider>();
                                if (pointProvier == null) break;

                                currentProvider = pointProvier;
                                _eventBus.Invoke(new SelectObjectInScene(currentProvider.Id));
                                _eventBus.Invoke(new ChangePropertiesProviderSignal(currentProvider));
                                break; // Выходим после нахождения первого подходящего объекта
                            }
                        }
                    }
                    //else
                    //{
                    //    manipulator.gameObject.SetActive(false);

                    //}
                }
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
        private Transform FindInChildren(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                var result = FindInChildren(child, name);
                if (result != null)
                    return result;
            }
            return null;
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
