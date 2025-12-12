using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class UIBlocker : MonoBehaviour
{
    private VisualElement root;
    public bool isPointerOverUI { get; private set; }
    public bool isInputMode { get; private set; }

    private List<VisualElement> uiElements;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        // ќпредел€ем список панелей, по которым нужно отслеживать курсор
        uiElements = new List<VisualElement>
        {
            root.Q("gyzmo-manipulator-mode-container"),
            root.Q("menu-bar-container"),
            root.Q("hierarchy-container"),
            root.Q("properties-container"),
            root.Q("axis-mode-panel-container"),
            root.Q("perspective-panel-container"),
        };
        Debug.Log(root);
        // –егистрируем событи€ дл€ каждой панели
        foreach (var panel in uiElements)
        {
            panel.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            panel.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
    }

    public void AddNewContextMenu(VisualElement contextMenu)
    {
        contextMenu.RegisterCallback<MouseEnterEvent>(OnContextMenuMouseEnter);
        contextMenu.RegisterCallback<MouseLeaveEvent>(OnContextMenuMouseLeave);
    }
    public void RemoveContextMenu(VisualElement contextMenu)
    {
        uiElements.Remove(contextMenu);
        contextMenu.UnregisterCallback<MouseEnterEvent>(OnContextMenuMouseEnter);
        contextMenu.UnregisterCallback<MouseLeaveEvent>(OnContextMenuMouseLeave);
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        Debug.Log(evt.target);
        isPointerOverUI = true;  //  огда курсор заходит на панель 
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        isPointerOverUI = false;  //  огда курсор покидает панель
    }

    private void OnContextMenuMouseEnter(MouseEnterEvent evt)
    {
        Debug.Log(evt.target);
        isPointerOverUI = true;  //  огда курсор заходит на панель 
    }

    private void OnContextMenuMouseLeave(MouseLeaveEvent evt)
    {
        isPointerOverUI = false;  //  огда курсор покидает панель
    }
    private void UnregisterUIElements()
    {
        foreach (var panel in uiElements)
        {
            panel.UnregisterCallback<MouseEnterEvent>(OnMouseEnter);
            panel.UnregisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }
    }

    public void EnableInputMode()
    {
        isInputMode = true;
    }

    public void DisableInputMode()
    {
        isInputMode = false;
    }
    private void OnDisable()
    {
        UnregisterUIElements();
    }
    private void OnDestroy()
    {
        UnregisterUIElements();
    }
}
