using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AxisModeEvents : MonoBehaviour
{
    //public GyzmoManupulator manipulator;
    public Button localAxisButton;
    public Button globalAxisButton;
    public TooltipEvents tooltipEvents;
    private VisualElement root;
    private List<(Button btn, EventCallback<ClickEvent> click)> registeredButtons
        = new List<(Button, EventCallback<ClickEvent>)>();
    private AxisModeManager _axisModeManager;
    private void Start()
    {
        _axisModeManager = ServiceManager.Current.Get<AxisModeManager>();
        root = GetComponent<UIDocument>().rootVisualElement;
        RegisterButtons();
    }
    private void RegisterButtons()
    {
        localAxisButton = root.Q<Button>("local-coord-button");
        globalAxisButton = root.Q<Button>("global-coord-button");
        localAxisButton.RegisterCallback<ClickEvent>(OnLocalModeClick);
        globalAxisButton.RegisterCallback<ClickEvent>(OnGlobalModeClick);
        tooltipEvents.RegisterTooltip(localAxisButton, "Локальные координаты");
        tooltipEvents.RegisterTooltip(globalAxisButton, "Глобальные координаты");
        globalAxisButton.AddToClassList("active");
    }
    private void OnLocalModeClick(ClickEvent click)
    {
        RemoveActive();
        localAxisButton.AddToClassList("active");
        _axisModeManager.SetAxisMode(AxisMode.Local);
        //manipulator.SetAxisMode(AxisMode.Local);
    }
    private void OnGlobalModeClick(ClickEvent click)
    {
        RemoveActive();
        globalAxisButton.AddToClassList("active");
        _axisModeManager.SetAxisMode(AxisMode.Global);
        //manipulator.SetAxisMode(AxisMode.Global);
    }
    private void RemoveActive()
    {
        globalAxisButton.RemoveFromClassList("active");
        localAxisButton.RemoveFromClassList("active");
    }
    private void OnDisable()
    {
        foreach (var (btn, click) in registeredButtons)
        {
            btn.UnregisterCallback(click);
        }
        registeredButtons.Clear();
    }
}
