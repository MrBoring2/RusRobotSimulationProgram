using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Simulation;
using Assets.Scripts.CustomServiceManager;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ManipulatorModePanelEvents : MonoBehaviour
{
    //public GyzmoManupulator manipulator;
    private SceneManipulatorModeManager _sceneManipulatorModeManager;
    private EventBus _eventBus;
    public Button lookButton;
    public Button moveButton;
    public Button rotateButton;
    public Button jogButton;
    public TooltipEvents tooltipEvents;
    private VisualElement root;
    private List<(Button btn, EventCallback<ClickEvent> click)> registeredButtons
        = new List<(Button, EventCallback<ClickEvent>)>();
    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        _sceneManipulatorModeManager = ServiceManager.Current.Get<SceneManipulatorModeManager>();
        root = GetComponent<UIDocument>().rootVisualElement;
        RegisterButtons();
    }

    private void RegisterButtons()
    {
        lookButton = root.Q<Button>("mode-look-button");
        moveButton = root.Q<Button>("mode-move-button");
        rotateButton = root.Q<Button>("mode-rotate-button");
        jogButton = root.Q<Button>("mode-jog-button");
        lookButton.RegisterCallback<ClickEvent>(OnLookModeClick);
        moveButton.RegisterCallback<ClickEvent>(OnMoveModeClick);
        rotateButton.RegisterCallback<ClickEvent>(OnRotationModeClick);
        jogButton.RegisterCallback<ClickEvent>(OnJogClick);
        tooltipEvents.RegisterTooltip(lookButton, "Режим просмотра");
        tooltipEvents.RegisterTooltip(moveButton, "Режим перемещения");
        tooltipEvents.RegisterTooltip(rotateButton, "Режим вращения");
        moveButton.AddToClassList("active");
        _sceneManipulatorModeManager.SetManipulatorMode(SceneManipulatorMode.Move);
        //manipulator.SetManipulatorMode(new MoveMode());
    }

    private void OnJogClick(ClickEvent evt)
    {
        RemoveActive();
        jogButton.AddToClassList("active");
        _sceneManipulatorModeManager.SetManipulatorMode(SceneManipulatorMode.JOG);
    }

    private void OnLookModeClick(ClickEvent evt)
    {
        RemoveActive();
        lookButton.AddToClassList("active");
        _sceneManipulatorModeManager.SetManipulatorMode(SceneManipulatorMode.Drag);
        //manipulator.SetManipulatorModeCamera();
    }

    private void OnMoveModeClick(ClickEvent click)
    {
        RemoveActive();
        moveButton.AddToClassList("active");
        _sceneManipulatorModeManager.SetManipulatorMode(SceneManipulatorMode.Move);
        //manipulator.SetManipulatorMode(new MoveMode());
    }
    //private void OnRotationModeClick() => manipulator.SetMode(new RotateMode(manipulator.GetComponent<LineRenderer>()));
    private void OnRotationModeClick(ClickEvent click)
    {
        RemoveActive();
        rotateButton.AddToClassList("active");
        _sceneManipulatorModeManager.SetManipulatorMode(SceneManipulatorMode.Rotation);
        //manipulator.SetManipulatorMode(new RotateMode());
    }

    private void RemoveActive()
    {
        lookButton.RemoveFromClassList("active");
        moveButton.RemoveFromClassList("active");
        rotateButton.RemoveFromClassList("active");
        jogButton.RemoveFromClassList("active");
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
