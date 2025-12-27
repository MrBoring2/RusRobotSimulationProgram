using Assets.Scripts.Models;
using Assets.Scripts.SystemManager;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PropertiesPanelEvents : MonoBehaviour
{
    private VisualElement root;
    private VisualElement propertiesPanel;
    public UIBlocker uiBlocker;
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

    private List<Action> cleanupActions = new List<Action>();

    public event Action OnTargetNameChanged;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        propertiesPanel = root.Q<VisualElement>("properties-container");
        HidePanel();
        commonContainer = root.Q("base-properties-container");
        customContainer = root.Q("custom-properties-container");

        posX = root.Q<FloatField>("position-x");
        posY = root.Q<FloatField>("position-y");
        posZ = root.Q<FloatField>("position-z");

        rotX = root.Q<FloatField>("rotation-x");
        rotY = root.Q<FloatField>("rotation-y");
        rotZ = root.Q<FloatField>("rotation-z");

        scaleX = root.Q<FloatField>("scale-x");
        scaleY = root.Q<FloatField>("scale-y");
        scaleZ = root.Q<FloatField>("scale-z");

        name = root.Q<TextField>("name");

        namePropertyContainer = root.Q<VisualElement>("name-property-container");
        positionPropertyContainer = root.Q<VisualElement>("position-property-container");
        rotationPropertyContainer = root.Q<VisualElement>("rotation-property-container");
        scalePropertyContainer = root.Q<VisualElement>("scale-property-container");

        RegisterButtons();
        RegisterInputs();
        UndoRedoSystem.Instance.OnCommandExecuted += OnUndoRedoPerformed;
        UndoRedoSystem.Instance.OnCommandUndone += OnUndoRedoPerformed;
    }

    public void ShowProperties(IPropertyProvider propertyProvider)
    {
        ClearBindings();

        current = propertyProvider;

        if (!current.DisplayName)
        {
            HideElement(namePropertyContainer);
        }
        else ShowElement(namePropertyContainer);
        if (!current.DisplayPosition)
        {
            HideElement(positionPropertyContainer);
        }
        else ShowElement(positionPropertyContainer);
        if (!current.DisplayRotation)
        {
            HideElement(rotationPropertyContainer);
        }
        else ShowElement(rotationPropertyContainer);
        if (!current.DisplayScale)
        {
            HideElement(scalePropertyContainer);
        }
        else ShowElement(scalePropertyContainer);


        customContainer.Clear();

        if (current != null)
        {
            UpdateUI();
            RegisterBaseFieldsBindings();
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

    private void OnUndoRedoPerformed(ICommand command)
    {
        // Если команда относится к текущему объекту, обновляем UI
        if (current != null && command is PropertyChangeCommand propertyCommand)
        {
            // Проверяем, относится ли команда к текущему объекту
            if (propertyCommand.Target == current ||
                (propertyCommand.Target is IPropertyProvider provider && provider == current))
            {
                UpdateUI();
            }
        }
    }

    private void UpdateUI()
    {
        name.SetValueWithoutNotify(current.Name);
        UpdateTransform(current);
        BuildCustomProperties(current);
    }

    private void ClearBindings()
    {
        // Очищаем все привязки
        foreach (var cleanup in cleanupActions)
        {
            cleanup?.Invoke();
        }
        cleanupActions.Clear();
    }

    private void RegisterBaseFieldsBindings()
    {
        if (current == null) return;

        // Позиция - сохраняем Action для отписки
        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(posX, current, nameof(IPropertyProvider.Position), () =>
        {
            if (current != null) current.Position = new Vector3(posX.value, current.Position.y, current.Position.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(posY, current, nameof(IPropertyProvider.Position), () =>
        {
            if (current != null) current.Position = new Vector3(current.Position.x, posY.value, current.Position.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(posZ, current, nameof(IPropertyProvider.Position), () =>
        {
            if (current != null) current.Position = new Vector3(current.Position.x, current.Position.y, posZ.value);
        }));

        // Поворот - сохраняем Action для отписки
        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(rotX, current, nameof(IPropertyProvider.Rotation), () =>
        {
            if (current != null) current.Rotation = new Vector3(NormalizeAngle(rotX.value), current.Rotation.y, current.Rotation.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(rotY, current, nameof(IPropertyProvider.Rotation), () =>
        {
            if (current != null) current.Rotation = new Vector3(current.Rotation.x, NormalizeAngle(rotY.value), current.Rotation.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(rotZ, current, nameof(IPropertyProvider.Rotation), () =>
        {
            if (current != null) current.Rotation = new Vector3(current.Rotation.x, current.Rotation.y, NormalizeAngle(rotZ.value));
        }));

        // Нормализация при Blur - сохраняем ссылки на обработчики для отписки
        EventCallback<BlurEvent> rotXBlurHandler = evt => NormalizeRotationUI();
        EventCallback<BlurEvent> rotYBlurHandler = evt => NormalizeRotationUI();
        EventCallback<BlurEvent> rotZBlurHandler = evt => NormalizeRotationUI();

        rotX.RegisterCallback(rotXBlurHandler);
        rotY.RegisterCallback(rotYBlurHandler);
        rotZ.RegisterCallback(rotZBlurHandler);

        cleanupActions.Add(() => rotX.UnregisterCallback(rotXBlurHandler));
        cleanupActions.Add(() => rotY.UnregisterCallback(rotYBlurHandler));
        cleanupActions.Add(() => rotZ.UnregisterCallback(rotZBlurHandler));

        // Масштаб - сохраняем Action для отписки
        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(scaleX, current, nameof(IPropertyProvider.Scale), () =>
        {
            if (current != null) current.Scale = new Vector3(scaleX.value, current.Scale.y, current.Scale.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(scaleY, current, nameof(IPropertyProvider.Scale), () =>
        {
            if (current != null) current.Scale = new Vector3(current.Scale.x, scaleY.value, current.Scale.z);
        }));

        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(scaleZ, current, nameof(IPropertyProvider.Scale), () =>
        {
            if (current != null) current.Scale = new Vector3(current.Scale.x, current.Scale.y, scaleZ.value);
        }));

        // Имя - сохраняем Action для отписки
        cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(name, current, nameof(IPropertyProvider.Name), () =>
        {
            if (current != null)
            {
                current.Name = name.value;
                OnTargetNameChanged?.Invoke();
            }
        }));
    }

    private void RegisterButtons()
    {
        var closeBtn = root.Q<Button>("close-button");
        closeBtn.clicked += () =>
        {
            propertiesPanel.visible = false;
        };
    }

    private void RegisterInputs()
    {
        var inputs = root.Query<FloatField>().ToList();
        foreach (var input in inputs)
        {
            input.focusable = false;
            input.RegisterCallback<MouseEnterEvent>(evt =>
            {
                input.focusable = true;
            });
            input.RegisterCallback<FocusEvent>(evt =>
            {
                uiBlocker?.EnableInputMode();
            });
            input.RegisterCallback<BlurEvent>(evt =>
            {
                input.focusable = false;
                uiBlocker?.DisableInputMode();
            });
            input.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                input.focusable = false;
            });
        }

        name.focusable = false;
        name.RegisterCallback<MouseEnterEvent>(evt =>
        {
            name.focusable = true;
        });
        name.RegisterCallback<FocusEvent>(evt =>
        {
            uiBlocker?.EnableInputMode();
        });
        name.RegisterCallback<BlurEvent>(evt =>
        {
            name.focusable = false;
            uiBlocker?.DisableInputMode();
        });
        name.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            name.focusable = false;
        });
    }

    public void HidePanel()
    {
        propertiesPanel.visible = false;
    }

    public void ShowPanel()
    {
        propertiesPanel.visible = true;
    }

    public void AddNewTransformOperation(IPropertyProvider provider)
    {

    }
    public void UpdateTransform(IPropertyProvider provider)
    {
        if (provider == null) return;

        posX.SetValueWithoutNotify(provider.Position.x);
        posY.SetValueWithoutNotify(provider.Position.y);
        posZ.SetValueWithoutNotify(provider.Position.z);

        rotX.SetValueWithoutNotify(provider.Rotation.x);
        rotY.SetValueWithoutNotify(provider.Rotation.y);
        rotZ.SetValueWithoutNotify(provider.Rotation.z);

        scaleX.SetValueWithoutNotify(provider.Scale.x);
        scaleY.SetValueWithoutNotify(provider.Scale.y);
        scaleZ.SetValueWithoutNotify(provider.Scale.z);
    }

    private void BuildCustomProperties(IPropertyProvider provider)
    {
        customContainer.Clear();
        if (provider.GetCustomProperties() == null) return;

        foreach (var prop in provider.GetCustomProperties())
        {
            if (prop.PropertyType == typeof(float))
            {
                var field = new FloatField(prop.Name);
                field.value = (float)prop.Getter();
                customContainer.Add(field);

                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(field, provider, prop.Name, () =>
                {
                    prop.Setter(field.value);
                }));
            }
            else if(prop.PropertyType == typeof(bool))
            {
                var field = new Toggle(prop.Name);
                field.value = (bool)prop.Getter();
                customContainer.Add(field);

                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(field, provider, prop.Name, () =>
                {
                    prop.Setter(field.value);
                }));
            }
            else if (prop.PropertyType == typeof(int))
            {
                var field = new IntegerField(prop.Name);
                field.value = (int)prop.Getter();
                customContainer.Add(field);

                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(field, provider, prop.Name, () =>
                {
                    prop.Setter(field.value);
                }));
            }
            else if (prop.PropertyType == typeof(string))
            {
                var field = new TextField(prop.Name);
                field.value = (string)prop.Getter();
                customContainer.Add(field);

                cleanupActions.Add(FieldBindingUtils.BindFieldWithHistory(field, provider, prop.Name, () =>
                {
                    prop.Setter(field.value);
                }));
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
    }
}