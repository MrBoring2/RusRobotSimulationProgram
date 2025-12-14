using System;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Windows;
using static UnityEditor.PlayerSettings;

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
    private TextField name;
    public event Action OnTargetNameChanged;
    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        propertiesPanel = root.Q<VisualElement>("properties-container");
        HidePanel();
        commonContainer = root.Q("base-properties-container");
        customContainer = root.Q("custom-properties-container");
        //inputs = root.Query<FloatField>().ToList();
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
        RegisterInputs();
        RegisterButtons();
        RegisterValueCallbacks();
    }

    private void RegisterButtons()
    {
        var closeBtn = root.Q<Button>("close-button");
        Debug.Log(closeBtn);
        closeBtn.RegisterCallback<ClickEvent>(evt =>
        {
            //if (evt.button == 0)
            {
                Debug.Log("Clicked");
                propertiesPanel.visible = false;
            }
        });
    }

    private void RegisterInputs()
    {
        var inputs = root.Query<FloatField>().ToList();
        Debug.Log(inputs.Count);
        foreach (var input in inputs)
        {
            input.focusable = false;
            input.RegisterCallback<MouseEnterEvent>(evt =>
            {
                Debug.Log(evt.button);
                input.focusable = true;
            });
            input.RegisterCallback<FocusEvent>(evt =>
            {
                uiBlocker.EnableInputMode();
            });
            input.RegisterCallback<BlurEvent>(evt =>
            {
                input.focusable = false;
                uiBlocker.DisableInputMode();
            });
            input.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                input.focusable = false;
            });
        }

        name.RegisterCallback<MouseEnterEvent>(evt =>
        {
            Debug.Log(evt.button);
            name.focusable = true;
        });
        name.RegisterCallback<FocusEvent>(evt =>
        {
            uiBlocker.EnableInputMode();
        });
        name.RegisterCallback<BlurEvent>(evt =>
        {
            name.focusable = false;
            uiBlocker.DisableInputMode();
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
    public void UpdateTransform(IPropertyProvider provider)
    {
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
    public void ShowProperties(IPropertyProvider propertyProvider)
    {
        current = propertyProvider;
        customContainer.Clear();
        name.SetValueWithoutNotify(current.Name);
        UpdateTransform(propertyProvider);
        //UpdateCustomProperties();

        //BindVector3("position",
        //() => propertyProvider.Position,
        //v => propertyProvider.Position = v);

        //BindVector3("rotation",
        //    () => propertyProvider.Rotation,
        //    v => propertyProvider.Rotation = v);

        //BindVector3("scale",
        //    () => propertyProvider.Scale,
        //    v => propertyProvider.Scale = v);
        propertyProvider.BuildCustomProperties(customContainer);
    }

    private void UpdateCustomProperties()
    {
        
    }

    private void BindVector3(string prefix, Func<Vector3> getter, Action<Vector3> setter)
    {
        var xField = root.Q<FloatField>($"{prefix}-x");
        var yField = root.Q<FloatField>($"{prefix}-y");
        var zField = root.Q<FloatField>($"{prefix}-z");

        // отключаем старые события, чтобы не дублировались
        xField.UnregisterValueChangedCallback(OnValueChanged);
        yField.UnregisterValueChangedCallback(OnValueChanged);
        zField.UnregisterValueChangedCallback(OnValueChanged);

        Vector3 v = getter();

        xField.value = v.x;
        yField.value = v.y;
        zField.value = v.z;

        void OnValueChanged(ChangeEvent<float> e)
        {
            Vector3 newV = new Vector3(xField.value, yField.value, zField.value);
            setter(newV);
        }

        xField.RegisterValueChangedCallback(OnValueChanged);
        yField.RegisterValueChangedCallback(OnValueChanged);
        zField.RegisterValueChangedCallback(OnValueChanged);
    }
    private void RegisterValueCallbacks()
    {
        posX.RegisterValueChangedCallback(_ => ApplyPosition());
        posY.RegisterValueChangedCallback(_ => ApplyPosition());
        posZ.RegisterValueChangedCallback(_ => ApplyPosition());

        rotX.RegisterValueChangedCallback(e => ApplyRotationWithLimit(e, rotX));
        rotY.RegisterValueChangedCallback(e => ApplyRotationWithLimit(e, rotY));
        rotZ.RegisterValueChangedCallback(e => ApplyRotationWithLimit(e, rotZ));
        rotX.RegisterCallback<BlurEvent>(evt => NormalizeRotationUI());
        rotY.RegisterCallback<BlurEvent>(evt => NormalizeRotationUI());
        rotZ.RegisterCallback<BlurEvent>(evt => NormalizeRotationUI());

        scaleX.RegisterValueChangedCallback(_ => ApplyScale());
        scaleY.RegisterValueChangedCallback(_ => ApplyScale());
        scaleZ.RegisterValueChangedCallback(_ => ApplyScale());

        name.RegisterValueChangedCallback(_ => ApplyName());
    }

    private void ApplyName()
    {
        if (current == null) return;

        current.Name = name.value;
        Debug.Log("NAME " + current.Name + "||||");
        OnTargetNameChanged?.Invoke();
    }

    private void ApplyPosition()
    {
        if (current == null) return;

        current.Position = new Vector3(
            posX.value,
            posY.value,
            posZ.value
        );
    }
    private void ApplyRotationWithLimit(ChangeEvent<float> e, FloatField field)
    {
        float value = Mathf.Repeat(e.newValue, 361f);

        field.SetValueWithoutNotify(value);
        ApplyRotation();
    }
    private void ApplyRotation()
    {
        if (current == null) return;

        current.Rotation = new Vector3(
            rotX.value,
            rotY.value,
            rotZ.value
        );
    }
    private void NormalizeRotationUI()
    {
        if (current == null) return;

        // Берем введенные значения
        float x = rotX.value;
        float y = rotY.value;
        float z = rotZ.value;

        // Нормализуем в диапазон [-360, 360]
        x = NormalizeAngle(x);
        y = NormalizeAngle(y);
        z = NormalizeAngle(z);

        // Сохраняем в модель
        current.Rotation = new Vector3(x, y, z);

        // Обновляем UI
        rotX.SetValueWithoutNotify(x);
        rotY.SetValueWithoutNotify(y);
        rotZ.SetValueWithoutNotify(z);
    }
    private float NormalizeAngle(float angle)
    {
        angle %= 360f;        // сначала остаток от деления на 360
        if (angle > 360f) return angle - 360f;
        if (angle < -360f) return angle + 360f;
        return angle;
    }
    private void ApplyScale()
    {
        if (current == null) return;

        current.Scale = new Vector3(
            scaleX.value,
            scaleY.value,
            scaleZ.value
        );
    }
}
