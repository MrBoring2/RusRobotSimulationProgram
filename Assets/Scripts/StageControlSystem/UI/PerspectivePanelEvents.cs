using Assets.Scripts.CustomEventBus;
using Assets.Scripts.CustomEventBus.Signals.Camera;
using Assets.Scripts.CustomServiceManager;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PerspectivePanelEvents : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    private VisualElement root;
    //private MainCameraMovement cameraMovement; 

    // Список для хранения колбэков
    private List<(PerspectiveButton btn, EventCallback<MouseDownEvent> click, EventCallback<MouseMoveEvent> move, EventCallback<MouseLeaveEvent> leave)> registeredButtons
        = new List<(PerspectiveButton, EventCallback<MouseDownEvent>, EventCallback<MouseMoveEvent>, EventCallback<MouseLeaveEvent>)>();
    private EventBus _eventBus;

    private bool topViewClicked = false;
    private void Start()
    {
        _eventBus = ServiceManager.Current.Get<EventBus>();
        root = GetComponent<UIDocument>().rootVisualElement;
        //cameraMovement = targetCamera.transform.parent.GetComponent<MainCameraMovement>();
        RegisterButton("back-view-btn", () => OnBackViewClick());
        RegisterButton("left-view-btn", () => OnLeftViewClick());
        RegisterButton("right-view-btn", () => OnRightViewClick());
        RegisterButton("top-bottom-view-btn", () => OnTopBottomViewClick());
        RegisterButton("front-view-btn", () => OnFrontViewClick());
        RegisterButton("toggle-proection-btn", () => OnToggleProection());
    }

    private void RegisterButton(string buttonName, Action clickHandler)
    {
        var btn = root.Q<PerspectiveButton>(buttonName);
        if (btn == null) return;

        // Создаём EventCallback для каждого события
        EventCallback<MouseLeaveEvent> onLeave = evt => btn.IsHovered = false;
        EventCallback<MouseMoveEvent> onMove = evt =>
        {
            Vector2 local = evt.localMousePosition;
            btn.IsHovered = btn.IsPointInside(local);
        };
        EventCallback<MouseDownEvent> onClick = evt =>
        {
            Vector2 local = evt.localMousePosition;
            if (btn.IsPointInside(local) && evt.button == 0)
                clickHandler.Invoke();
            else
                evt.StopPropagation();
        };

        // Регистрируем колбэки
        btn.RegisterCallback(onLeave);
        btn.RegisterCallback(onMove);
        btn.RegisterCallback(onClick);

        // Сохраняем для отписки
        registeredButtons.Add((btn, onClick, onMove, onLeave));
    }

    private void OnDisable()
    {
        foreach (var (btn, click, move, leave) in registeredButtons)
        {
            btn.UnregisterCallback(click);
            btn.UnregisterCallback(move);
            btn.UnregisterCallback(leave);
        }
        registeredButtons.Clear();
    }
    private void RotateCamera(Vector3 eulerAngles)
    {
        if (targetCamera == null) return;

        targetCamera.transform.rotation = Quaternion.Euler(eulerAngles);

        Debug.Log($"Камера повернута на {eulerAngles}");
    }
    private void OnBackViewClick() => _eventBus.Invoke(new RotateCameraSingal(new Vector3(0, 180, 0))); //cameraMovement.RotateToView(new Vector3(0, 180, 0));
    private void OnLeftViewClick() => _eventBus.Invoke(new RotateCameraSingal(new Vector3(0, -90, 0)));//cameraMovement.RotateToView(new Vector3(0, -90, 0));
    private void OnRightViewClick() => _eventBus.Invoke(new RotateCameraSingal(new Vector3(0, 90, 0)));//cameraMovement.RotateToView(new Vector3(0, 90, 0));
    private void OnTopBottomViewClick()
    {
        if (!topViewClicked)
        {
            topViewClicked = true;
            _eventBus.Invoke(new RotateCameraSingal(new Vector3(90, 0, 0)));
            //cameraMovement.RotateToView(new Vector3(90, 0, 0));
        }
        else
        {
            topViewClicked = false;
            _eventBus.Invoke(new RotateCameraSingal(new Vector3(-90, 0, 0)));
            //cameraMovement.RotateToView(new Vector3(-90, 0, 0));
        }
        
    }
    private void OnFrontViewClick() => _eventBus.Invoke(new RotateCameraSingal(new Vector3(0, 0, 0))); //cameraMovement.RotateToView(new Vector3(0, 0, 0));
    private void OnToggleProection() => _eventBus.Invoke(new ToggleOrthographicSignal()); //cameraMovement.ToggleOrthographic();
}
