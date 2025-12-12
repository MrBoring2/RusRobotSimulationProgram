using System;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UIElements;
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

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        propertiesPanel = root.Q<VisualElement>("properties-container");
        commonContainer = root.Q("base-properties-container");
        customContainer = root.Q("custom-properties-container");
        //inputs = root.Query<FloatField>().ToList();
        RegisterInputs();
        RegisterButtons();
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

        
    }

    public void ShowProperties(IPropertyProvider propertyProvider)
    {
        current = propertyProvider;
        customContainer.Clear();

        BindVector3("position",
        () => propertyProvider.Position,
        v => propertyProvider.Position = v);

        BindVector3("rotation",
            () => propertyProvider.Rotation,
            v => propertyProvider.Rotation = v);

        BindVector3("scale",
            () => propertyProvider.Scale,
            v => propertyProvider.Scale = v);
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
}
