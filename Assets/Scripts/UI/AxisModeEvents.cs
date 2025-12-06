using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AxisModeEvents : MonoBehaviour
{
    public GyzmoManupulator manipulator;
    public Button localAxisButton;
    public Button globalAxisButton;
    private VisualElement root;
    private List<(Button btn, EventCallback<ClickEvent> click)> registeredButtons
        = new List<(Button, EventCallback<ClickEvent>)>();
    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        RegisterButton("local-mode-btn", () => OnLocalModeClick());
        RegisterButton("global-mode-btn", () => OnGlobalModeClick());
    }
    private void RegisterButton(string buttonName, Action clickHandler)
    {
        var btn = root.Q<Button>(buttonName);
        if (btn == null) return;

        EventCallback<ClickEvent> onClick = evt =>
        {
            clickHandler.Invoke();
        };

        btn.RegisterCallback(onClick);

        registeredButtons.Add((btn, onClick));
    }
    private void OnLocalModeClick() => manipulator.SetAxisMode(AxisMode.Local);
    //private void OnRotationModeClick() => manipulator.SetMode(new RotateMode(manipulator.GetComponent<LineRenderer>()));
    private void OnGlobalModeClick() => manipulator.SetAxisMode(AxisMode.Global);

    private void OnDisable()
    {
        foreach (var (btn, click) in registeredButtons)
        {
            btn.UnregisterCallback(click);
        }
        registeredButtons.Clear();
    }
}
