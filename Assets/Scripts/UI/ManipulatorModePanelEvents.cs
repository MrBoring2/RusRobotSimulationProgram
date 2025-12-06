using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ManipulatorController : MonoBehaviour
{
    public GyzmoManupulator manipulator;
    public Button moveButton;
    public Button rotateButton;
    private VisualElement root;
    private List<(Button btn, EventCallback<ClickEvent> click)> registeredButtons
        = new List<(Button, EventCallback<ClickEvent>)>();
    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        RegisterButton("move-mode-btn", () => OnMoveModeClick());
        RegisterButton("rotate-mode-btn", () => OnRotationModeClick());
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
    private void OnMoveModeClick() => manipulator.SetManipulatorMode(new MoveMode());
    //private void OnRotationModeClick() => manipulator.SetMode(new RotateMode(manipulator.GetComponent<LineRenderer>()));
    private void OnRotationModeClick() => manipulator.SetManipulatorMode(new RotateMode());

    private void OnDisable()
    {
        foreach (var (btn, click) in registeredButtons)
        {
            btn.UnregisterCallback(click);
        }
        registeredButtons.Clear();
    }
}
