using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.PropertiesPanel;
using Assets.Scripts.CustomEventBus.Signals.UndoRedoSystem;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.Models;
using Assets.Scripts.Providers;
using Assets.Scripts.StageControlSystem.Models;
using Assets.Scripts.StageControlSystem.Utils;
using Assets.Scripts.SystemManager;
using Assets.UI.CustomElements;
using Assets.UI.CustomElements.ColorField;
using Assets.UI.CustomElements.ColorPicker;
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using static UnityEngine.Rendering.STP;

public class PropertiesPanelEvents : MonoBehaviour
{
    private VisualElement root;
    private VisualElement propertiesPanel;
    //public UIBlocker uiBlocker;
    private IPropertyProvider current;
    private VisualElement commonContainer;
    private VisualElement customContainer;
    private FloatField posX, posY, posZ;
    private FloatField rotX, rotY, rotZ;
    private FloatField scaleX, scaleY, scaleZ;
    private VisualElement namePropertyContainer;
    private VisualElement positionPropertyContainer;
    private VisualElement rotationPropertyContainer;
    private VisualElement scalePropertyContainer;
    private TextField name;
    private EventBus _eventBus;
    private UIStatusManager _UIStatusManager;
    private List<Action> cleanupActions = new List<Action>();
    private UndoRedoManager _undoRedoManager;

    // public event Action OnTargetNameChanged;

    void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _eventBus.Subscribe<PropertiesTransformUpdateSignal>(OnTransformChanged);
        _eventBus.Subscribe<ChangeAnglesJOGSignal>(OnAnglesChanged);
        _eventBus.Subscribe<ChangePropertiesProviderSignal>(OnChangePropertiesProvider);
        _eventBus.Subscribe<TogglePropertiesSignal>(OnToggleProperties);
        _eventBus.Subscribe<ExecuteCommandSignal>(OnCommandExecuted);
        _eventBus.Subscribe<UndoneCommandSignal>(OnCommandUndoned);
        _UIStatusManager = ServiceManager.Current.Get<UIStatusManager>();
        _undoRedoManager = ServiceManager.Current.Get<UndoRedoManager>();

        root = GetComponent<UIDocument>().rootVisualElement;
        propertiesPanel = root.Q<VisualElement>("properties-container");
        TogglePanel();
        commonContainer = propertiesPanel.Q("base-properties-container");
        customContainer = propertiesPanel.Q("custom-properties-container");

        posX = propertiesPanel.Q<FloatField>("position-x");
        posY = propertiesPanel.Q<FloatField>("position-y");
        posZ = propertiesPanel.Q<FloatField>("position-z");

        rotX = propertiesPanel.Q<FloatField>("rotation-x");
        rotY = propertiesPanel.Q<FloatField>("rotation-y");
        rotZ = propertiesPanel.Q<FloatField>("rotation-z");

        scaleX = propertiesPanel.Q<FloatField>("scale-x");
        scaleY = propertiesPanel.Q<FloatField>("scale-y");
        scaleZ = propertiesPanel.Q<FloatField>("scale-z");

        name = propertiesPanel.Q<TextField>("name");

        namePropertyContainer = propertiesPanel.Q<VisualElement>("name-property-container");
        positionPropertyContainer = propertiesPanel.Q<VisualElement>("position-property-container");
        rotationPropertyContainer = propertiesPanel.Q<VisualElement>("rotation-property-container");
        scalePropertyContainer = propertiesPanel.Q<VisualElement>("scale-property-container");

        RegisterButtons();
        RegisterInputs();
        HideElement(namePropertyContainer);
        HideElement(positionPropertyContainer);
        HideElement(rotationPropertyContainer);
        HideElement(scalePropertyContainer);
        //UndoRedoManager.Instance.OnCommandExecuted += OnUndoRedoPerformed;
        //UndoRedoManager.Instance.OnCommandUndone += OnUndoRedoPerformed;
    }

    private void OnAnglesChanged(ChangeAnglesJOGSignal signal)
    {
        UpdateJointsJOG(signal.Robot);
    }

    private void OnToggleProperties(TogglePropertiesSignal signal)
    {
        TogglePanel();
    }

    private void OnChangePropertiesProvider(ChangePropertiesProviderSignal signal)
    {
        ChangePropertiesProvider(signal.PropertyProvider);
        if (signal.PropertyProvider != null)
            _UIStatusManager.SetPropertiesPanelVisibility(true);
        else _UIStatusManager.SetPropertiesPanelVisibility(false);
        Debug.Log($"[UI] current provider instance = {(signal.PropertyProvider as MonoBehaviour)?.GetInstanceID()}");
    }

    private void OnTransformChanged(PropertiesTransformUpdateSignal signal)
    {
        UpdateTransform();
    }

    public void ChangePropertiesProvider(IPropertyProvider propertyProvider)
    {
        FieldBindingUtils.FlushAllPendingChanges();
        ClearBindings();

        current = propertyProvider;

        if (propertyProvider == null || !current.DisplayName)
        {
            HideElement(namePropertyContainer);
        }
        else ShowElement(namePropertyContainer);
        if (propertyProvider == null || !current.DisplayPosition)
        {
            HideElement(positionPropertyContainer);
        }
        else ShowElement(positionPropertyContainer);
        if (propertyProvider == null || !current.DisplayRotation)
        {
            HideElement(rotationPropertyContainer);
        }
        else ShowElement(rotationPropertyContainer);
        if (propertyProvider == null || !current.DisplayScale)
        {
            HideElement(scalePropertyContainer);
        }
        else ShowElement(scalePropertyContainer);

        if (propertyProvider == null || current.NameReadOnly)
        {
            var name = namePropertyContainer.Q<TextField>("name");
            if (name != null)
            {
                name.isReadOnly = true;
            }
        }
        else
        {
            var name = namePropertyContainer.Q<TextField>("name");
            if (name != null)
            {
                name.isReadOnly = false;
            }
        }

        customContainer.Clear();

        if (current != null)
        {
            UpdateUI();
            RegisterBaseFieldsBindings();
            BuildCustomProperties(current);
        }
    }

    private void HideElement(VisualElement element)
    {
        element.style.display = DisplayStyle.None;
    }
    private void ShowElement(VisualElement element)
    {
        element.style.display = DisplayStyle.Flex;
    }

    private void OnCommandUndoned(UndoneCommandSignal signal)
    {
        PerformUndoRedo(signal.Command);
    }

    private void OnCommandExecuted(ExecuteCommandSignal signal)
    {
        PerformUndoRedo(signal.Command);
    }
    private void PerformUndoRedo(ICommand command)
    {
        // ���� ������� ��������� � �������� �������, ��������� UI
        if (current != null && command is PropertyChangeCommand propertyCommand)
        {
            // ���������, ��������� �� ������� � �������� �������
            if (propertyCommand.Target == current ||
                (propertyCommand.Target is IPropertyProvider provider && provider == current))
            {
                UpdateUI();
            }
        }
        else if (command is CustomPropertyChangeCommand customCommand)
        {
            if (customCommand.Target == current ||
                (customCommand.Target is IPropertyProvider provider && provider == current))
            {
                // Перестроить кастомные поля
                BuildCustomProperties(current);
            }
        }
    }

    private void UpdateUI()
    {
        name.SetValueWithoutNotify(current.Name);
        UpdateTransform();
        //BuildCustomProperties(current);
    }

    private void ClearBindings()
    {
        // ������� ��� ��������
        foreach (var cleanup in cleanupActions)
        {
            cleanup?.Invoke();
        }
        cleanupActions.Clear();
    }

    private void RegisterBaseFieldsBindings()
    {
        if (current == null) return;

        // ������� - ��������� Action ��� �������
        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(posX, current, nameof(IPropertyProvider.LocalPosition), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.LocalPosition = new Vector3(posX.value, current.LocalPosition.y, current.LocalPosition.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(posY, current, nameof(IPropertyProvider.LocalPosition), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.LocalPosition = new Vector3(current.LocalPosition.x, posY.value, current.LocalPosition.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(posZ, current, nameof(IPropertyProvider.LocalPosition), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.LocalPosition = new Vector3(current.LocalPosition.x, current.LocalPosition.y, posZ.value);
        }));

        // ������� - ��������� Action ��� �������
        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(rotX, current, nameof(IPropertyProvider.Rotation), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.Rotation = new Vector3(NormalizeAngle(rotX.value), current.Rotation.y, current.Rotation.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(rotY, current, nameof(IPropertyProvider.Rotation), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.Rotation = new Vector3(current.Rotation.x, NormalizeAngle(rotY.value), current.Rotation.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(rotZ, current, nameof(IPropertyProvider.Rotation), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.Rotation = new Vector3(current.Rotation.x, current.Rotation.y, NormalizeAngle(rotZ.value));
        }));


        EventCallback<BlurEvent> rotXBlurHandler = evt => NormalizeRotationUI();
        EventCallback<BlurEvent> rotYBlurHandler = evt => NormalizeRotationUI();
        EventCallback<BlurEvent> rotZBlurHandler = evt => NormalizeRotationUI();

        rotX.RegisterCallback(rotXBlurHandler);
        rotY.RegisterCallback(rotYBlurHandler);
        rotZ.RegisterCallback(rotZBlurHandler);

        cleanupActions.Add(() => rotX.UnregisterCallback(rotXBlurHandler));
        cleanupActions.Add(() => rotY.UnregisterCallback(rotYBlurHandler));
        cleanupActions.Add(() => rotZ.UnregisterCallback(rotZBlurHandler));

        // ������� - ��������� Action ��� �������
        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(scaleX, current, nameof(IPropertyProvider.Scale), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.Scale = new Vector3(scaleX.value, current.Scale.y, current.Scale.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(scaleY, current, nameof(IPropertyProvider.Scale), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.Scale = new Vector3(current.Scale.x, scaleY.value, current.Scale.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(scaleZ, current, nameof(IPropertyProvider.Scale), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null) current.Scale = new Vector3(current.Scale.x, current.Scale.y, scaleZ.value);
        }));

        // ��� - ��������� Action ��� �������
        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(name, current, nameof(IPropertyProvider.Name), _undoRedoManager, _UIStatusManager, () =>
        {
            if (current != null)
            {
                current.Name = name.value;
                _eventBus.Invoke(new ChangeNamePropertySignal(current.Id, name.value));
                //OnTargetNameChanged?.Invoke();
            }
        }));
    }

    private void RegisterButtons()
    {
        var closeBtn = root.Q<Button>("close-properties-button");
        closeBtn.clicked += () =>
        {
            _UIStatusManager.TogglePropertiesPanel();
        };
    }

    private void RegisterInputs()
    {
        var inputs = root.Query<FloatField>().ToList();
        foreach (var input in inputs)
        {
            RegisterEventsforInput(input);
            //input.focusable = false;
            //input.RegisterCallback<MouseEnterEvent>(evt =>
            //{
            //    input.focusable = true;
            //});
            //input.RegisterCallback<FocusEvent>(evt =>
            //{
            //    _UIStatusManager.SetInputMode(true);
            //    //uiBlocker?.EnableInputMode();
            //});
            //input.RegisterCallback<BlurEvent>(evt =>
            //{
            //    input.focusable = false;
            //    _UIStatusManager.SetInputMode(false);
            //    //uiBlocker?.DisableInputMode();
            //});
            //input.RegisterCallback<MouseLeaveEvent>(evt =>
            //{
            //    input.focusable = false;
            //});
        }
        RegisterEventsforInput(name);
        //name.focusable = false;
        //name.RegisterCallback<MouseEnterEvent>(evt =>
        //{
        //    name.focusable = true;
        //});
        //name.RegisterCallback<FocusEvent>(evt =>
        //{
        //    _UIStatusManager.SetInputMode(true);
        //    //uiBlocker?.EnableInputMode();
        //});
        //name.RegisterCallback<BlurEvent>(evt =>
        //{
        //    name.focusable = false;
        //    _UIStatusManager.SetInputMode(false);
        //    //uiBlocker?.DisableInputMode();
        //});
        //name.RegisterCallback<MouseLeaveEvent>(evt =>
        //{
        //    name.focusable = false;
        //});
        rotX.RegisterCallback<ChangeEvent<float>>(evt =>
        {
            if (evt.newValue > 360)
            {
                rotX.SetValueWithoutNotify(360);
                return;
            }
            else if (evt.newValue < -360)
            {
                rotX.SetValueWithoutNotify(-360);
                return;
            }
            rotX.SetValueWithoutNotify(evt.newValue);
        });
        rotY.RegisterCallback<ChangeEvent<float>>(evt =>
        {
            if (evt.newValue > 360)
            {
                rotY.SetValueWithoutNotify(360);
                return;
            }
            else if (evt.newValue < -360)
            {
                rotY.SetValueWithoutNotify(-360);
                return;
            }
            rotY.SetValueWithoutNotify(evt.newValue);
        });
        rotZ.RegisterCallback<ChangeEvent<float>>(evt =>
        {
            if (evt.newValue > 360)
            {
                rotZ.SetValueWithoutNotify(360);
                return;
            }
            else if (evt.newValue < -360)
            {
                rotZ.SetValueWithoutNotify(-360);
                return;
            }
            rotZ.SetValueWithoutNotify(evt.newValue);
        });
    }

    private void RegisterEventsforInput(VisualElement input)
    {
        //input.focusable = false;
        //input.RegisterCallback<MouseEnterEvent>(evt =>
        //{
        //    input.focusable = true;
        //});
        //input.RegisterCallback<FocusEvent>(evt =>
        //{
        //    _UIStatusManager.SetInputMode(true);
        //    //uiBlocker?.EnableInputMode();
        //});
        //input.RegisterCallback<BlurEvent>(evt =>
        //{
        //    input.focusable = false;
        //    _UIStatusManager.SetInputMode(false);
        //    //uiBlocker?.DisableInputMode();
        //});
        //input.RegisterCallback<MouseLeaveEvent>(evt =>
        //{
        //    input.focusable = false;
        //});
    }

    public void TogglePanel()
    {
        propertiesPanel.visible = _UIStatusManager.IsPropertiesPanelVisible == true ? true : false;
    }

    //public void ShowPanel()
    //{
    //    propertiesPanel.visible = true;
    //}

    public void AddNewTransformOperation(IPropertyProvider provider)
    {

    }
    //public void UpdateTransform(IPropertyProvider provider)
    public void UpdateTransform()
    {
        //if (provider == null) return;

        posX.SetValueWithoutNotify(current.LocalPosition.x);
        posY.SetValueWithoutNotify(current.LocalPosition.y);
        posZ.SetValueWithoutNotify(current.LocalPosition.z);

        rotX.SetValueWithoutNotify(current.Rotation.x);
        rotY.SetValueWithoutNotify(current.Rotation.y);
        rotZ.SetValueWithoutNotify(current.Rotation.z);

        scaleX.SetValueWithoutNotify(current.Scale.x);
        scaleY.SetValueWithoutNotify(current.Scale.y);
        scaleZ.SetValueWithoutNotify(current.Scale.z);
    }

    private void UpdateJointsJOG(RobotPropertyProvider robot)
    {
        if (current is JOGPropertyProvider)
        {
            var j1angle = propertiesPanel?.Q<VisualElement>("J1Angle");
            var j2angle = propertiesPanel?.Q<VisualElement>("J2Angle");
            var j3angle = propertiesPanel?.Q<VisualElement>("J3Angle");
            var j4angle = propertiesPanel?.Q<VisualElement>("J4Angle");
            var j5angle = propertiesPanel?.Q<VisualElement>("J5Angle");
            var j6angle = propertiesPanel?.Q<VisualElement>("J6Angle");
            j1angle.Q<Slider>().value = (current as JOGPropertyProvider).J1Angle;
            j2angle.Q<Slider>().value = (current as JOGPropertyProvider).J2Angle;
            j3angle.Q<Slider>().value = (current as JOGPropertyProvider).J3Angle;
            j4angle.Q<Slider>().value = (current as JOGPropertyProvider).J4Angle;
            j5angle.Q<Slider>().value = (current as JOGPropertyProvider).J5Angle;
            j6angle.Q<Slider>().value = (current as JOGPropertyProvider).J6Angle;
            var jogPoint = propertiesPanel?.Q<VisualElement>("ConfigPoint");
            jogPoint.Q<FloatField>().value = (current as JOGPropertyProvider).ConfigPoint;
        }
        if(current is PointPropertyProvider)
        {
            var config = robot.JOGpoint.ConfigPoint;
            var jogPoint = propertiesPanel?.Q<VisualElement>("ConfigPoint");
            jogPoint.Q<FloatField>().value = config;
        }  
    }

    private void BuildCustomProperties(IPropertyProvider provider)
    {
        customContainer.Clear();
        if (provider.GetCustomProperties() == null) return;

        foreach (var prop in provider.GetCustomProperties())
        {
            var container = PropertyFieldFactory.CreateField(prop, out var setValue, out var getValue, out var fieldElement);
            customContainer.Add(container);
            if (fieldElement is FloatField floatField)
            {
                var slider = container.Q<Slider>();
                if (slider != null)
                {
                    var floatFieldInSlider = container.Q<FloatField>();
                    slider.RegisterCallback<BlurEvent>(_ => prop.Setter(floatFieldInSlider.value));
                }
                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(floatField, provider, prop.Name,
                    _undoRedoManager, _UIStatusManager, () => prop.Setter(floatField.value)));
            }
            else if (fieldElement is DropdownField dropdown)
            {

                dropdown.RegisterCallback<MouseDownEvent>(evt =>
                {
                    _UIStatusManager?.SetInputMode(true);
                    _UIStatusManager?.SetPointerOverUI(true);
                });

                dropdown.RegisterValueChangedCallback(evt =>
                {
                    _UIStatusManager?.SetInputMode(false);
                    _UIStatusManager?.SetPointerOverUI(false);
                    prop.Setter(evt.newValue);
                });

                //dropdown.RegisterCallback<BlurEvent>(_ =>
                //{
                //    if (isDropdownOpen)
                //    {
                //        isDropdownOpen = false;
                //        _UIStatusManager?.SetPointerOverUI(false);
                //    }
                //});
                //dropdown.RegisterCallback<BlurEvent>(_ => _UIStatusManager?.SetPointerOverUI(false));
                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(dropdown, provider, prop.Name,
                    _undoRedoManager, _UIStatusManager, () => prop.Setter(dropdown.value)));
            }
            else if (fieldElement is IntegerField intField)
            {
                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(intField, provider, prop.Name,
                    _undoRedoManager, _UIStatusManager, () => prop.Setter(intField.value)));
            }
            else if (fieldElement is Toggle toggle)
            {
                toggle.RegisterValueChangedCallback(evt => prop.Setter(evt.newValue));
                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(toggle, provider, prop.Name,
                    _undoRedoManager, _UIStatusManager, () => prop.Setter(toggle.value)));
            }
            else if (fieldElement is TextField textField)
            {
                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(textField, provider, prop.Name,
                    _undoRedoManager, _UIStatusManager, () => prop.Setter(textField.value)));
            }
            else if (fieldElement is ColorFieldElement colorField) 
            {
                cleanupActions.Add(FieldBindingUtils.BindColorFieldWithHistory(
                    colorField, provider, prop.Name,
                    getter: () => getValue(),
                    setter: val => prop.Setter(val),
                    _undoRedoManager, _UIStatusManager));
            }
            else if (fieldElement is ButtonPropertyElement buttonField)
            {
               
            }

        }
    }

    private void NormalizeRotationUI()
    {
        if (current == null) return;

        Vector3 rotation = current.Rotation;
        rotation.x = NormalizeAngle(rotation.x);
        rotation.y = NormalizeAngle(rotation.y);
        rotation.z = NormalizeAngle(rotation.z);

        current.Rotation = rotation;

        rotX.SetValueWithoutNotify(rotation.x);
        rotY.SetValueWithoutNotify(rotation.y);
        rotZ.SetValueWithoutNotify(rotation.z);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }

    void OnDestroy()
    {
        ClearBindings();
        _UIStatusManager.SetInputMode(false);
        _UIStatusManager.SetPointerOverUI(false);
    }
}